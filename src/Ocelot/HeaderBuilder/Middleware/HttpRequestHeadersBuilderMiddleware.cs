using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.DownstreamRouteFinder;
using Ocelot.Errors;
using Ocelot.Infrastructure.Provider;
using Ocelot.Middleware;

namespace Ocelot.HeaderBuilder.Middleware
{
    public class HttpRequestHeadersBuilderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAddHeadersToRequest _addHeadersToRequest;
        private readonly IDataProvider<DownstreamRoute> _dataProvider;

        public HttpRequestHeadersBuilderMiddleware(RequestDelegate next, 
            IAddHeadersToRequest addHeadersToRequest,
            IDataProvider<DownstreamRoute> dataProvider,
            IDataProvider<List<Error>> errorProvider) 
            : base(errorProvider)
        {
            _next = next;
            _addHeadersToRequest = addHeadersToRequest;
            _dataProvider = dataProvider;
        }

        public async Task Invoke(HttpContext context)
        {
            var downstreamRoute = _dataProvider.Get();

            if (downstreamRoute.Data.ReRoute.ClaimsToHeaders.Any())
            {
                _addHeadersToRequest.SetHeadersOnContext(downstreamRoute.Data.ReRoute.ClaimsToHeaders, context);
            }
            
            await _next.Invoke(context);
        }
    }
}
