using Ocelot.Errors;
using Ocelot.Infrastructure.Provider;

namespace Ocelot.UnitTests.ClaimsBuilder
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Ocelot.ClaimsBuilder;
    using Ocelot.ClaimsBuilder.Middleware;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Builder;
    using Ocelot.DownstreamRouteFinder;
    using Ocelot.DownstreamRouteFinder.UrlMatcher;
    using Responses;
    using TestStack.BDDfy;
    using Xunit;

    public class ClaimsBuilderMiddlewareTests : IDisposable
    {
        private readonly Mock<IAddClaimsToRequest> _addHeaders;
        private readonly string _url;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private Response<DownstreamRoute> _downstreamRoute;
        private HttpResponseMessage _result;
        private readonly Mock<IDataProvider<DownstreamRoute>> _provider;
        private readonly Mock<IDataProvider<List<Error>>> _errorProvider;

        public ClaimsBuilderMiddlewareTests()
        {
            _url = "http://localhost:51879";
            _addHeaders = new Mock<IAddClaimsToRequest>();
            _provider = new Mock<IDataProvider<DownstreamRoute>>();
            _errorProvider = new Mock<IDataProvider<List<Error>>>();

            var builder = new WebHostBuilder()
              .ConfigureServices(x =>
              {
                  x.AddSingleton(_errorProvider.Object);
                  x.AddSingleton(_provider.Object);
                  x.AddSingleton(_addHeaders.Object);
              })
              .UseUrls(_url)
              .UseKestrel()
              .UseContentRoot(Directory.GetCurrentDirectory())
              .UseIISIntegration()
              .UseUrls(_url)
              .Configure(app =>
              {
                  app.UseClaimsBuilderMiddleware();
              });

            _server = new TestServer(builder);
            _client = _server.CreateClient();
        }

        [Fact]
        public void happy_path()
        {
            var downstreamRoute = new DownstreamRoute(new List<TemplateVariableNameAndValue>(),
                new ReRouteBuilder()
                    .WithDownstreamTemplate("any old string")
                    .WithClaimsToClaims(new List<ClaimToThing>
                    {
                        new ClaimToThing("sub", "UserType", "|", 0)
                    })
                    .Build());

            this.Given(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .And(x => x.GivenTheAddClaimsToRequestReturns())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheClaimsToRequestIsCalledCorrectly())
                .BDDfy();
        }

        private void GivenTheAddClaimsToRequestReturns()
        {
            _addHeaders
                .Setup(x => x.SetClaimsOnContext(It.IsAny<List<ClaimToThing>>(),
                It.IsAny<HttpContext>()))
                .Returns(new OkResponse());
        }

        private void ThenTheClaimsToRequestIsCalledCorrectly()
        {
            _addHeaders
                .Verify(x => x.SetClaimsOnContext(It.IsAny<List<ClaimToThing>>(),
                It.IsAny<HttpContext>()), Times.Once);
        }

        private void WhenICallTheMiddleware()
        {
            _result = _client.GetAsync(_url).Result;
        }

        private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute)
        {
            _downstreamRoute = new OkResponse<DownstreamRoute>(downstreamRoute);
            _provider
                .Setup(x => x.Get())
                .Returns(_downstreamRoute);
        }

        public void Dispose()
        {
            _client.Dispose();
            _server.Dispose();
        }
    }
}
