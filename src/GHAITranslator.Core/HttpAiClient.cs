using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GHAITranslator.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// HttpWebRequest / WebRequest are obsolete in net6+ but still the only
// portable HTTP API available in netstandard2.0 / net48. Suppress the
// obsolete warning rather than pull a System.Net.Http polyfill that's
// been deprecated for years.
#pragma warning disable SYSLIB0014

namespace GHAITranslator.Core;

/// <summary>
/// OpenAI-compatible chat-completions HTTP client. Works with OpenAI,
/// Qwen, DeepSeek, and any custom endpoint that implements the
/// <c>POST /chat/completions</c> JSON shape.
///
/// Uses <see cref="HttpWebRequest"/> instead of <c>HttpClient</c> so the
/// library stays portable across netstandard2.0 / net48 / net7.0-windows
/// without a per-target NuGet dance.
/// </summary>
public sealed class HttpAiClient : IAiClient, IDisposable
{
    private readonly AiProviderConfig _cfg;
    private readonly PromptBuilder _prompts;

    public HttpAiClient(AiProviderConfig cfg, PromptBuilder? prompts = null)
    {
        _cfg = cfg ?? new AiProviderConfig();
        _prompts = prompts ?? new PromptBuilder();
    }

    public Task<TranslationEntry?> TranslateAsync(ComponentInfo info, CancellationToken ct = default)
    {
        if (info == null || string.IsNullOrEmpty(info.OriginalName))
            return Task.FromResult<TranslationEntry?>(null);
        if (string.IsNullOrEmpty(_cfg.ApiKey))
        {
            Log.Warn("HttpAiClient: API key is empty, skipping translation.");
            return Task.FromResult<TranslationEntry?>(null);
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
            var body = JsonConvert.SerializeObject(payload);
            var req = (HttpWebRequest)WebRequest.Create(_cfg.Endpoint);
            req.Method = "POST";
            req.ContentType = "application/json; charset=utf-8";
            req.Headers.Add("Authorization", $"Bearer {_cfg.ApiKey}");
            req.Timeout = Math.Max(5, _cfg.TimeoutSeconds) * 1000;
            req.ReadWriteTimeout = req.Timeout;

            using (var stream = req.GetRequestStream())
            {
                var bytes = Encoding.UTF8.GetBytes(body);
                stream.Write(bytes, 0, bytes.Length);
            }

            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
            {
                var respBody = reader.ReadToEnd();
                return Task.FromResult(ParseResponse(respBody, info));
            }
        }
        catch (WebException wex)
        {
            var respBody = ReadErrorBody(wex);
            Log.Warn($"HttpAiClient {wex.Status}: {Truncate(respBody, 200)}");
            return Task.FromResult<TranslationEntry?>(null);
        }
        catch (Exception ex)
        {
            Log.Warn($"HttpAiClient.TranslateAsync failed: {ex.Message}");
            return Task.FromResult<TranslationEntry?>(null);
        }
    }

    private static string ReadErrorBody(WebException wex)
    {
        try
        {
            using var stream = wex.Response?.GetResponseStream();
            if (stream == null) return "";
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }
        catch
        {
            return "";
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

    private static string StripCodeFence(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var t = s!.Trim();
        if (t.StartsWith("```"))
        {
            var firstNewline = t.IndexOf('\n');
            if (firstNewline >= 0) t = t.Substring(firstNewline + 1);
            if (t.EndsWith("```")) t = t.Substring(0, t.Length - 3);
        }
        return t.Trim();
    }

    private static string Truncate(string s, int max) =>
        s == null ? "" : (s.Length <= max ? s : s.Substring(0, max) + "…");

    public void Dispose() { /* HttpWebRequest has no managed lifetime */ }
}