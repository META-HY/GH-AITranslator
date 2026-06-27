using System;
using GHAITranslator.Core.Models;

namespace GHAITranslator.Core;

/// <summary>
/// Known provider presets. Users may also enter a Custom endpoint.
/// </summary>
public static class ProviderRegistry
{
    public sealed class Preset
    {
        public string Name     { get; init; } = "";
        public string Endpoint { get; init; } = "";
        public string Model    { get; init; } = "";
    }

    public static readonly Preset[] Known = new[]
    {
        new Preset { Name = "OpenAI",   Endpoint = "https://api.openai.com/v1/chat/completions",
                                       Model    = "gpt-4o-mini" },
        new Preset { Name = "DeepSeek", Endpoint = "https://api.deepseek.com/v1/chat/completions",
                                       Model    = "deepseek-chat" },
        new Preset { Name = "Qwen",     Endpoint = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions",
                                       Model    = "qwen-plus" },
        new Preset { Name = "Custom",   Endpoint = "", Model = "" },
    };

    public static Preset? Find(string name)
    {
        foreach (var p in Known)
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) return p;
        return null;
    }

    /// <summary>
    /// Apply preset defaults to <paramref name="cfg"/> when the user picks a
    /// known provider. Does not overwrite the user's API key.
    /// </summary>
    public static AiProviderConfig ApplyPreset(AiProviderConfig cfg, string presetName)
    {
        var p = Find(presetName);
        if (p == null) return cfg;
        return new AiProviderConfig
        {
            Provider = p.Name,
            ApiKey   = cfg.ApiKey,
            Endpoint = p.Endpoint,
            Model    = p.Model,
            TimeoutSeconds = cfg.TimeoutSeconds > 0 ? cfg.TimeoutSeconds : 30,
        };
    }
}