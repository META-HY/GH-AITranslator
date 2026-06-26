// Dictionary import / export. Format is the same JSON the dictionary already
// uses (see TranslationDictionary.DictionaryFile), so an export is also a
// portable backup file the user can hand to a colleague.

using System;
using System.Collections.Generic;
using System.IO;
using GHAITranslator.Core.Models;
using Newtonsoft.Json;

namespace GHAITranslator.Core
{
    public static class DictionaryIo
    {
        public sealed class ExportResult
        {
            public int Total { get; set; }
            public int Builtin { get; set; }
            public int User { get; set; }
            public int Ai { get; set; }
            public string FilePath { get; set; } = string.Empty;
        }

        public sealed class ImportResult
        {
            public int Imported { get; set; }
            public int Skipped { get; set; }
            public int Overwritten { get; set; }
        }

        public static ExportResult Export(TranslationDictionary dict, string targetPath, bool pretty = true)
        {
            if (dict == null) throw new ArgumentNullException(nameof(dict));
            if (string.IsNullOrWhiteSpace(targetPath))
                throw new ArgumentException("targetPath required", nameof(targetPath));

            var snapshot = dict.Snapshot();
            // net48 doesn't have a Dictionary<TK,TV>(IReadOnlyDictionary<TK,TV>, IEqualityComparer<TK>)
            // constructor, so copy via foreach to keep both targets happy.
            var entries = new Dictionary<string, TranslationEntry>(snapshot.Count, StringComparer.Ordinal);
            foreach (var kv in snapshot) entries[kv.Key] = kv.Value;
            var payload = new DictionaryFile { Version = "1.0", Entries = entries };
            var json = JsonConvert.SerializeObject(payload, pretty ? Formatting.Indented : Formatting.None);
            PluginPaths.EnsureDir(Path.GetDirectoryName(targetPath) ?? string.Empty);
            File.WriteAllText(targetPath, json);

            var r = new ExportResult { Total = snapshot.Count, FilePath = targetPath };
            foreach (var e in snapshot.Values)
            {
                if (e.Source == TranslationSource.Builtin) r.Builtin++;
                else if (e.Source == TranslationSource.Ai) r.Ai++;
                else r.User++;
            }
            return r;
        }

        /// <summary>
        /// Import a JSON file into the dictionary. The merge strategy is
        /// controlled by <paramref name="mode"/>:
        ///   * <see cref="ImportMode.MergeKeep"/>       — skip keys that already exist
        ///   * <see cref="ImportMode.MergeOverwrite"/>  — overwrite existing keys
        ///   * <see cref="ImportMode.Replace"/>         — wipe the dict and load only the file
        /// </summary>
        public static ImportResult Import(TranslationDictionary dict, string sourcePath, ImportMode mode)
        {
            if (dict == null) throw new ArgumentNullException(nameof(dict));
            if (string.IsNullOrWhiteSpace(sourcePath))
                throw new ArgumentException("sourcePath required", nameof(sourcePath));
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("Dictionary import file missing.", sourcePath);

            var json = File.ReadAllText(sourcePath);
            var payload = JsonConvert.DeserializeObject<DictionaryFile>(json);
            var incoming = payload?.Entries ?? new Dictionary<string, TranslationEntry>();

            var r = new ImportResult();

            if (mode == ImportMode.Replace)
            {
                // External snapshot loaded into a fresh in-memory map, then we
                // call the dictionary's internal AddOrUpdate for each. The
                // dictionary retains its own user/builtin seed? No — user picked
                // Replace, so we drop everything (including builtins) and load
                // the file as the new source of truth.
                dict.ReplaceAll(incoming);
                r.Imported = incoming.Count;
                return r;
            }

            var existing = dict.Snapshot();
            foreach (var kv in incoming)
            {
                if (string.IsNullOrEmpty(kv.Key) || kv.Value == null) continue;
                if (existing.ContainsKey(kv.Key))
                {
                    if (mode == ImportMode.MergeOverwrite)
                    {
                        dict.AddOrUpdate(kv.Key, kv.Value);
                        r.Overwritten++;
                    }
                    else
                    {
                        r.Skipped++;
                    }
                }
                else
                {
                    dict.AddOrUpdate(kv.Key, kv.Value);
                    r.Imported++;
                }
            }
            return r;
        }

        public enum ImportMode
        {
            MergeKeep = 0,
            MergeOverwrite = 1,
            Replace = 2
        }

        // Mirror of TranslationDictionary.DictionaryFile, kept private here so
        // the on-disk schema is owned by the IO layer, not the dictionary.
        private sealed class DictionaryFile
        {
            public string Version { get; set; } = "1.0";
            public Dictionary<string, TranslationEntry> Entries { get; set; } = new();
        }
    }
}
