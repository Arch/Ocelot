using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.DownstreamRouteFinder;
using Ocelot.Errors;
using Ocelot.Infrastructure.Provider;
using Ocelot.Middleware;

namespace Ocelot.Authorisation.Middleware
{
    public class AuthorisationMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAuthoriser _authoriser;
        private readonly IDataProvider<DownstreamRoute> _dataProvider;

        public AuthorisationMiddleware(RequestDelegate next,
            IAuthoriser authoriser,
            IDataProvider<DownstreamRoute> dataProvider,
            IDataProvider<List<Error>> errorProvider)
            : base(errorProvider)
        {
            _next = next;
            _authoriser = authoriser;
            _dataProvider = dataProvider;
        }

        public async Task Invoke(HttpContext context)
        {
            var downstreamRoute = _dataProvider.Get();

            if (downstreamRoute.IsError)
            {
                SetPipelineError(downstreamRoute.Errors);
                return;
            }

            if (downstreamRoute.Data.ReRoute.IsAuthorised)
            {
                var authorised = _authoriser.Authorise(context.User, downstreamRoute.Data.ReRoute.RouteClaimsRequirement);

                if (authorised.IsError)
                {
                    SetPipelineError(authorised.Errors);
                    return;
                }

                if (authorised.Data)
                {
                    await _next.Invoke(context);
                }
                else
                {
                    SetPipelineError(new List<Error>
                    {
                        new UnauthorisedError(
                            $"{context.User.Identity.Name} unable to access {downstreamRoute.Data.ReRoute.UpstreamTemplate}")
                    });
                }
            }
            else
            {
                await _next.Invoke(context);
            }
        }
    }
}
