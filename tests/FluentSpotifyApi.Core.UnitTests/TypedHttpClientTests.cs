﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentSpotifyApi.Core.Client;
using FluentSpotifyApi.Core.Internal.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;

namespace FluentSpotifyApi.Core.UnitTests
{
    [TestClass]
    public class TypedHttpClientTests
    {
        [TestMethod]
        public async Task ShouldCallHttpClientWrapperSendWithBodyParametersAsync()
        {
            // Arrange
            var uri = new Uri("http://localhost");
            var httpMethod = HttpMethod.Get;
            var queryStringParameters = new { key1 = "test value", key2 = "test 2" };
            var requestBodyParameters = new { bodyKey1 = "body test value?", bodyKey2 = "test 2" };
            var requestHeaders = new[] { new KeyValuePair<string, string>("Header1", "HeaderValue1"), new KeyValuePair<string, string>("Header2", "HeaderValue2") };
            var routeValues = new object[] { "test1&test2", 123 };
            var testResult = new TestResult { Test1 = 12, Test2 = 67 };

            var mock = new Mock<IHttpClientWrapper>();
            mock.Setup(x => x
                .SendAsync(
                    It.Is<HttpRequest<TestResult>>(
                        item =>
                            item.UriFromValuesBuilder.Build() == new Uri("http://localhost/test1%26test2/123?key1=test%20value&key2=test%202") &&  
                            item.HttpMethod == httpMethod),
                    It.IsAny<CancellationToken>()))
                .Returns((Func<HttpRequest<TestResult>, CancellationToken, Task<TestResult>>)((h, c) =>
                  {
                      h.RequestHeaders.Should().Equal(requestHeaders);

                      var requestContent = h.RequestContentProvider();
                      requestContent.Should().BeOfType<FormUrlEncodedContent>().Which.ReadAsStringAsync().Result.Should().Be("bodyKey1=body+test+value%3F&bodyKey2=test+2");

                      var stringContent = new StringContent(JsonConvert.SerializeObject(testResult));
                      return h.ResponseProcessor(stringContent);
                  }));

            // Act
            var result = await new TypedHttpClient(mock.Object).SendAsync<TestResult>(uri, httpMethod, queryStringParameters, requestBodyParameters, requestHeaders, CancellationToken.None, routeValues);

            // Assert   
            result.ShouldBeEquivalentTo(testResult);
        }

        [TestMethod]
        public async Task ShouldCallHttpClientWrapperSendWithJsonBodyAsync()
        {
            // Arrange
            var uri = new Uri("http://localhost");
            var httpMethod = HttpMethod.Get;
            var queryStringParameters = new { key1 = "test value", key2 = "test 2" };
            var requestBody = new Body { Body1 = "Test body", Body2 = 765 };
            var requestHeaders = new[] { new KeyValuePair<string, string>("Header1", "HeaderValue1"), new KeyValuePair<string, string>("Header2", "HeaderValue2") };
            var routeValues = new object[] { "test1&test2", 123 };
            var testResult = new TestResult { Test1 = 12, Test2 = 67 };

            var mock = new Mock<IHttpClientWrapper>();
            mock.Setup(x => x
                .SendAsync(
                    It.Is<HttpRequest<TestResult>>(
                        item => 
                            item.UriFromValuesBuilder.Build() == new Uri("http://localhost/test1%26test2/123?key1=test%20value&key2=test%202") && 
                            item.HttpMethod == httpMethod),
                    It.IsAny<CancellationToken>()))
                .Returns((Func<HttpRequest<TestResult>, CancellationToken, Task<TestResult>>)((h, c) =>
                {
                    h.RequestHeaders.Should().Equal(requestHeaders);

                    var requestContent = h.RequestContentProvider();
                    requestContent.Should().BeOfType<StringContent>().Which.ReadAsStringAsync().Result.Yield().Select(item => JsonConvert.DeserializeObject<Body>(item)).First().ShouldBeEquivalentTo(requestBody);

                    var stringContent = new StringContent(JsonConvert.SerializeObject(testResult));
                    return h.ResponseProcessor(stringContent);
                }));

            // Act
            var result = await new TypedHttpClient(mock.Object).SendWithJsonBodyAsync<TestResult, Body>(uri, httpMethod, queryStringParameters, requestBody, requestHeaders, CancellationToken.None, routeValues);

            // Assert   
            result.ShouldBeEquivalentTo(testResult);
        }

        private class TestResult
        {
            [JsonProperty(PropertyName = "testA")]
            public int Test1 { get; set; }

            [JsonProperty(PropertyName = "testB")]
            public int Test2 { get; set; }
        }

        private class Body
        {
            [JsonProperty(PropertyName = "bodyA")]
            public string Body1 { get; set; }

            [JsonProperty(PropertyName = "bodyB")]
            public int Body2 { get; set; }
        }
    }
}