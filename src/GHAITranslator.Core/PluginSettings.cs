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

    /// <summary>
    /// Master switch. When false, the renderer does nothing regardless of
    /// <see cref="Mode"/> — the user has fully turned translation off.
    /// </summary>
    public bool EnableTranslation { get; set; } = true;

    /// <summary>
    /// Active display mode for translated component labels. Drives what the
    /// component's <c>Name</c> / <c>NickName</c> properties are rewritten to.
    /// Defaults to <see cref="LanguageMode.Chinese"/> (GHChinese-style).
    /// </summary>
    public LanguageMode Mode { get; set; } = LanguageMode.Chinese;

    public float LabelFontSize { get; set; } = 9f;

    /// <summary>ARGB int, written to the label text colour. Black by default.</summary>
    public int LabelTextColorArgb { get; set; } = unchecked((int)0xFF000000);

    public int LabelBackgroundArgb { get; set; } = unchecked((int)0xE6FFFFFF);

    public bool ShowDescriptionOnHover { get; set; } = true;
}

/// <summary>
/// How translated components are rendered on the Grasshopper canvas.
/// Serialised as an int by Newtonsoft (so renumbering enum members is a
/// breaking change — always append, never reorder).
/// </summary>
public enum LanguageMode
{
    /// <summary>
    /// Show only the Chinese translation. The component's <c>Name</c> and
    /// <c>NickName</c> become the Chinese strings. This is the GHChinese
    /// default behaviour.
    /// </summary>
    Chinese = 0,

    /// <summary>
    /// Show both English and Chinese on the component's title bar as
    /// "Curve / 曲线". The English part comes from the component's
    /// original <c>NickName</c> (snapshotted on first translation); the
    /// Chinese part from the dictionary.
    /// </summary>
    Bilingual = 1,

    /// <summary>
    /// Don't translate at all. Component labels show their original English
    /// text. Translation still happens in the background so flipping back
    /// to Chinese/Bilingual is instant.
    /// </summary>
    English = 2,
}
