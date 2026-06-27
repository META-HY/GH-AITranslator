using System;
using System.IO;
using Newtonsoft.Json;

namespace GHAITranslator.Core;

/// <summary>
/// Persists <see cref="PluginSettings"/> as JSON. Defaults are returned on
/// any error so a corrupt file never blocks the plugin.
/// </summary>
public static class SettingsStore
{
    public static PluginSettings Load()
    {
        try
        {
            var path = PluginPaths.SettingsFile;
            if (!File.Exists(path)) return new PluginSettings();
            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) return new PluginSettings();
            return JsonConvert.DeserializeObject<PluginSettings>(json) ?? new PluginSettings();
        }
        catch (Exception ex)
        {
            Log.Warn($"SettingsStore.Load failed: {ex.Message}");
            return new PluginSettings();
        }
    }

    public static bool Save(PluginSettings settings)
    {
        try
        {
            var path = PluginPaths.SettingsFile;
            Directory.CreateDirectory(PluginPaths.UserDataDir);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonConvert.SerializeObject(settings, Formatting.Indented));
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warn($"SettingsStore.Save failed: {ex.Message}");
            return false;
        }
    }
}