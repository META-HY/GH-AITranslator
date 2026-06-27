using System.Collections.Generic;
using Newtonsoft.Json;

namespace GHAITranslator.Core.Models;

/// <summary>
/// A single translation entry. Source: builtin | user | ai.
///
/// Field history:
///   * Original: Name (Chinese full name) + Description + Inputs/Outputs dicts.
///   * Added in v2: NickName (Chinese short name) + NickNameEn (English short
///     name, optional) + NameEn (English full name, optional). All new fields
///     default to "" so dictionaries written by v1 still load — Newtonsoft
///     leaves missing JSON keys at the property's default value.
///
/// The display layer combines Name/NickName/NameEn/NickNameEn with the
/// active <see cref="LanguageMode"/> to produce the final string the
/// Grasshopper component receives.
/// </summary>
public class TranslationEntry
{
    /// <summary>Chinese full component name. Empty == not yet translated.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Chinese short name shown on the GH canvas title bar (the "nickname").
    /// Falls back to <see cref="Name"/> when empty so the GHChinese-style
    /// effect always has something to display.
    /// </summary>
    public string NickName { get; set; } = string.Empty;

    /// <summary>English full name. Optional — used in bilingual mode.</summary>
    public string NameEn { get; set; } = string.Empty;

    /// <summary>
    /// English short name shown on the GH canvas title bar in bilingual mode.
    /// Falls back to <see cref="NameEn"/> when empty.
    /// </summary>
    public string NickNameEn { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>English param name -> Chinese display name.</summary>
    public Dictionary<string, string> Inputs { get; set; } = new();

    public Dictionary<string, string> Outputs { get; set; } = new();

    /// <summary>Origin of the entry.</summary>
    public string Source { get; set; } = GHAITranslator.Core.TranslationSource.User;

    /// <summary>Last update timestamp (ISO 8601). Used for AI cache eviction.</summary>
    public string UpdatedAt { get; set; } = string.Empty;

    /// <summary>
    /// True when the entry has at least one piece of Chinese text. Entries
    /// with no Chinese at all are treated as "no translation available" by
    /// the display layer so the component keeps its original English text
    /// instead of being rewritten to "".
    /// </summary>
    [JsonIgnore]
    public bool HasChinese =>
        !string.IsNullOrEmpty(Name) || !string.IsNullOrEmpty(NickName);
}
