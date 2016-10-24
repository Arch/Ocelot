using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamUrlCreator.UrlTemplateReplacer;
using Ocelot.Errors;
using Ocelot.Infrastructure.Provider;
using Ocelot.Middleware;

namespace Ocelot.DownstreamUrlCreator.Middleware
{
    public class DownstreamUrlCreatorMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDownstreamUrlTemplateVariableReplacer _urlReplacer;
        private readonly IDataProvider<DownstreamRoute> _routeDataProvider;
        private readonly IDataProvider<DownstreamUrl> _urlDataProvider;

        public DownstreamUrlCreatorMiddleware(RequestDelegate next, 
            IDownstreamUrlTemplateVariableReplacer urlReplacer,
            IDataProvider<DownstreamRoute> routeDataProvider,
            IDataProvider<List<Error>> errorProvider, 
            IDataProvider<DownstreamUrl> urlDataProvider)
            :base(errorProvider)
        {
            _next = next;
            _urlReplacer = urlReplacer;
            _routeDataProvider = routeDataProvider;
            _urlDataProvider = urlDataProvider;
        }

        public async Task Invoke(HttpContext context)
        {
            var downstreamRoute = _routeDataProvider.Get();

            if (downstreamRoute.IsError)
            {
                SetPipelineError(downstreamRoute.Errors);
                return;
            }

            var downstreamUrl = _urlReplacer.ReplaceTemplateVariables(downstreamRoute.Data);

            if (downstreamUrl.IsError)
            {
                SetPipelineError(downstreamUrl.Errors);
                return;
            }

            _urlDataProvider.Set(downstreamUrl.Data);
                
            await _next.Invoke(context);
        }
    }
}