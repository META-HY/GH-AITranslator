// Composition root: wires together the Core services and the Rhino-side adapters.
// Lives separately from the plugin class so the same wiring can be exercised
// from a unit test by calling Bootstrapper.Initialize(testRoot: ...).

using System;
using System.IO;
using GHAITranslator.Core;
using GHAITranslator.Core.AI;
using GHAITranslator.Core.Packs;
using GHAITranslator.Integration;

namespace GHAITranslator
{
    internal static class Bootstrapper
    {
        private static TranslationDictionary _dict;
        private static PluginSettings _settings;
        private static TranslationPipeline _pipeline;
        private static CanvasLabelRenderer _renderer;
        private static HttpAiClient _aiClient;
        private static SettingsMenu _menu;

        public static TranslationPipeline Pipeline => _pipeline;
        public static TranslationDictionary Dictionary => _dict;
        public static PluginSettings Settings => _settings;

        public static void Initialize(string appDataOverride = null)
        {
            if (_dict != null) return; // idempotent

            var rhinoVersion = DetectRhinoVersion();
            var dictPath = PluginPaths.GetDictionaryPath(rhinoVersion, appDataOverride);
            var settingsPath = PluginPaths.GetSettingsPath(rhinoVersion, appDataOverride);
            var logPath = PluginPaths.GetLogPath(rhinoVersion, appDataOverride);

            Log.Bind(logPath);
            Log.Info($"Bootstrap starting. Rhino={rhinoVersion}, dict={dictPath}");

            _dict = new TranslationDictionary(dictPath);
            _dict.Load();

            _settings = SettingsStore.Load(settingsPath);

            _aiClient = new HttpAiClient(_settings);
            _pipeline = new TranslationPipeline(_dict, _aiClient, maxConcurrency: 3);

            _renderer = new CanvasLabelRenderer(_pipeline, _settings);
            _renderer.Attach();

            // third-party packs
            try
            {
                foreach (var p in ThirdPartyPacks.All) _dict.AddPack(p);
            }
            catch (Exception ex) { Log.Error("Pack registration failed", ex); }

            // menu — registered LATER, once Grasshopper's UI is fully up.
            // Calling SettingsMenu.Install() from the static cctor fails
            // silently because Grasshopper.Instances.DocumentEditor and
            // its MainMenuStrip are not yet initialised when the assembly
            // is first discovered. Schedule a one-shot install that fires
            // off the WinForms message loop (Application.Idle) instead.
            try
            {
                _menu = new SettingsMenu();
                ScheduleMenuInstall();
            }
            catch (Exception ex) { Log.Error("Menu scheduling failed", ex); }

            Log.Info($"Bootstrap done. Dictionary entries: {_dict.Count}");
        }

        private static System.Windows.Forms.Timer _menuInstallTimer;

        private static void ScheduleMenuInstall()
        {
            // Timer that polls for the GH MainMenuStrip every 250ms. As soon
            // as it appears, we install the menu and stop the timer. The
            // Application.Idle event isn't available in the static cctor
            // (the message loop isn't running yet) so a WinForms Timer is
            // the cleanest cross-version way to defer.
            _menuInstallTimer?.Dispose();
            _menuInstallTimer = new System.Windows.Forms.Timer { Interval = 250 };
            _menuInstallTimer.Tick += (_, _) =>
            {
                try
                {
                    var editor = Grasshopper.Instances.DocumentEditor;
                    if (editor == null) return;
                    var menu = editor.MainMenuStrip;
                    if (menu == null) return;

                    _menu.Install();
                    _menuInstallTimer.Stop();
                    _menuInstallTimer.Dispose();
                    _menuInstallTimer = null;
                    Log.Info("Menu installed (delayed).");
                }
                catch (Exception ex)
                {
                    Log.Error("Delayed menu install failed", ex);
                    _menuInstallTimer?.Stop();
                    _menuInstallTimer?.Dispose();
                    _menuInstallTimer = null;
                }
            };
            _menuInstallTimer.Start();
        }

        public static void Shutdown()
        {
            try { _menuInstallTimer?.Dispose(); _menuInstallTimer = null; } catch { /* best-effort */ }
            try { _menu?.Dispose(); } catch (Exception ex) { Log.Error("menu dispose", ex); }
            try { _renderer?.Detach(); } catch (Exception ex) { Log.Error("renderer detach", ex); }
            try { _dict?.Save(); } catch (Exception ex) { Log.Error("dict save", ex); }
            try
            {
                if (_settings != null)
                {
                    var rhinoVersion = DetectRhinoVersion();
                    SettingsStore.Save(PluginPaths.GetSettingsPath(rhinoVersion), _settings);
                }
            }
            catch (Exception ex) { Log.Error("settings save", ex); }

            _aiClient?.Dispose();
            _aiClient = null;
            _renderer = null;
            _pipeline = null;
            _dict = null;
            _settings = null;
        }

        public static void NotifySettingsChanged()
        {
            if (_settings == null || _renderer == null) return;
            _renderer.UpdateSettings(_settings);
        }

        private static string DetectRhinoVersion()
        {
            // Rhino 7 ships .NET 4.8; Rhino 8 ships .NET 7. We don't have a
            // clean compile-time switch for "which Rhino is hosting us" inside
            // a single binary, so we sniff the running Rhino major version.
            // Fallback to "7.0" — that data path already exists for both releases
            // in our testing environment and the user can re-migrate.
            try
            {
                var v = Rhino.RhinoApp.Version;
                if (v != null && v.Major >= 8) return "8.0";
            }
            catch
            {
                // Rhino not loaded (e.g. unit test) — keep default.
            }
            return "7.0";
        }
    }
}
