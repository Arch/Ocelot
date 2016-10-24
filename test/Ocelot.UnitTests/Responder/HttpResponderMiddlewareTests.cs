﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Ocelot.Errors;
using Ocelot.Infrastructure.Provider;
using Ocelot.Responder;
using Ocelot.Responder.Middleware;
using Ocelot.Responses;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Responder
{
    public class HttpResponderMiddlewareTests : IDisposable
    {
        private readonly Mock<IHttpResponder> _responder;
        private readonly Mock<IErrorsToHttpStatusCodeMapper> _codeMapper;
        private readonly string _url;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private HttpResponseMessage _result;
        private OkResponse<HttpResponseMessage> _response;
        private readonly Mock<IDataProvider<List<Error>>> _errorProvider;
        private readonly Mock<IDataProvider<HttpResponseMessage>> _responseDataProvider;

        public HttpResponderMiddlewareTests()
        {
            _url = "http://localhost:51879";
            _responder = new Mock<IHttpResponder>();
            _codeMapper = new Mock<IErrorsToHttpStatusCodeMapper>();
            _errorProvider = new Mock<IDataProvider<List<Error>>>();
            _responseDataProvider = new Mock<IDataProvider<HttpResponseMessage>>();

            var builder = new WebHostBuilder()
              .ConfigureServices(x =>
              {
                  x.AddSingleton(_responseDataProvider.Object);
                  x.AddSingleton(_errorProvider.Object);
                  x.AddSingleton(_codeMapper.Object);
                  x.AddSingleton(_responder.Object);
              })
              .UseUrls(_url)
              .UseKestrel()
              .UseContentRoot(Directory.GetCurrentDirectory())
              .UseIISIntegration()
              .UseUrls(_url)
              .Configure(app =>
              {
                  app.UseHttpResponderMiddleware();
              });

            _server = new TestServer(builder);
            _client = _server.CreateClient();
        }

        [Fact]
        public void happy_path()
        {
            this.Given(x => x.GivenTheHttpResponseMessageIs(new HttpResponseMessage()))
                .And(x => x.GivenThereAreNoPipelineErrors())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenNoErrorsAreThrown())
                .BDDfy();
        }

        private void GivenThereAreNoPipelineErrors()
        {
            _errorProvider
                .Setup(x => x.Get())
                .Returns(new OkResponse<List<Error>>(new List<Error>()));
        }

        private void ThenNoErrorsAreThrown()
        {
            //blahhh :(
        }

        private void WhenICallTheMiddleware()
        {
            _result = _client.GetAsync(_url).Result;
        }

        private void GivenTheHttpResponseMessageIs(HttpResponseMessage response)
        {
            _response = new OkResponse<HttpResponseMessage>(response);
            _responseDataProvider
                .Setup(x => x.Get())
                .Returns(_response);
        }

        public void Dispose()
        {
            _client.Dispose();
            _server.Dispose();
        }
    }
}
