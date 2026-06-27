namespace GHAITranslator.Core.Models;

/// <summary>
/// Configuration for the AI translation provider. Stored in
/// <c>%APPDATA%\GH-AITranslator\settings.json</c>.
/// </summary>
public sealed class AiProviderConfig
{
    public string Provider { get; set; } = "OpenAI";   // OpenAI | Qwen | DeepSeek | Custom
    public string ApiKey   { get; set; } = "";
    public string Endpoint { get; set; } = "https://api.openai.com/v1/chat/completions";
    public string Model    { get; set; } = "gpt-4o-mini";
    public int    TimeoutSeconds { get; set; } = 30;
}