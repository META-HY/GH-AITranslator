using System.Collections.Generic;

namespace GHAITranslator.Core.Models;

/// <summary>
/// A single translation entry. Source: builtin | user | ai.
/// </summary>
public class TranslationEntry
{
    /// <summary>Chinese component name (2-6 chars typically).</summary>
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>English param name -> Chinese display name.</summary>
    public Dictionary<string, string> Inputs { get; set; } = new();

    public Dictionary<string, string> Outputs { get; set; } = new();

    /// <summary>Origin of the entry.</summary>
    public string Source { get; set; } = GHAITranslator.Core.TranslationSource.User;

    /// <summary>Last update timestamp (ISO 8601). Used for AI cache eviction.</summary>
    public string UpdatedAt { get; set; } = string.Empty;
}
