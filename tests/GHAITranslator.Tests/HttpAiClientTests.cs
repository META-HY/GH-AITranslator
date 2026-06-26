// Integration test: stand up a fake OpenAI-compatible server, drive HttpAiClient
// against it, assert the request shape and the parsed response. Runs on Linux
// thanks to System.Net.Http + .NET 8.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GHAITranslator.Core;
using GHAITranslator.Core.AI;
using GHAITranslator.Core.Models;
using Xunit;

namespace GHAITranslator.Tests
{
    public class HttpAiClientTests
    {
        private class FakeHandler : HttpMessageHandler
        {
            public HttpRequestMessage? LastRequest { get; private set; }
            public string? LastBody { get; private set; }
            public string NextResponse { get; set; } = "{}";
            public HttpStatusCode NextStatus { get; set; } = HttpStatusCode.OK;
            public string? ForceException { get; set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                if (request.Content != null)
                    LastBody = await request.Content.ReadAsStringAsync();
                if (ForceException != null) throw new Exception(ForceException);
                return new HttpResponseMessage(NextStatus)
                {
                    Content = new StringContent(NextResponse, Encoding.UTF8, "application/json")
                };
            }
        }

        private static PluginSettings MakeSettings(string endpoint = "https://api.example.com/v1/chat/completions")
        {
            return new PluginSettings
            {
                Provider = AiProvider.Qwen,
                ApiEndpoint = endpoint,
                ApiKey = "test-key",
                ModelName = "qwen-plus"
            };
        }

        [Fact]
        public async Task SendsCorrectShape_OnSuccess()
        {
            var handler = new FakeHandler
            {
                NextResponse = @"{
                    ""choices"": [
                        { ""message"": { ""content"": ""{\""name\"":\""新名字\"",\""description\"":\""d\"",\""inputs\"":{},\""outputs\"":{}}"" } }
                    ]
                }"
            };
            using var client = new HttpAiClient(MakeSettings(), handler);

            var info = new ComponentInfo
            {
                Key = "Kangaroo2_BendGoal",
                Name = "BendGoal",
                PluginName = "Kangaroo2",
                Description = "Bend lines to angle",
                InputParams = new[] { new ParamInfo { Name = "A", Description = "angle" } }
            };
            var entry = await client.TranslateAsync(info);

            Assert.NotNull(entry);
            Assert.Equal("新名字", entry!.Name);
            Assert.Equal(HttpMethod.Post, handler.LastRequest?.Method);
            Assert.Equal("https://api.example.com/v1/chat/completions", handler.LastRequest?.RequestUri?.ToString());
            Assert.NotNull(handler.LastRequest?.Headers.Authorization);
            Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization!.Scheme);
            Assert.Equal("test-key", handler.LastRequest.Headers.Authorization.Parameter);

            // body should be JSON with the right model + role
            Assert.NotNull(handler.LastBody);
            Assert.Contains("\"qwen-plus\"", handler.LastBody!);
            Assert.Contains("\"system\"", handler.LastBody);
            Assert.Contains("\"user\"", handler.LastBody);
            Assert.Contains("Kangaroo2", handler.LastBody);
        }

        [Fact]
        public async Task ReturnsNull_On4xx()
        {
            var handler = new FakeHandler { NextStatus = HttpStatusCode.Unauthorized, NextResponse = @"{""error"":""bad key""}" };
            using var client = new HttpAiClient(MakeSettings(), handler);

            var entry = await client.TranslateAsync(new ComponentInfo { Key = "X", Name = "X" });
            Assert.Null(entry);
        }

        [Fact]
        public async Task ReturnsNull_On5xx()
        {
            var handler = new FakeHandler { NextStatus = HttpStatusCode.InternalServerError, NextResponse = "boom" };
            using var client = new HttpAiClient(MakeSettings(), handler);

            var entry = await client.TranslateAsync(new ComponentInfo { Key = "X", Name = "X" });
            Assert.Null(entry);
        }

        [Fact]
        public async Task ReturnsNull_OnMalformedJson()
        {
            var handler = new FakeHandler { NextResponse = "{not json" };
            using var client = new HttpAiClient(MakeSettings(), handler);

            var entry = await client.TranslateAsync(new ComponentInfo { Key = "X", Name = "X" });
            Assert.Null(entry);
        }

        [Fact]
        public async Task ReturnsNull_OnEmptyContent()
        {
            var handler = new FakeHandler { NextResponse = @"{ ""choices"": [] }" };
            using var client = new HttpAiClient(MakeSettings(), handler);

            var entry = await client.TranslateAsync(new ComponentInfo { Key = "X", Name = "X" });
            Assert.Null(entry);
        }

        [Fact]
        public async Task ReturnsNull_WhenApiKeyMissing()
        {
            var settings = MakeSettings();
            settings.ApiKey = "";
            using var client = new HttpAiClient(settings, new FakeHandler());

            var entry = await client.TranslateAsync(new ComponentInfo { Key = "X", Name = "X" });
            Assert.Null(entry);
        }

        [Fact]
        public async Task HonorsCancellation()
        {
            var handler = new FakeHandler { ForceException = null };
            // override SendAsync to actually wait
            var slowHandler = new SlowHandler();
            using var client = new HttpAiClient(MakeSettings(), slowHandler);

            using var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => client.TranslateAsync(new ComponentInfo { Key = "X", Name = "X" }, cts.Token));
        }

        private class SlowHandler : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await Task.Delay(2000, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}")
                };
            }
        }
    }
}
