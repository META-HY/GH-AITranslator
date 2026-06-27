using GHAITranslator.Core;
using GHAITranslator.Core.Models;
using Xunit;

namespace GHAITranslator.Tests;

public class ProviderRegistryTests
{
    [Fact]
    public void OpenAI_preset_has_endpoint()
    {
        var p = ProviderRegistry.Find("OpenAI");
        Assert.NotNull(p);
        Assert.Contains("openai.com", p!.Endpoint);
    }

    [Fact]
    public void ApplyPreset_overwrites_endpoint_but_preserves_api_key()
    {
        var cfg = new AiProviderConfig { ApiKey = "my-key" };
        var updated = ProviderRegistry.ApplyPreset(cfg, "DeepSeek");
        Assert.Equal("my-key", updated.ApiKey);
        Assert.Contains("deepseek", updated.Endpoint);
        Assert.Equal("DeepSeek", updated.Provider);
    }

    [Fact]
    public void Unknown_preset_returns_input_unchanged()
    {
        var cfg = new AiProviderConfig { Provider = "Mystery", ApiKey = "k", Endpoint = "e", Model = "m" };
        var same = ProviderRegistry.ApplyPreset(cfg, "Mystery");
        Assert.Same(cfg, same);
    }
}