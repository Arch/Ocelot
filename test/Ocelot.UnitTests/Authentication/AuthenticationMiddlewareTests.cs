using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Ocelot.Authentication.Handler.Factory;
using Ocelot.Authentication.Middleware;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Errors;
using Ocelot.Infrastructure.Provider;
using Ocelot.Responses;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Authentication
{
    public class AuthenticationMiddlewareTests : IDisposable
    {
        private readonly Mock<IDataProvider<DownstreamRoute>> _provider;
        private readonly Mock<IDataProvider<List<Error>>> _errorProvider;
        private readonly Mock<IAuthenticationHandlerFactory> _authFactory;
        private readonly string _url;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private HttpResponseMessage _result;
        private OkResponse<DownstreamRoute> _downstreamRoute;

        public AuthenticationMiddlewareTests()
        {
            _url = "http://localhost:51879";
            _authFactory = new Mock<IAuthenticationHandlerFactory>();
            _provider = new Mock<IDataProvider<DownstreamRoute>>();
            _errorProvider = new Mock<IDataProvider<List<Error>>>();
            var builder = new WebHostBuilder()
              .ConfigureServices(x =>
              {
                  x.AddSingleton(_errorProvider.Object);
                  x.AddSingleton(_provider.Object);
                  x.AddSingleton(_authFactory.Object);
              })
              .UseUrls(_url)
              .UseKestrel()
              .UseContentRoot(Directory.GetCurrentDirectory())
              .UseIISIntegration()
              .UseUrls(_url)
              .Configure(app =>
              {
                  app.UseAuthenticationMiddleware();
              });

            _server = new TestServer(builder);
            _client = _server.CreateClient();
        }

        [Fact]
        public void happy_path()
        {
            this.Given(x => x.GivenTheDownStreamRouteIs(new DownstreamRoute(new List<TemplateVariableNameAndValue>(), new ReRouteBuilder().Build())))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenNoExceptionsAreThrown())
                .BDDfy();
        }

        private void ThenNoExceptionsAreThrown()
        {
            //todo not suck
        }

        private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute)
        {
            _downstreamRoute = new OkResponse<DownstreamRoute>(downstreamRoute);
            _provider
                .Setup(x => x.Get())
                .Returns(_downstreamRoute);
        }

        private void WhenICallTheMiddleware()
        {
            _result = _client.GetAsync(_url).Result;
        }


        public void Dispose()
        {
            _client.Dispose();
            _server.Dispose();
        }
    }
}
