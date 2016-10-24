using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.Errors;
using Ocelot.Infrastructure.Provider;
using Ocelot.Middleware;

namespace Ocelot.DownstreamRouteFinder.Middleware
{
    public class DownstreamRouteFinderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDownstreamRouteFinder _downstreamRouteFinder;
        private readonly IDataProvider<DownstreamRoute> _dataProvider;

        public DownstreamRouteFinderMiddleware(RequestDelegate next, 
            IDownstreamRouteFinder downstreamRouteFinder, 
            IDataProvider<DownstreamRoute> dataProvider,
            IDataProvider<List<Error>> errorProvider)
            :base(errorProvider)
        {
            _next = next;
            _downstreamRouteFinder = downstreamRouteFinder;
            _dataProvider = dataProvider;
        }

        public async Task Invoke(HttpContext context)
        {   
            var upstreamUrlPath = context.Request.Path.ToString();

            var downstreamRoute = _downstreamRouteFinder.FindDownstreamRoute(upstreamUrlPath, context.Request.Method);

            if (downstreamRoute.IsError)
            {
                SetPipelineError(downstreamRoute.Errors);
                return;
            }


            _dataProvider.Set(downstreamRoute.Data);

            await _next.Invoke(context);
        }
    }
}