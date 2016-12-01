using Microsoft.AspNetCore.Builder;
using Ocelot.Authentication.Middleware;
using Ocelot.Cache.Middleware;
using Ocelot.Claims.Middleware;
using Ocelot.DownstreamRouteFinder.Middleware;
using Ocelot.DownstreamUrlCreator.Middleware;
using Ocelot.Errors.Middleware;
using Ocelot.Headers.Middleware;
using Ocelot.QueryStrings.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Requester.Middleware;
using Ocelot.RequestId.Middleware;
using Ocelot.Responder.Middleware;
using Microsoft.AspNetCore.Hosting;
using Ocelot.Configuration.Provider;
using Microsoft.Extensions.DependencyInjection;

namespace Ocelot.Middleware
{
    using System;
    using System.Threading.Tasks;
    using Authorisation.Middleware;
    using Configuration;
    using Microsoft.AspNetCore.Http;

    public static class OcelotMiddlewareExtensions
    {
        /// <summary>
        /// Registers the Ocelot default middlewares
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseOcelot(this IApplicationBuilder builder)
        {
            builder.UseOcelot(new OcelotMiddlewareConfiguration());
            return builder;
        }

        /// <summary>
        /// Registers the Ocelot admin portal
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseOcelotPortal(this IApplicationBuilder builder, IHostingEnvironment env)
        {
            var configProvider = builder.ApplicationServices.GetService<IOcelotConfigurationProvider>();

            var configResponse = configProvider.Get();

            if (configResponse.IsError)
            {
                return builder;
            }

            if (AdminPagePathNotSet(configResponse.Data))
            {
                return builder;
            }

            builder.Map(configResponse.Data.AdminstrationSettings.AdminPagePath, map =>
            {
                if (env.IsDevelopment())
                {
                    map.UseDeveloperExceptionPage();
                    map.UseDatabaseErrorPage();
                    map.UseBrowserLink();
                }
                else
                {
                    map.UseExceptionHandler("/Home/Error");
                }

                map.UseStaticFiles();

                map.UseIdentity();

                // Add external authentication middleware below. To configure them please see http://go.microsoft.com/fwlink/?LinkID=532715

                map.UseMvc(routes =>
                {
                    routes.MapRoute(
                        name: "default",
                        template: "{controller=Home}/{action=Index}/{id?}");
                });
            });
            return builder;
        }

        private static bool AdminPagePathNotSet(IOcelotConfiguration config)
        {
            return string.IsNullOrEmpty(config.AdminstrationSettings?.AdminPagePath);
        }

        /// <summary>
        /// Registers Ocelot with a combination of default middlewares and optional middlewares in the configuration
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="middlewareConfiguration"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseOcelot(this IApplicationBuilder builder, OcelotMiddlewareConfiguration middlewareConfiguration)
        {
            // This is registered to catch any global exceptions that are not handled
            builder.UseExceptionHandlerMiddleware();

            // Allow the user to respond with absolutely anything they want.
            builder.UseIfNotNull(middlewareConfiguration.PreErrorResponderMiddleware);

            // This is registered first so it can catch any errors and issue an appropriate response
            builder.UseResponderMiddleware();

            // Then we get the downstream route information
            builder.UseDownstreamRouteFinderMiddleware();

            // Now we can look for the requestId
            builder.UseRequestIdMiddleware();

            // Allow pre authentication logic. The idea being people might want to run something custom before what is built in.
            builder.UseIfNotNull(middlewareConfiguration.PreAuthenticationMiddleware);

            // Now we know where the client is going to go we can authenticate them.
            // We allow the ocelot middleware to be overriden by whatever the
            // user wants
            if (middlewareConfiguration.AuthenticationMiddleware == null)
            {
                builder.UseAuthenticationMiddleware();
            }
            else
            {
                builder.Use(middlewareConfiguration.AuthenticationMiddleware);
            }

            // The next thing we do is look at any claims transforms in case this is important for authorisation
            builder.UseClaimsBuilderMiddleware();

            // Allow pre authorisation logic. The idea being people might want to run something custom before what is built in.
            builder.UseIfNotNull(middlewareConfiguration.PreAuthorisationMiddleware);

            // Now we have authenticated and done any claims transformation we 
            // can authorise the request
            // We allow the ocelot middleware to be overriden by whatever the
            // user wants
            if (middlewareConfiguration.AuthorisationMiddleware == null)
            {
                builder.UseAuthorisationMiddleware();
            }
            else
            {
                builder.Use(middlewareConfiguration.AuthorisationMiddleware);
            }

            // Now we can run any header transformation logic
            builder.UseHttpRequestHeadersBuilderMiddleware();

            // Allow the user to implement their own query string manipulation logic
            builder.UseIfNotNull(middlewareConfiguration.PreQueryStringBuilderMiddleware);

            // Now we can run any query string transformation logic
            builder.UseQueryStringBuilderMiddleware();

            // This takes the downstream route we retrieved earlier and replaces any placeholders with the variables that should be used
            builder.UseDownstreamUrlCreatorMiddleware();

            // Not sure if this is the best place for this but we use the downstream url 
            // as the basis for our cache key.
            builder.UseOutputCacheMiddleware();

            // Everything should now be ready to build or HttpRequest
            builder.UseHttpRequestBuilderMiddleware();

            //We fire off the request and set the response on the scoped data repo
            builder.UseHttpRequesterMiddleware();

            return builder;
        }

        private static void UseIfNotNull(this IApplicationBuilder builder, Func<HttpContext, Func<Task>, Task> middleware)
        {
            if (middleware != null)
            {
                builder.Use(middleware);
            }
        }
    }
}
