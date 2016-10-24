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
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.DownstreamUrlCreator;
using Ocelot.DownstreamUrlCreator.Middleware;
using Ocelot.DownstreamUrlCreator.UrlTemplateReplacer;
using Ocelot.Errors;
using Ocelot.Infrastructure.Provider;
using Ocelot.Responses;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.DownstreamUrlCreator
{
    public class DownstreamUrlCreatorMiddlewareTests : IDisposable
    {
        private readonly Mock<IDownstreamUrlTemplateVariableReplacer> _downstreamUrlTemplateVariableReplacer;
        private readonly string _url;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private Response<DownstreamRoute> _downstreamRoute;
        private HttpResponseMessage _result;
        private OkResponse<DownstreamUrl> _downstreamUrl;
        private readonly Mock<IDataProvider<DownstreamRoute>> _provider;
        private readonly Mock<IDataProvider<List<Error>>> _errorProvider;
        private readonly Mock<IDataProvider<DownstreamUrl>> _urlProvider;

        public DownstreamUrlCreatorMiddlewareTests()
        {
            _url = "http://localhost:51879";
            _downstreamUrlTemplateVariableReplacer = new Mock<IDownstreamUrlTemplateVariableReplacer>();
            _provider = new Mock<IDataProvider<DownstreamRoute>>();
            _errorProvider = new Mock<IDataProvider<List<Error>>>();
            _urlProvider = new Mock<IDataProvider<DownstreamUrl>>();

            var builder = new WebHostBuilder()
              .ConfigureServices(x =>
              {
                  x.AddSingleton(_urlProvider.Object);
                  x.AddSingleton(_errorProvider.Object);
                  x.AddSingleton(_provider.Object);
                  x.AddSingleton(_downstreamUrlTemplateVariableReplacer.Object);
              })
              .UseUrls(_url)
              .UseKestrel()
              .UseContentRoot(Directory.GetCurrentDirectory())
              .UseIISIntegration()
              .UseUrls(_url)
              .Configure(app =>
              {
                  app.UseDownstreamUrlCreatorMiddleware();
              });

            _server = new TestServer(builder);
            _client = _server.CreateClient();
        }

        [Fact]
        public void happy_path()
        {
            this.Given(x => x.GivenTheDownStreamRouteIs(new DownstreamRoute(new List<TemplateVariableNameAndValue>(), new ReRouteBuilder().WithDownstreamTemplate("any old string").Build())))
                .And(x => x.TheUrlReplacerReturns("any old string"))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheScopedDataRepositoryIsCalledCorrectly())
                .BDDfy();
        }

        private void TheUrlReplacerReturns(string downstreamUrl)
        {
            _downstreamUrl = new OkResponse<DownstreamUrl>(new DownstreamUrl(downstreamUrl));
            _downstreamUrlTemplateVariableReplacer
                .Setup(x => x.ReplaceTemplateVariables(It.IsAny<DownstreamRoute>()))
                .Returns(_downstreamUrl);
        }

        private void ThenTheScopedDataRepositoryIsCalledCorrectly()
        {
            _urlProvider
                .Verify(x => x.Set(_downstreamUrl.Data), Times.Once());
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
