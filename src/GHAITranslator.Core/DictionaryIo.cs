using System;
using System.Collections.Generic;
using System.IO;
using GHAITranslator.Core.Models;
using Newtonsoft.Json;

namespace GHAITranslator.Core;

/// <summary>
/// Read/write <see cref="TranslationEntry"/> overlays to
/// <c>dictionary.json</c> in the user-data directory. The overlay file only
/// contains user customizations on top of <see cref="BuiltinSeed"/>.
/// </summary>
public static class DictionaryIo
{
    public sealed class OverlayFile
    {
        [JsonProperty("version")]
        public int Version { get; set; } = 2;

        [JsonProperty("entries")]
        public List<TranslationEntry> Entries { get; set; } = new();
    }

    /// <summary>
    /// Load overlay from <paramref name="path"/>. Returns an empty overlay if
    /// the file does not exist or is unreadable. Never throws.
    /// </summary>
    public static OverlayFile Load(string path)
    {
        try
        {
            if (!File.Exists(path)) return new OverlayFile();
            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) return new OverlayFile();
            var parsed = JsonConvert.DeserializeObject<OverlayFile>(json);
            return parsed ?? new OverlayFile();
        }
        catch (Exception ex)
        {
            Log.Warn($"DictionaryIo.Load failed for {path}: {ex.Message}");
            return new OverlayFile();
        }
    }

    /// <summary>
    /// Save <paramref name="overlay"/> to <paramref name="path"/>. Atomic
    /// write via temp + rename. Returns <c>false</c> on IO failure (logged).
    /// </summary>
    public static bool Save(string path, OverlayFile overlay)
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonConvert.SerializeObject(overlay, Formatting.Indented));
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warn($"DictionaryIo.Save failed for {path}: {ex.Message}");
            return false;
        }
    }
}