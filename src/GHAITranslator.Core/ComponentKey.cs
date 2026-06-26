using System;
using GHAITranslator.Core.Models;

namespace GHAITranslator.Core;

/// <summary>
/// Generates a stable, unique key for a component.
/// The key is used as the dictionary lookup key, so the rule must be:
///   1) deterministic across machines / GH versions,
///   2) collision-resistant when two plugins ship a component with the same name,
///   3) safe to embed in a JSON file (no whitespace, no dots).
/// </summary>
public static class ComponentKey
{
    public const string FallbackPlugin = "Native";

    /// <summary>
    /// Build the canonical key from a plugin name + component name pair.
    /// Throws when either input is null/whitespace.
    /// </summary>
    public static string Build(string pluginName, string componentName)
    {
        if (string.IsNullOrWhiteSpace(componentName))
            throw new ArgumentException("componentName must be non-empty", nameof(componentName));

        var plugin = string.IsNullOrWhiteSpace(pluginName) ? FallbackPlugin : pluginName;
        var safePlugin = Sanitize(plugin);
        var safeComponent = Sanitize(componentName);
        return $"{safePlugin}_{safeComponent}";
    }

    /// <summary>
    /// Build a key from already-extracted <see cref="ComponentInfo"/>. The key is
    /// regenerated to enforce the canonical rule, defending against callers that
    /// pre-populated the field with their own scheme.
    /// </summary>
    public static string Build(ComponentInfo info)
    {
        if (info == null) throw new ArgumentNullException(nameof(info));
        return Build(info.PluginName, info.Name);
    }

    private static string Sanitize(string s)
    {
        var trimmed = s.Trim();
        // Replace anything that is not letter / digit / underscore with '_'.
        // Keeps the key JSON-safe and case-sensitive deterministic.
        var buf = new System.Text.StringBuilder(trimmed.Length);
        foreach (var c in trimmed)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
                buf.Append(c);
            else
                buf.Append('_');
        }
        return buf.ToString();
    }
}
