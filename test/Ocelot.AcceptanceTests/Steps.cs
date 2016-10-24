﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using Ocelot.Configuration.Yaml;
using Ocelot.ManualTest;
using Shouldly;
using YamlDotNet.Serialization;

namespace Ocelot.AcceptanceTests
{
    public class Steps : IDisposable
    {
        private TestServer _ocelotServer;
        private HttpClient _ocelotClient;
        private HttpResponseMessage _response;
        private HttpContent _postContent;
        private BearerToken _token;
        public HttpClient OcelotClient => _ocelotClient;

        public void GivenThereIsAConfiguration(YamlConfiguration yamlConfiguration)
        {
            var configurationPath = TestConfiguration.ConfigurationPath;

            var serializer = new Serializer();

            if (File.Exists(configurationPath))
            {
                File.Delete(configurationPath);
            }

            using (TextWriter writer = File.CreateText(configurationPath))
            {
                serializer.Serialize(writer, yamlConfiguration);
            }
        }

        public void GivenThereIsAConfiguration(YamlConfiguration yamlConfiguration, string configurationPath)
        {
            var serializer = new Serializer();

            if (File.Exists(configurationPath))
            {
                File.Delete(configurationPath);
            }

            using (TextWriter writer = File.CreateText(configurationPath))
            {
                serializer.Serialize(writer, yamlConfiguration);
            }
        }

        /// <summary>
        /// This is annoying cos it should be in the constructor but we need to set up the yaml file before calling startup so its a step.
        /// </summary>
        public void GivenOcelotIsRunning()
        {
            _ocelotServer = new TestServer(new WebHostBuilder()
                .UseStartup<Startup>());

            _ocelotClient = _ocelotServer.CreateClient();
        }

        public void GivenIHaveAddedATokenToMyRequest()
        {
            _ocelotClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
        }

        public void GivenIHaveAToken(string url)
        {
            var tokenUrl = $"{url}/connect/token";
            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", "client"),
                new KeyValuePair<string, string>("client_secret", "secret"),
                new KeyValuePair<string, string>("scope", "api"),
                new KeyValuePair<string, string>("username", "test"),
                new KeyValuePair<string, string>("password", "test"),
                new KeyValuePair<string, string>("grant_type", "password")
            };
            var content = new FormUrlEncodedContent(formData);

            using (var httpClient = new HttpClient())
            {
                var response = httpClient.PostAsync(tokenUrl, content).Result;
                response.EnsureSuccessStatusCode();
                var responseContent = response.Content.ReadAsStringAsync().Result;
                _token = JsonConvert.DeserializeObject<BearerToken>(responseContent);
            }
        }

        public void VerifyIdentiryServerStarted(string url)
        {
            using (var httpClient = new HttpClient())
            {
                var response = httpClient.GetAsync($"{url}/.well-known/openid-configuration").Result;
                response.EnsureSuccessStatusCode();
            }
        }


        public void WhenIGetUrlOnTheApiGateway(string url)
        {
            _response = _ocelotClient.GetAsync(url).Result;
        }

        public void WhenIPostUrlOnTheApiGateway(string url)
        {
            _response = _ocelotClient.PostAsync(url, _postContent).Result;
        }

        public void GivenThePostHasContent(string postcontent)
        {
            _postContent = new StringContent(postcontent);
        }

        public void ThenTheResponseBodyShouldBe(string expectedBody)
        {
            _response.Content.ReadAsStringAsync().Result.ShouldBe(expectedBody);
        }

        public void ThenTheStatusCodeShouldBe(HttpStatusCode expectedHttpStatusCode)
        {
            _response.StatusCode.ShouldBe(expectedHttpStatusCode);
        }

        public void Dispose()
        {
            _ocelotClient?.Dispose();
            _ocelotServer?.Dispose();
        }
    }
}
