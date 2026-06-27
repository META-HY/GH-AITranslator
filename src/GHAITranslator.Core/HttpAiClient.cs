using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GHAITranslator.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GHAITranslator.Core;

/// <summary>
/// OpenAI-compatible chat-completions HTTP client. Works with OpenAI,
/// Qwen, DeepSeek, and any custom endpoint that implements the
/// <c>POST /chat/completions</c> JSON shape.
/// </summary>
public sealed class HttpAiClient : IAiClient, IDisposable
{
    private readonly AiProviderConfig _cfg;
    private readonly HttpClient _http;
    private readonly PromptBuilder _prompts;

    public HttpAiClient(AiProviderConfig cfg, PromptBuilder? prompts = null)
    {
        _cfg = cfg ?? new AiProviderConfig();
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(Math.Max(5, _cfg.TimeoutSeconds)) };
        _prompts = prompts ?? new PromptBuilder();
    }

    public async Task<TranslationEntry?> TranslateAsync(ComponentInfo info, CancellationToken ct = default)
    {
        if (info == null || string.IsNullOrEmpty(info.OriginalName)) return null;
        if (string.IsNullOrEmpty(_cfg.ApiKey))
        {
            Log.Warn("HttpAiClient: API key is empty, skipping translation.");
            return null;
        }

        var messages = _prompts.BuildChatMessages(info);
        var payload = new
        {
            model = _cfg.Model,
            messages,
            temperature = 0.1,
            response_format = new { type = "json_object" },
        };

        try
        {
            var req = new HttpRequestMessage(HttpMethod.Post, _cfg.Endpoint);
            req.Headers.Add("Authorization", $"Bearer {_cfg.ApiKey}");
            req.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                Log.Warn($"HttpAiClient {(int)resp.StatusCode}: {Truncate(body, 200)}");
                return null;
            }

            return ParseResponse(body, info);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Warn($"HttpAiClient.TranslateAsync failed: {ex.Message}");
            return null;
        }
    }

    private static TranslationEntry? ParseResponse(string body, ComponentInfo info)
    {
        try
        {
            var jo = JObject.Parse(body);
            var content = jo["choices"]?[0]?["message"]?["content"]?.Value<string>();
            if (string.IsNullOrEmpty(content)) return null;

            // The model is asked to return JSON. Some providers wrap it in
            // ```json ... ``` even with response_format=json_object.
            content = StripCodeFence(content);

            var parsed = JObject.Parse(content);
            return new TranslationEntry
            {
                Key          = info.LookupKey,
                Name         = parsed.Value<string>("name") ?? "",
                NickName     = parsed.Value<string>("nick") ?? "",
                Description  = parsed.Value<string>("desc") ?? "",
                Category     = parsed.Value<string>("cat")  ?? "",
                NameEn       = info.OriginalName,
                DescriptionEn = info.OriginalDescription,
                CategoryEn   = info.Category,
            };
        }
        catch (Exception ex)
        {
            Log.Warn($"HttpAiClient.ParseResponse failed: {ex.Message}");
            return null;
        }
    }

    private static string StripCodeFence(string s)
    {
        s = s.Trim();
        if (s.StartsWith("```"))
        {
            var firstNewline = s.IndexOf('\n');
            if (firstNewline >= 0) s = s.Substring(firstNewline + 1);
            if (s.EndsWith("```")) s = s.Substring(0, s.Length - 3);
        }
        return s.Trim();
    }

    private static string Truncate(string s, int max) =>
        s == null ? "" : (s.Length <= max ? s : s.Substring(0, max) + "…");

    public void Dispose() => _http.Dispose();
}