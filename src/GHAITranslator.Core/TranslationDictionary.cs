using System;
using System.Collections.Generic;
using System.IO;
using GHAITranslator.Core.Models;

namespace GHAITranslator.Core;

/// <summary>
/// In-memory dictionary with BuiltinSeed as the immutable base and the
/// user's <c>dictionary.json</c> overlaid on top (user wins on same key).
/// Persisted via <see cref="DictionaryIo"/>.
/// </summary>
public sealed class TranslationDictionary
{
    private readonly Dictionary<string, TranslationEntry> _entries;

    public TranslationDictionary(IEnumerable<TranslationEntry> seed)
    {
        _entries = new Dictionary<string, TranslationEntry>(StringComparer.Ordinal);
        foreach (var e in seed)
        {
            if (e == null || string.IsNullOrEmpty(e.Key)) continue;
            _entries[e.Key] = e;
        }
    }

    /// <summary>Number of entries currently in the dictionary.</summary>
    public int Count => _entries.Count;

    /// <summary>True if <paramref name="key"/> exists.</summary>
    public bool Contains(string key) => !string.IsNullOrEmpty(key) && _entries.ContainsKey(key);

    /// <summary>Lookup by exact key; returns <c>null</c> if not present.</summary>
    public TranslationEntry? Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        return _entries.TryGetValue(key, out var e) ? e : null;
    }

    /// <summary>
    /// Add or replace. Silently ignores invalid entries (empty key, null).
    /// </summary>
    public void AddOrUpdate(TranslationEntry entry)
    {
        if (entry == null || string.IsNullOrEmpty(entry.Key)) return;
        _entries[entry.Key] = entry;
    }

    /// <summary>
    /// Merge a user overlay on top of the existing dictionary. Existing keys
    /// are replaced by the user values; new keys are added.
    /// </summary>
    public void MergeOverlay(IEnumerable<TranslationEntry> overlay)
    {
        if (overlay == null) return;
        foreach (var e in overlay)
        {
            AddOrUpdate(e);
        }
    }

    /// <summary>All entries (read-only copy).</summary>
    public IReadOnlyCollection<TranslationEntry> All() => _entries.Values;
}