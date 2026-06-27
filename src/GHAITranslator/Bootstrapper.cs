using System;
using System.Collections.Generic;
using GHAITranslator.Core;
using GHAITranslator.Core.Models;
using GHAITranslator.Integration;
using GH = Grasshopper;
using Grasshopper;
using Grasshopper.Kernel;

namespace GHAITranslator;

/// <summary>
/// Plugin startup and shutdown. Held statically so the GC doesn't collect
/// event subscriptions that hook into GH lifetime.
/// </summary>
public static class Bootstrapper
{
    private static TranslationDictionary? _dict;
    private static PluginSettings?        _settings;
    private static TranslationPipeline?   _pipeline;
    private static DocumentHook?          _docHook;
    private static SettingsMenu?          _menu;

    public static void Initialize()
    {
        try
        {
            // 1. Load settings (defaults if file missing/corrupt)
            _settings = SettingsStore.Load();

            // 2. Build dictionary: BuiltinSeed always merged + user overlay
            var dict = new TranslationDictionary(BuiltinSeed.All);
            var overlay = DictionaryIo.Load(PluginPaths.DictionaryFile);
            dict.MergeOverlay(overlay.Entries);
            _dict = dict;

            // 3. Build AI pipeline (HttpAiClient + TranslationPipeline)
            var http = new HttpAiClient(_settings.Ai);
            _pipeline = new TranslationPipeline(_dict, http, OnPipelinePersist);

            // 4. Hook document lifecycle
            _docHook = new DocumentHook(
                () => _dict!,
                () => _settings!,
                OnPipelinePersist);
            _docHook.Attach();

            // 5. Install menu
            _menu = new SettingsMenu(
                () => _settings!,
                SetLanguageMode,
                TranslateAll,
                RestoreAll);
            _menu.Install();

            Log.Info($"GH-AITranslator-Pro loaded. dict={_dict.Count} entries, mode={_settings.LanguageMode}");
        }
        catch (Exception ex)
        {
            Log.Error("Bootstrapper.Initialize failed", ex);
            ShowFatal(ex);
        }
    }

    private static void SetLanguageMode(LanguageMode mode)
    {
        if (_settings == null) return;
        _settings.LanguageMode = mode;
        SettingsStore.Save(_settings);
    }

    /// <summary>
    /// Re-translate every object on every open canvas. Triggered by the
    /// "翻译当前画布" menu item.
    /// </summary>
    public static void TranslateAll()
    {
        if (_dict == null || _settings == null) return;
        var mode = _settings.LanguageMode;
        var translated = 0;
        // GH_DocumentServer exposes `DocumentCount` (int) and a numeric indexer
        // in BOTH GH 7 and GH 8. The string-name indexer only exists in GH 8.
        // To stay cross-version compatible, iterate by index.
        var server = Instances.DocumentServer;
        var count = server.DocumentCount;
        for (var i = 0; i < count; i++)
        {
            var d = server[i];
            if (d == null) continue;
            foreach (var obj in d.Objects)
            {
                var key = GhAdapter.KeyFor(obj);
                var entry = _dict.Get(key);
                if (entry == null) continue;
                if (ComponentTranslator.ApplyToObject(obj, entry, mode))
                    translated++;
            }
        }
        try { Instances.InvalidateCanvas(); Instances.RedrawCanvas(); } catch { }
        Log.Info($"TranslateAll: {translated} object(s) updated to mode={mode}.");
    }

    /// <summary>
    /// Restore every object's English Name / Description / Category from
    /// <see cref="TranslationEntry.NameEn"/> etc.
    /// </summary>
    public static void RestoreAll()
    {
        if (_dict == null) return;
        var restored = 0;
        var server = Instances.DocumentServer;
        var count = server.DocumentCount;
        for (var i = 0; i < count; i++)
        {
            var d = server[i];
            if (d == null) continue;
            foreach (var obj in d.Objects)
            {
                var key = GhAdapter.KeyFor(obj);
                var entry = _dict.Get(key);
                if (entry == null) continue;
                if (ComponentTranslator.RestoreEnglish(obj, entry))
                    restored++;
            }
        }
        try { Instances.InvalidateCanvas(); Instances.RedrawCanvas(); } catch { }
        Log.Info($"RestoreAll: {restored} object(s) restored to English.");
    }

    // Signature is `Func<string, bool>` to match both TranslationPipeline
    // and DocumentHook. The `immediate` flag from older callers is
    // intentionally dropped — persistence is always "immediate enough"
    // (we write the whole overlay file, which is cheap).
    private static bool OnPipelinePersist(string key)
    {
        if (_dict == null) return false;
        try
        {
            var overlay = new DictionaryIo.OverlayFile { Entries = new List<TranslationEntry>() };
            foreach (var e in _dict.All())
            {
                // Only persist user-added entries (not BuiltinSeed).
                if (!e.Key.StartsWith(ComponentKey.NativePrefix, StringComparison.Ordinal)) continue;
                if (e.NameEn == null) continue;          // BuiltinSeed entries carry NameEn
                overlay.Entries.Add(e);
            }
            DictionaryIo.Save(PluginPaths.DictionaryFile, overlay);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warn($"OnPipelinePersist failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Show a fatal-error dialog so the user knows what happened instead of
    /// silently failing. Visible because the plugin won't load otherwise.
    /// </summary>
    public static void ShowFatal(Exception ex)
    {
        try
        {
            System.Windows.Forms.MessageBox.Show(
                "GH-AITranslator-Pro 加载失败:\n\n" + ex.Message +
                "\n\n详见 " + PluginPaths.LogFile,
                "GH-AITranslator-Pro",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Error);
        }
        catch { /* give up */ }
    }
}