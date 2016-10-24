using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.DownstreamRouteFinder;
using Ocelot.Errors;
using Ocelot.Infrastructure.Provider;
using Ocelot.Middleware;

namespace Ocelot.ClaimsBuilder.Middleware
{
    public class ClaimsBuilderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAddClaimsToRequest _addClaimsToRequest;
        private readonly IDataProvider<DownstreamRoute> _dataProvider;

        public ClaimsBuilderMiddleware(RequestDelegate next, 
            IAddClaimsToRequest addClaimsToRequest,
            IDataProvider<DownstreamRoute> dataProvider,
            IDataProvider<List<Error>> errorProvider) 
            : base(errorProvider)
        {
            _next = next;
            _addClaimsToRequest = addClaimsToRequest;
            _dataProvider = dataProvider;
        }

        public async Task Invoke(HttpContext context)
        {
            var downstreamRoute = _dataProvider.Get();

            if (downstreamRoute.Data.ReRoute.ClaimsToClaims.Any())
            {
                var result = _addClaimsToRequest.SetClaimsOnContext(downstreamRoute.Data.ReRoute.ClaimsToClaims, context);

                if (result.IsError)
                {
                    SetPipelineError(result.Errors);
                    return;
                }
            }
            
            await _next.Invoke(context);
        }
    }
}
