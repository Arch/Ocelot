using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.DownstreamRouteFinder.Middleware;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Errors;
using Ocelot.Infrastructure.Provider;
using Ocelot.Responses;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.DownstreamRouteFinder
{
    public class DownstreamRouteFinderMiddlewareTests : IDisposable
    {
        private readonly Mock<IDownstreamRouteFinder> _downstreamRouteFinder;
        private readonly string _url;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private Response<DownstreamRoute> _downstreamRoute;
        private HttpResponseMessage _result;
        private Mock<IDataProvider<DownstreamRoute>> _provider;
        private Mock<IDataProvider<List<Error>>> _errorProvider;

        public DownstreamRouteFinderMiddlewareTests()
        {
            _url = "http://localhost:51879";
            _downstreamRouteFinder = new Mock<IDownstreamRouteFinder>();
            _provider = new Mock<IDataProvider<DownstreamRoute>>();
            _errorProvider = new Mock<IDataProvider<List<Error>>>();
            var builder = new WebHostBuilder()
              .ConfigureServices(x =>
              {
                  x.AddSingleton(_errorProvider.Object);
                  x.AddSingleton(_provider.Object);
                  x.AddSingleton(_downstreamRouteFinder.Object);
              })
              .UseUrls(_url)
              .UseKestrel()
              .UseContentRoot(Directory.GetCurrentDirectory())
              .UseIISIntegration()
              .UseUrls(_url)
              .Configure(app =>
              {
                  app.UseDownstreamRouteFinderMiddleware();
              });

            _server = new TestServer(builder);
            _client = _server.CreateClient();
        }

        [Fact]
        public void happy_path()
        {
            this.Given(x => x.GivenTheDownStreamRouteFinderReturns(new DownstreamRoute(new List<TemplateVariableNameAndValue>(), new ReRouteBuilder().WithDownstreamTemplate("any old string").Build())))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheScopedDataRepositoryIsCalledCorrectly())
                .BDDfy();
        }

        private void ThenTheScopedDataRepositoryIsCalledCorrectly()
        {
            _provider
                .Verify(x => x.Set(_downstreamRoute.Data), Times.Once());
        }

        private void WhenICallTheMiddleware()
        {
            _result = _client.GetAsync(_url).Result;
        }

        private void GivenTheDownStreamRouteFinderReturns(DownstreamRoute downstreamRoute)
        {
            _downstreamRoute = new OkResponse<DownstreamRoute>(downstreamRoute);
            _downstreamRouteFinder
                .Setup(x => x.FindDownstreamRoute(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(_downstreamRoute);
        }

        public void Dispose()
        {
            _client.Dispose();
            _server.Dispose();
        }
    }
}
