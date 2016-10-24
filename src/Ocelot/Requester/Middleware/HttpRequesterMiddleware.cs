using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Errors;
using Ocelot.Infrastructure.Provider;
using Ocelot.Middleware;
using Ocelot.RequestBuilder;
using Ocelot.Responder;

namespace Ocelot.Requester.Middleware
{
    public class HttpRequesterMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpRequester _requester;
        private readonly IDataProvider<Request> _requestDataProvider;
        private readonly IHttpResponder _responder;

        public HttpRequesterMiddleware(RequestDelegate next, 
            IHttpRequester requester, 
            IDataProvider<List<Error>> errorProvider, 
            IDataProvider<Request> requestDataProvider, 
            IHttpResponder responder)
            :base(errorProvider)
        {
            _next = next;
            _requester = requester;
            _requestDataProvider = requestDataProvider;
            _responder = responder;
        }

        public async Task Invoke(HttpContext context)
        {
            var request = _requestDataProvider.Get();

            if (request.IsError)
            {
                SetPipelineError(request.Errors);
                return;
            }

            var response = await _requester.GetResponse(request.Data);

            if (response.IsError)
            {
                SetPipelineError(response.Errors);
                return;
            }

            await _responder.SetResponseOnContext(context, response.Data);
        }
    }
}