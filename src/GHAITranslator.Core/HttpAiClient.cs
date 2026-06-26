// Concrete IAiClient: POSTs to an OpenAI-compatible chat-completions endpoint
// and parses the JSON content the model is asked to return.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GHAITranslator.Core;
using GHAITranslator.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GHAITranslator.Core.AI
{
    public sealed class HttpAiClient : IAiClient, IDisposable
    {
        private readonly PluginSettings _settings;
        private readonly HttpClient _http;
        private readonly bool _ownsHttp;

        public HttpAiClient(PluginSettings settings) : this(settings, new HttpClient { Timeout = TimeSpan.FromSeconds(15) }, ownsHttp: true) { }

        /// <summary>
        /// Test-friendly constructor that takes a custom <see cref="HttpMessageHandler"/>.
        /// The handler is wrapped in a fresh HttpClient owned by this instance.
        /// </summary>
        public HttpAiClient(PluginSettings settings, HttpMessageHandler handler)
            : this(settings, new HttpClient(handler, disposeHandler: false) { Timeout = TimeSpan.FromSeconds(15) }, ownsHttp: true) { }

        private HttpAiClient(PluginSettings settings, HttpClient http, bool ownsHttp)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _http = http;
            _ownsHttp = ownsHttp;
        }

        public async Task<TranslationEntry?> TranslateAsync(
            ComponentInfo component,
            CancellationToken cancellationToken = default)
        {
            if (component == null) return null;
            if (string.IsNullOrEmpty(_settings.ApiKey))
            {
                Log.Warn("AI translation requested but ApiKey is empty. Set one in the settings panel.");
                return null;
            }
            if (string.IsNullOrEmpty(_settings.ApiEndpoint))
            {
                Log.Warn("AI translation requested but ApiEndpoint is empty.");
                return null;
            }

            try
            {
                var body = new
                {
                    model = _settings.ModelName,
                    temperature = 0.1,
                    response_format = new { type = "json_object" },
                    messages = new[]
                    {
                        new { role = "system", content = PromptBuilder.SystemPrompt },
                        new { role = "user", content = PromptBuilder.BuildUserPrompt(component) }
                    }
                };
                var json = JsonConvert.SerializeObject(body);
                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                using (var req = new HttpRequestMessage(HttpMethod.Post, _settings.ApiEndpoint) { Content = content })
                {
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
                    using (var resp = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false))
                    {
                        if (!resp.IsSuccessStatusCode) return null;
                        var raw = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                        return ParseContent(raw);
                    }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Log.Error("AI translate failed", ex);
                return null;
            }
        }

        private static TranslationEntry? ParseContent(string responseJson)
        {
            try
            {
                var outer = JObject.Parse(responseJson);
                var content = outer["choices"]?[0]?["message"]?["content"]?.Value<string>();
                if (string.IsNullOrEmpty(content)) return null;
                var entry = JsonConvert.DeserializeObject<TranslationEntry>(content);
                if (entry == null || string.IsNullOrEmpty(entry.Name)) return null;
                return entry;
            }
            catch (Exception ex)
            {
                Log.Error("Failed to parse AI response", ex);
                return null;
            }
        }

        public void Dispose()
        {
            if (_ownsHttp) _http.Dispose();
        }
    }
}
