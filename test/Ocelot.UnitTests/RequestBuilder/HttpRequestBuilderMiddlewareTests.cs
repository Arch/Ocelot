using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Ocelot.DownstreamUrlCreator;
using Ocelot.Errors;
using Ocelot.Infrastructure.Provider;
using Ocelot.RequestBuilder;
using Ocelot.RequestBuilder.Builder;
using Ocelot.RequestBuilder.Middleware;
using Ocelot.Responses;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.RequestBuilder
{
    public class HttpRequestBuilderMiddlewareTests : IDisposable
    {
        private readonly Mock<IRequestBuilder> _requestBuilder;
        private readonly Mock<IDataProvider<List<Error>>> _errorProvider;  
        private readonly string _url;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private HttpResponseMessage _result;
        private OkResponse<Request> _request;
        private OkResponse<DownstreamUrl> _downstreamUrl;
        private readonly Mock<IDataProvider<DownstreamUrl>> _urlProvider;
        private readonly Mock<IDataProvider<Request>> _requestDataProvider;

        public HttpRequestBuilderMiddlewareTests()
        {
            _url = "http://localhost:51879";
            _requestBuilder = new Mock<IRequestBuilder>();
            _errorProvider = new Mock<IDataProvider<List<Error>>>();
            _urlProvider = new Mock<IDataProvider<DownstreamUrl>>();
            _requestDataProvider = new Mock<IDataProvider<Request>>();

            var builder = new WebHostBuilder()
              .ConfigureServices(x =>
              {
                  x.AddSingleton(_requestDataProvider.Object);
                  x.AddSingleton(_urlProvider.Object);
                  x.AddSingleton(_errorProvider.Object);
                  x.AddSingleton(_requestBuilder.Object);
              })
              .UseUrls(_url)
              .UseKestrel()
              .UseContentRoot(Directory.GetCurrentDirectory())
              .UseIISIntegration()
              .UseUrls(_url)
              .Configure(app =>
              {
                  app.UseHttpRequestBuilderMiddleware();
              });

            _server = new TestServer(builder);
            _client = _server.CreateClient();
        }

        [Fact]
        public void happy_path()
        {
            this.Given(x => x.GivenTheDownStreamUrlIs("any old string"))
                .And(x => x.GivenTheRequestBuilderReturns(new Request(new HttpRequestMessage(), new CookieContainer())))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheScopedDataRepositoryIsCalledCorrectly())
                .BDDfy();
        }

        private void GivenTheRequestBuilderReturns(Request request)
        {
            _request = new OkResponse<Request>(request);
            _requestBuilder
                .Setup(x => x.Build(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<IHeaderDictionary>(),
                It.IsAny<IRequestCookieCollection>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(_request);
        }

        private void ThenTheScopedDataRepositoryIsCalledCorrectly()
        {
            _requestDataProvider
                .Verify(x => x.Set(_request.Data), Times.Once());
        }

        private void WhenICallTheMiddleware()
        {
            _result = _client.GetAsync(_url).Result;
        }

        private void GivenTheDownStreamUrlIs(string downstreamUrl)
        {
            _downstreamUrl = new OkResponse<DownstreamUrl>(new DownstreamUrl(downstreamUrl));
            _urlProvider
                .Setup(x => x.Get())
                .Returns(_downstreamUrl);
        }

        public void Dispose()
        {
            _client.Dispose();
            _server.Dispose();
        }
    }
}
