using GHAITranslator.Core.Models;

namespace GHAITranslator.Core;

/// <summary>
/// Snapshot of user-visible settings. Persisted to
/// <c>%APPDATA%\GH-AITranslator\settings.json</c>.
/// </summary>
public sealed class PluginSettings
{
    public LanguageMode LanguageMode { get; set; } = LanguageMode.Chinese;
    public bool TranslateOnCanvasOpen { get; set; } = true;
    public bool AutoTranslateNew { get; set; } = true;
    public AiProviderConfig Ai { get; set; } = new();
}