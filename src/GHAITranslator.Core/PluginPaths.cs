using System;
using System.IO;

namespace GHAITranslator.Core;

/// <summary>
/// Single source of truth for on-disk locations. Both the Core and Plugin layers
/// depend on this so the dictionary, settings, and log files never end up
/// in inconsistent places.
///
/// We follow the design doc's location:
///   %AppData%\McNeel\Rhinoceros\&lt;version&gt;\Plug-ins\GH-AITranslator\
/// </summary>
public static class PluginPaths
{
    public const string PluginFolderName = "GH-AITranslator";

    /// <summary>Resolve a per-version plugin data directory.</summary>
    /// <param name="rhinoVersion">"7.0" or "8.0"; used to namespace the data folder.</param>
    /// <param name="appData">Override the %AppData% root (used by tests).</param>
    public static string GetPluginDir(string rhinoVersion, string? appData = null)
    {
        if (string.IsNullOrWhiteSpace(rhinoVersion))
            throw new ArgumentException("rhinoVersion required", nameof(rhinoVersion));

        var root = appData ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(root, "McNeel", "Rhinoceros", rhinoVersion, "Plug-ins", PluginFolderName);
    }

    public static string GetDictionaryPath(string rhinoVersion, string? appData = null)
        => Path.Combine(GetPluginDir(rhinoVersion, appData), "dictionary.json");

    public static string GetSettingsPath(string rhinoVersion, string? appData = null)
        => Path.Combine(GetPluginDir(rhinoVersion, appData), "settings.json");

    public static string GetLogPath(string rhinoVersion, string? appData = null)
        => Path.Combine(GetPluginDir(rhinoVersion, appData), "plugin.log");

    /// <summary>Create the directory tree if missing. Idempotent.</summary>
    public static void EnsureDir(string dir)
    {
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    }
}
