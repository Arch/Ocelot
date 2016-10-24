using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Errors;
using Ocelot.HeaderBuilder;
using Ocelot.HeaderBuilder.Middleware;
using Ocelot.Infrastructure.Provider;
using Ocelot.Responses;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.HeaderBuilder
{
    public class HttpRequestHeadersBuilderMiddlewareTests : IDisposable
    {
        private readonly Mock<IAddHeadersToRequest> _addHeaders;
        private readonly string _url;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private Response<DownstreamRoute> _downstreamRoute;
        private HttpResponseMessage _result;
        private readonly Mock<IDataProvider<DownstreamRoute>> _provider;
        private readonly Mock<IDataProvider<List<Error>>> _errorProvider;

        public HttpRequestHeadersBuilderMiddlewareTests()
        {
            _url = "http://localhost:51879";
            _addHeaders = new Mock<IAddHeadersToRequest>();
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
                  app.UseHttpRequestHeadersBuilderMiddleware();
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
                    .WithClaimsToHeaders(new List<ClaimToThing>
                    {
                        new ClaimToThing("UserId", "Subject", "", 0)
                    })
                    .Build());

            this.Given(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .And(x => x.GivenTheAddHeadersToRequestReturns("123"))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheAddHeadersToRequestIsCalledCorrectly())
                .BDDfy();
        }

        private void GivenTheAddHeadersToRequestReturns(string claimValue)
        {
            _addHeaders
                .Setup(x => x.SetHeadersOnContext(It.IsAny<List<ClaimToThing>>(), 
                It.IsAny<HttpContext>()))
                .Returns(new OkResponse());
        }

        private void ThenTheAddHeadersToRequestIsCalledCorrectly()
        {
            _addHeaders
                .Verify(x => x.SetHeadersOnContext(It.IsAny<List<ClaimToThing>>(),
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
