using GHAITranslator.Core;
using Xunit;

namespace GHAITranslator.Tests
{
    public class ProviderRegistryTests
    {
        [Fact]
        public void Resolve_KnownProviders()
        {
            Assert.Equal("通义千问 (Qwen)", ProviderRegistry.Qwen.DisplayName);
            Assert.Equal("DeepSeek", ProviderRegistry.DeepSeek.DisplayName);
            Assert.Equal("OpenAI", ProviderRegistry.OpenAi.DisplayName);
            Assert.Equal(AiProvider.Qwen, ProviderRegistry.Resolve(AiProvider.Qwen).Id);
            Assert.Equal(AiProvider.DeepSeek, ProviderRegistry.Resolve(AiProvider.DeepSeek).Id);
            Assert.Equal(AiProvider.OpenAi, ProviderRegistry.Resolve(AiProvider.OpenAi).Id);
            Assert.Equal(AiProvider.Custom, ProviderRegistry.Resolve(AiProvider.Custom).Id);
        }

        [Fact]
        public void Resolve_UnknownProvider_FallsBackToQwen()
        {
            var desc = ProviderRegistry.Resolve((AiProvider)999);
            Assert.Equal(AiProvider.Qwen, desc.Id);
        }

        [Fact]
        public void AllProviders_HaveNonEmptyEndpoint()
        {
            // "Custom" is allowed to have empty endpoint (user fills it).
            foreach (var p in ProviderRegistry.All)
            {
                if (p.Id == AiProvider.Custom) continue;
                Assert.False(string.IsNullOrWhiteSpace(p.DefaultEndpoint), $"{p.Id} missing endpoint");
                Assert.False(string.IsNullOrWhiteSpace(p.DefaultModel), $"{p.Id} missing model");
            }
        }

        [Fact]
        public void ApplyDefaults_SwitchesEndpointAndModel_OnProviderChange()
        {
            var s = new PluginSettings(); // starts as Qwen
            Assert.Equal("https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions", s.ApiEndpoint);

            ProviderRegistry.ApplyDefaults(s, AiProvider.DeepSeek);
            Assert.Equal(AiProvider.DeepSeek, s.Provider);
            Assert.Equal("https://api.deepseek.com/v1/chat/completions", s.ApiEndpoint);
            Assert.Equal("deepseek-chat", s.ModelName);
        }

        [Fact]
        public void ApplyDefaults_DoesNotClobber_EndpointOnReselect()
        {
            var s = new PluginSettings();
            ProviderRegistry.ApplyDefaults(s, AiProvider.OpenAi);
            // user customizes endpoint, then re-selects OpenAi — should keep custom endpoint
            s.ApiEndpoint = "https://my-proxy.example.com/v1/chat/completions";
            ProviderRegistry.ApplyDefaults(s, AiProvider.OpenAi);
            Assert.Equal("https://my-proxy.example.com/v1/chat/completions", s.ApiEndpoint);
        }
    }
}
