// Provider registry: every supported LLM endpoint. The HTTP client picks the
// provider by Id at construction time and stamps the right headers / request
// shape. Adding a new provider is a one-line addition here.

using System;

namespace GHAITranslator.Core
{
    /// <summary>
    /// Logical name for a model provider. The default endpoint + default model
    /// are looked up from <see cref="ProviderRegistry"/>; users can still
    /// override either field manually in the settings panel.
    /// </summary>
    public enum AiProvider
    {
        Qwen = 0,          // 通义千问 DashScope
        DeepSeek = 1,      // DeepSeek
        OpenAi = 2,        // OpenAI
        Custom = 99        // 用户自定义端点
    }

    public sealed class ProviderDescriptor
    {
        public AiProvider Id { get; }
        public string DisplayName { get; }
        public string DefaultEndpoint { get; }
        public string DefaultModel { get; }
        public string KeyPlaceholder { get; }
        public string HelpUrl { get; }

        public ProviderDescriptor(
            AiProvider id,
            string displayName,
            string defaultEndpoint,
            string defaultModel,
            string keyPlaceholder,
            string helpUrl)
        {
            Id = id;
            DisplayName = displayName;
            DefaultEndpoint = defaultEndpoint;
            DefaultModel = defaultModel;
            KeyPlaceholder = keyPlaceholder;
            HelpUrl = helpUrl;
        }
    }

    public static class ProviderRegistry
    {
        public static readonly ProviderDescriptor Qwen = new(
            AiProvider.Qwen,
            "通义千问 (Qwen)",
            "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions",
            "qwen-plus",
            "sk-... DashScope API Key",
            "https://dashscope.console.aliyun.com/apiKey");

        public static readonly ProviderDescriptor DeepSeek = new(
            AiProvider.DeepSeek,
            "DeepSeek",
            "https://api.deepseek.com/v1/chat/completions",
            "deepseek-chat",
            "sk-... DeepSeek API Key",
            "https://platform.deepseek.com/api_keys");

        public static readonly ProviderDescriptor OpenAi = new(
            AiProvider.OpenAi,
            "OpenAI",
            "https://api.openai.com/v1/chat/completions",
            "gpt-4o-mini",
            "sk-... OpenAI API Key",
            "https://platform.openai.com/api-keys");

        public static readonly ProviderDescriptor Custom = new(
            AiProvider.Custom,
            "自定义 (OpenAI 兼容)",
            "",
            "",
            "Bearer Token",
            "");

        public static ProviderDescriptor Resolve(AiProvider id) => id switch
        {
            AiProvider.Qwen => Qwen,
            AiProvider.DeepSeek => DeepSeek,
            AiProvider.OpenAi => OpenAi,
            AiProvider.Custom => Custom,
            _ => Qwen
        };

        public static ProviderDescriptor[] All => new[] { Qwen, DeepSeek, OpenAi, Custom };

        /// <summary>
        /// Apply provider defaults into the settings when the user switches
        /// provider. Does NOT overwrite a custom endpoint that the user has
        /// already set for this provider (we keep it if non-empty).
        /// </summary>
        public static void ApplyDefaults(PluginSettings settings, AiProvider id)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            var desc = Resolve(id);

            // If the user is switching TO this provider for the first time, we
            // override endpoint/model. If they have an existing key, we keep it.
            if (id != settings.Provider)
            {
                settings.Provider = id;
                settings.ApiEndpoint = desc.DefaultEndpoint;
                settings.ModelName = desc.DefaultModel;
            }
            else
            {
                // re-apply defaults for blank fields (e.g. fresh settings)
                if (string.IsNullOrWhiteSpace(settings.ApiEndpoint))
                    settings.ApiEndpoint = desc.DefaultEndpoint;
                if (string.IsNullOrWhiteSpace(settings.ModelName))
                    settings.ModelName = desc.DefaultModel;
            }
        }
    }
}
