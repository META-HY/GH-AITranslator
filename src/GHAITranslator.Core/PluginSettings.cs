namespace GHAITranslator.Core;

/// <summary>
/// All user-visible knobs. Stored as JSON, defaults match the design doc
/// (Qwen-compatible endpoint, qwen-plus, 9pt label font).
/// </summary>
public class PluginSettings
{
    /// <summary>Selected LLM provider. Defaults to Qwen (the design-doc default).</summary>
    public AiProvider Provider { get; set; } = AiProvider.Qwen;

    /// <summary>OpenAI-compatible chat completions URL.</summary>
    public string ApiEndpoint { get; set; } = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions";

    public string ApiKey { get; set; } = string.Empty;

    public string ModelName { get; set; } = "qwen-plus";

    public bool EnableTranslation { get; set; } = true;

    public float LabelFontSize { get; set; } = 9f;

    /// <summary>ARGB int, written to the label text colour. Black by default.</summary>
    public int LabelTextColorArgb { get; set; } = unchecked((int)0xFF000000);

    public int LabelBackgroundArgb { get; set; } = unchecked((int)0xE6FFFFFF);

    public bool ShowDescriptionOnHover { get; set; } = true;

    public TranslationDisplayMode DisplayMode { get; set; } = TranslationDisplayMode.LabelOverlay;
}

public enum TranslationDisplayMode
{
    LabelOverlay = 0,    // 不改原生组件,叠中文标签
    PropertyReplace = 1, // 反射改 NickName,实验性
}
