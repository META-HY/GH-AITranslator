using System;
using System.IO;
using System.Reflection;

namespace GHAITranslator.Core;

/// <summary>
/// File-system locations used by the plugin. All paths are derived from
/// <c>%APPDATA%\GH-AITranslator\</c> on Windows so the plugin survives
/// Rhino/GH reinstalls and never writes inside Program Files.
/// </summary>
public static class PluginPaths
{
    public const string AppDirName = "GH-AITranslator";

    /// <summary>User data directory (settings, dictionary, log).</summary>
    public static string UserDataDir
    {
        get
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, AppDirName);
        }
    }

    public static string SettingsFile => Path.Combine(UserDataDir, "settings.json");
    public static string DictionaryFile => Path.Combine(UserDataDir, "dictionary.json");
    public static string LogFile => Path.Combine(UserDataDir, "plugin.log");
}