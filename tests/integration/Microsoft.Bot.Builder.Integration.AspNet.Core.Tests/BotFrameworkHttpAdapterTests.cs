﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Schema;
using Microsoft.Rest.Serialization;
using Moq;
using Moq.Protected;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Bot.Builder.Integration.AspNet.Core.Tests
{
    public class BotFrameworkHttpAdapterTests
    {
        [Fact]
        public async Task BasicMessageActivity()
        {
            // Arrange
            var headerDictionaryMock = new Mock<IHeaderDictionary>();
            headerDictionaryMock.Setup(h => h[It.Is<string>(v => v == "Authorization")]).Returns<string>(null);

            var httpRequestMock = new Mock<HttpRequest>();
            httpRequestMock.Setup(r => r.Body).Returns(CreateMessageActivityStream());
            httpRequestMock.Setup(r => r.Headers).Returns(headerDictionaryMock.Object);

            var httpResponseMock = new Mock<HttpResponse>();

            var botMock = new Mock<IBot>();
            botMock.Setup(b => b.OnTurnAsync(It.IsAny<TurnContext>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Act
            var adapter = new BotFrameworkHttpAdapter();
            await adapter.ProcessAsync(httpRequestMock.Object, httpResponseMock.Object, botMock.Object);

            // Assert
            botMock.Verify(m => m.OnTurnAsync(It.Is<TurnContext>(tc => true), It.Is<CancellationToken>(ct => true)), Times.Once());
        }

        [Fact]
        public async Task InvokeActivity()
        {
            // Arrange
            var headerDictionaryMock = new Mock<IHeaderDictionary>();
            headerDictionaryMock.Setup(h => h[It.Is<string>(v => v == "Authorization")]).Returns<string>(null);

            var httpRequestMock = new Mock<HttpRequest>();
            httpRequestMock.Setup(r => r.Body).Returns(CreateInvokeActivityStream());
            httpRequestMock.Setup(r => r.Headers).Returns(headerDictionaryMock.Object);

            var response = new MemoryStream();
            var httpResponseMock = new Mock<HttpResponse>();
            httpResponseMock.Setup(r => r.Body).Returns(response);

            var bot = new InvokeResponseBot();

            // Act
            var adapter = new BotFrameworkHttpAdapter();
            await adapter.ProcessAsync(httpRequestMock.Object, httpResponseMock.Object, bot);

            // Assert
            using (var stream = new MemoryStream(response.GetBuffer()))
            using (var reader = new StreamReader(stream))
            {
                var s = reader.ReadToEnd();
                var json = JObject.Parse(s);
                Assert.Equal("im.feeling.really.attacked.right.now", json["quite.honestly"]);
            }
        }

        [Fact]
        public async Task MessageActivityWithHttpClient()
        {
            // Arrange
            var headerDictionaryMock = new Mock<IHeaderDictionary>();
            headerDictionaryMock.Setup(h => h[It.Is<string>(v => v == "Authorization")]).Returns<string>(null);

            var httpRequestMock = new Mock<HttpRequest>();
            httpRequestMock.Setup(r => r.Body).Returns(CreateMessageActivityStream());
            httpRequestMock.Setup(r => r.Headers).Returns(headerDictionaryMock.Object);

            var httpResponseMock = new Mock<HttpResponse>();

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns((HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(CreateInternalHttpResponse()));

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);

            var bot = new MessageBot();

            // Act
            var adapter = new BotFrameworkHttpAdapter(null, null, httpClient, null);
            await adapter.ProcessAsync(httpRequestMock.Object, httpResponseMock.Object, bot);

            // Assert
            mockHttpMessageHandler.Protected().Verify<Task<HttpResponseMessage>>("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }

        private static Stream CreateMessageActivityStream()
        {
            return CreateStream(new Activity
            {
                Type = ActivityTypes.Message,
                Text = "hi",
                ServiceUrl = "http://localhost",
                ChannelId = "ChannelId",
                Conversation = new ConversationAccount { Id = "ConversationId" },
            });
        }

        private static HttpResponseMessage CreateInternalHttpResponse()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(new JObject { { "id", "SendActivityId" } }.ToString());
            return response;
        }

        private static Stream CreateInvokeActivityStream()
        {
            return CreateStream(new Activity { Type = ActivityTypes.Invoke, ServiceUrl = "http://localhost" });
        }

        private static Stream CreateStream(Activity activity)
        {
            string json = SafeJsonConvert.SerializeObject(activity, MessageSerializerSettings.Create());
            var stream = new MemoryStream();
            var textWriter = new StreamWriter(stream);
            textWriter.Write(json);
            textWriter.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        private class InvokeResponseBot : IBot
        {
            public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
            {
                await turnContext.SendActivityAsync(CreateInvokeResponseActivity());
            }

            private static Activity CreateInvokeResponseActivity()
            {
                return new Activity
                {
                    Type = ActivityTypesEx.InvokeResponse,
                    Value = new InvokeResponse
                    {
                        Status = 200,
                        Body = new JObject { { "quite.honestly", "im.feeling.really.attacked.right.now" } },
                    },
                };
            }
        }

        private class MessageBot : IBot
        {
            public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("rage.rage.against.the.dying.of.the.light"));
            }
        }
    }
}
