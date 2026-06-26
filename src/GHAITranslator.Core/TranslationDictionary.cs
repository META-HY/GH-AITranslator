using System;
using System.Collections.Generic;
using System.IO;
using GHAITranslator.Core.Models;
using Newtonsoft.Json;

namespace GHAITranslator.Core;

/// <summary>
/// In-memory translation dictionary backed by a single JSON file.
/// Concurrency model: a single read-write lock guards all mutations and the file
/// itself. Reads are not locked because we always swap a single reference atomically.
/// </summary>
public sealed class TranslationDictionary
{
    private readonly object _lock = new();
    private readonly string _filePath;
    private Dictionary<string, TranslationEntry> _entries = new(StringComparer.Ordinal);

    public TranslationDictionary(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("filePath required", nameof(filePath));
        _filePath = filePath;
    }

    /// <summary>How many entries are currently in memory.</summary>
    public int Count
    {
        get { lock (_lock) { return _entries.Count; } }
    }

    /// <summary>Read-only view (snapshot) of all entries. Safe to enumerate.</summary>
    public IReadOnlyDictionary<string, TranslationEntry> Snapshot()
    {
        lock (_lock) { return new Dictionary<string, TranslationEntry>(_entries); }
    }

    /// <summary>Load dictionary from disk. If the file is missing, the built-in seed is installed.</summary>
    public void Load()
    {
        lock (_lock)
        {
            if (!File.Exists(_filePath))
            {
                _entries = BuiltinSeed.Build();
                TrySaveUnsafe();
                return;
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                var data = JsonConvert.DeserializeObject<DictionaryFile>(json);
                _entries = data?.Entries != null
                    ? new Dictionary<string, TranslationEntry>(data.Entries, StringComparer.Ordinal)
                    : BuiltinSeed.Build();
            }
            catch (Exception)
            {
                // Corrupt file: fall back to built-in seed so the plugin still works.
                _entries = BuiltinSeed.Build();
            }
        }
    }

    /// <summary>Persist the current dictionary to disk. Silently ignores IO errors.</summary>
    public void Save()
    {
        lock (_lock) { TrySaveUnsafe(); }
    }

    private void TrySaveUnsafe()
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var payload = new DictionaryFile { Version = "1.0", Entries = _entries };
            var json = JsonConvert.SerializeObject(payload, Formatting.Indented);
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Intentionally swallow: save failure must not crash the host.
        }
    }

    /// <summary>Returns the Chinese display name for a component key, or null when missing.</summary>
    public string? GetTranslation(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        lock (_lock)
        {
            return _entries.TryGetValue(key, out var entry) ? entry.Name : null;
        }
    }

    public TranslationEntry? GetEntry(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        lock (_lock)
        {
            return _entries.TryGetValue(key, out var entry) ? entry : null;
        }
    }

    public bool ContainsKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        lock (_lock) { return _entries.ContainsKey(key); }
    }

    public void AddOrUpdate(string key, TranslationEntry entry)
    {
        if (string.IsNullOrEmpty(key) || entry == null) return;
        lock (_lock)
        {
            entry.UpdatedAt = DateTime.UtcNow.ToString("o");
            _entries[key] = entry;
        }
    }

    /// <summary>
    /// Drop everything and load the given entries. Used by "Replace" import.
    /// Caller is responsible for persisting afterwards.
    /// </summary>
    public void ReplaceAll(IEnumerable<KeyValuePair<string, TranslationEntry>> items)
    {
        if (items == null) return;
        var now = DateTime.UtcNow.ToString("o");
        var fresh = new Dictionary<string, TranslationEntry>(StringComparer.Ordinal);
        foreach (var kv in items)
        {
            if (string.IsNullOrEmpty(kv.Key) || kv.Value == null) continue;
            kv.Value.UpdatedAt = now;
            fresh[kv.Key] = kv.Value;
        }
        lock (_lock) { _entries = fresh; }
    }

    /// <summary>Replace a batch of entries at once (used when bulk-importing a user dictionary).</summary>
    public void Merge(IEnumerable<KeyValuePair<string, TranslationEntry>> items, bool overwrite)
    {
        if (items == null) return;
        lock (_lock)
        {
            foreach (var kv in items)
            {
                if (string.IsNullOrEmpty(kv.Key) || kv.Value == null) continue;
                if (!overwrite && _entries.ContainsKey(kv.Key)) continue;
                kv.Value.UpdatedAt = DateTime.UtcNow.ToString("o");
                _entries[kv.Key] = kv.Value;
            }
        }
    }

    /// <summary>
    /// Merge a third-party translation pack into the dictionary. Existing
    /// user-edited entries (Source == "user") are NEVER overwritten by a
    /// pack — user's edits always win.
    /// </summary>
    public int AddPack(Packs.ITranslationPack pack)
    {
        if (pack == null) return 0;
        var added = 0;
        lock (_lock)
        {
            foreach (var kv in pack.Entries)
            {
                if (!_entries.TryGetValue(kv.Key, out var existing) || existing.Source != TranslationSource.User)
                {
                    kv.Value.UpdatedAt = DateTime.UtcNow.ToString("o");
                    _entries[kv.Key] = kv.Value;
                    added++;
                }
            }
        }
        return added;
    }

    private sealed class DictionaryFile
    {
        public string Version { get; set; } = "1.0";
        public Dictionary<string, TranslationEntry> Entries { get; set; } = new();
    }
}
