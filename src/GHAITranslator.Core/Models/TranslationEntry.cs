using Newtonsoft.Json;

namespace GHAITranslator.Core.Models;

/// <summary>
/// One dictionary entry. Five fields are mandatory; English mirrors are
/// optional but recommended so <see cref="LanguageFormatter"/> can render the
/// Bilingual and English modes without re-querying the AI.
/// </summary>
public sealed class TranslationEntry
{
    /// <summary>Lookup key, e.g. <c>"Native_Point"</c>.</summary>
    [JsonProperty("key")]
    public string Key { get; set; } = "";

    /// <summary>Chinese display name, e.g. <c>"点"</c>.</summary>
    [JsonProperty("name")]
    public string Name { get; set; } = "";

    /// <summary>Chinese port label, ≤ 2 chars.</summary>
    [JsonProperty("nick")]
    public string NickName { get; set; } = "";

    /// <summary>Chinese hover-tip. Must end with <c>。</c>.</summary>
    [JsonProperty("desc")]
    public string Description { get; set; } = "";

    /// <summary>Chinese tab name, e.g. <c>"参数"</c>.</summary>
    [JsonProperty("cat")]
    public string Category { get; set; } = "";

    /// <summary>Original English Name (preserved for Bilingual / English modes).</summary>
    [JsonProperty("nameEn", NullValueHandling = NullValueHandling.Ignore)]
    public string? NameEn { get; set; }

    /// <summary>Original English Description.</summary>
    [JsonProperty("descEn", NullValueHandling = NullValueHandling.Ignore)]
    public string? DescriptionEn { get; set; }

    /// <summary>Original English Category.</summary>
    [JsonProperty("catEn", NullValueHandling = NullValueHandling.Ignore)]
    public string? CategoryEn { get; set; }

    /// <summary>
    /// Returns <c>true</c> when every mandatory field is non-empty.
    /// </summary>
    [JsonIgnore]
    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(Key) &&
        !string.IsNullOrWhiteSpace(Name) &&
        !string.IsNullOrWhiteSpace(NickName) &&
        !string.IsNullOrWhiteSpace(Description) &&
        !string.IsNullOrWhiteSpace(Category);
}