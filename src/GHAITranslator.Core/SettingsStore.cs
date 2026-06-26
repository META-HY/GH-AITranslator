using System;
using System.IO;
using Newtonsoft.Json;

namespace GHAITranslator.Core;

/// <summary>
/// Loads and persists <see cref="PluginSettings"/> as JSON. Mirrors the dictionary
/// contract: missing file → defaults, corrupt file → defaults, IO failure → silent.
/// </summary>
public static class SettingsStore
{
    public static PluginSettings Load(string path)
    {
        if (!File.Exists(path)) return new PluginSettings();
        try
        {
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<PluginSettings>(json) ?? new PluginSettings();
        }
        catch
        {
            return new PluginSettings();
        }
    }

    public static void Save(string path, PluginSettings settings)
    {
        if (settings == null) return;
        try
        {
            PluginPaths.EnsureDir(Path.GetDirectoryName(path) ?? string.Empty);
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(path, json);
        }
        catch
        {
            // Save failure must not crash the host.
        }
    }
}
