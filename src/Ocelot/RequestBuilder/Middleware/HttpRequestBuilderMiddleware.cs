using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.DownstreamUrlCreator;
using Ocelot.Errors;
using Ocelot.Infrastructure.Provider;
using Ocelot.Middleware;
using Ocelot.RequestBuilder.Builder;

namespace Ocelot.RequestBuilder.Middleware
{
    public class HttpRequestBuilderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IRequestBuilder _requestBuilder;
        private readonly IDataProvider<DownstreamUrl> _urlDataProvider;
        private readonly IDataProvider<Request> _requestDataProvider;

        public HttpRequestBuilderMiddleware(RequestDelegate next, 
            IRequestBuilder requestBuilder,
            IDataProvider<List<Error>> errorProvider, 
            IDataProvider<DownstreamUrl> urlDataProvider, 
            IDataProvider<Request> requestDataProvider)
            :base(errorProvider)
        {
            _next = next;
            _requestBuilder = requestBuilder;
            _urlDataProvider = urlDataProvider;
            _requestDataProvider = requestDataProvider;
        }

        public async Task Invoke(HttpContext context)
        {
            var downstreamUrl = _urlDataProvider.Get();

            if (downstreamUrl.IsError)
            {
                SetPipelineError(downstreamUrl.Errors);
                return;
            }

            var request = await _requestBuilder
              .Build(context.Request.Method, downstreamUrl.Data.Value, context.Request.Body,
              context.Request.Headers, context.Request.Cookies, context.Request.QueryString.Value, context.Request.ContentType);

            if (request.IsError)
            {
                SetPipelineError(request.Errors);
                return;
            }

            _requestDataProvider.Set(request.Data);
            
            await _next.Invoke(context);
        }
    }
}