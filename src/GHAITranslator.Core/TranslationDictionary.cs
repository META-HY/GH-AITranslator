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

    /// <summary>
    /// Load dictionary from disk. Built-in seed entries are ALWAYS merged in
    /// as the floor — user files (and AI translations) add on top. This means
    /// a v1 dictionary file that used ComponentGuid-shaped keys won't shadow
    /// the canonical "Native_*" keys we ship, and switching from v1 to v2 is
    /// seamless: the user's AI entries survive, the seed is restored.
    /// </summary>
    public void Load()
    {
        lock (_lock)
        {
            var seed = BuiltinSeed.Build();
            if (!File.Exists(_filePath))
            {
                _entries = seed;
                TrySaveUnsafe();
                return;
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                var data = JsonConvert.DeserializeObject<DictionaryFile>(json);
                var userEntries = data?.Entries != null
                    ? new Dictionary<string, TranslationEntry>(data.Entries, StringComparer.Ordinal)
                    : new Dictionary<string, TranslationEntry>(StringComparer.Ordinal);

                // Start from the seed (canonical "Native_*" + third-party
                // pack keys) and let user entries override it on a per-key
                // basis. This way a v1 dictionary file using GUID-shaped
                // keys never erases the canonical Native_ keys, and a v2
                // user file that DOES share a key with the seed (e.g. the
                // user customised Native_Curve's Chinese name) wins.
                foreach (var kv in userEntries) seed[kv.Key] = kv.Value;
                _entries = seed;
            }
            catch (Exception)
            {
                // Corrupt file: fall back to built-in seed so the plugin still works.
                _entries = seed;
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

    /// <summary>
    /// Compute the string the component's <c>Name</c>/<c>NickName</c>
    /// properties should be rewritten to under the given display mode.
    /// Returns null when there is no usable translation for the key —
    /// callers must then fall back to keeping the component's original
    /// English text rather than blanking it out.
    ///
    /// Behaviour by mode:
    ///   * Chinese   → entry.NickName (or entry.Name when NickName empty)
    ///   * Bilingual → "NickNameEn / NickName" (or "NameEn / Name" when
    ///                 the English nickname is missing). Falls back to
    ///                 single-language when no English half is known.
    ///   * English   → null (the caller leaves the original text alone)
    ///
    /// This is a pure function over the in-memory dictionary — no AI, no IO.
    /// </summary>
    public string? GetDisplayText(string key, LanguageMode mode)
    {
        var entry = GetEntry(key);
        if (entry == null || !entry.HasChinese) return null;

        switch (mode)
        {
            case LanguageMode.Chinese:
                return string.IsNullOrEmpty(entry.NickName) ? entry.Name : entry.NickName;

            case LanguageMode.Bilingual:
            {
                var zh = !string.IsNullOrEmpty(entry.NickName) ? entry.NickName : entry.Name;
                var en = !string.IsNullOrEmpty(entry.NickNameEn) ? entry.NickNameEn
                       : !string.IsNullOrEmpty(entry.NameEn)    ? entry.NameEn
                       : null;
                return en == null ? zh : $"{en} / {zh}";
            }

            case LanguageMode.English:
            default:
                return null;
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
