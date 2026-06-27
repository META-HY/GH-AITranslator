// Grasshopper menu hook. Registers a "GH-AITranslator" item in
// the GH menu bar with:
//   * Display mode radio items (Chinese / Bilingual / English) — flip the
//     active LanguageMode and re-translate the canvas immediately.
//   * Translate Canvas Now — force-translate every visible component, hitting
//     the AI only when the local dictionary is cold (kept from v1).
//   * Reload Dictionary    — re-apply all third-party packs on top of the
//     current user dictionary.
//   * Settings…            — open the modeless settings panel.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using GHAITranslator.Core;
using GHAITranslator.Core.Packs;
using GHAITranslator.Integration;
using GHAITranslator.UI;

namespace GHAITranslator.Integration
{
    internal sealed class SettingsMenu : IDisposable
    {
        private ToolStripMenuItem? _rootItem;
        private ToolStripMenuItem? _chineseItem;
        private ToolStripMenuItem? _bilingualItem;
        private ToolStripMenuItem? _englishItem;

        public void Install()
        {
            if (_rootItem != null) return;
            try
            {
                var menu = Grasshopper.Instances.DocumentEditor?.MainMenuStrip;
                if (menu == null) return;

                _rootItem = new ToolStripMenuItem("GH-AITranslator");

                // Display-mode radio group. We build the items first, then
                // wire click handlers and refresh the checkmarks against
                // the current PluginSettings so the menu reflects whatever
                // the user picked in the settings panel.
                _chineseItem = new ToolStripMenuItem("中文(&C)")
                {
                    CheckOnClick = true,
                    ToolTipText = "Curve → 曲线"
                };
                _chineseItem.Click += (_, _) => ApplyMode(LanguageMode.Chinese);
                _rootItem.DropDownItems.Add(_chineseItem);

                _bilingualItem = new ToolStripMenuItem("中英对照(&B)")
                {
                    CheckOnClick = true,
                    ToolTipText = "Curve / 曲线"
                };
                _bilingualItem.Click += (_, _) => ApplyMode(LanguageMode.Bilingual);
                _rootItem.DropDownItems.Add(_bilingualItem);

                _englishItem = new ToolStripMenuItem("英文原文(&E)")
                {
                    CheckOnClick = true,
                    ToolTipText = "Keep the original English label, no Chinese."
                };
                _englishItem.Click += (_, _) => ApplyMode(LanguageMode.English);
                _rootItem.DropDownItems.Add(_englishItem);

                _rootItem.DropDownItems.Add(new ToolStripSeparator());

                var translateItem = new ToolStripMenuItem("翻译当前画布(&T)...");
                translateItem.Click += async (_, _) => await TranslateCanvasAsync().ConfigureAwait(false);
                _rootItem.DropDownItems.Add(translateItem);

                var reloadItem = new ToolStripMenuItem("重载第三方词库(&R)...");
                reloadItem.Click += (_, _) => ReloadThirdPartyPacks();
                _rootItem.DropDownItems.Add(reloadItem);

                _rootItem.DropDownItems.Add(new ToolStripSeparator());

                var settingsItem = new ToolStripMenuItem("设置(&S)...");
                settingsItem.Click += (_, _) => OpenSettings();
                _rootItem.DropDownItems.Add(settingsItem);

                RefreshCheckmarks();

                // DropDownOpening fires every time the user opens the menu —
                // use it to re-sync checkmarks in case the settings panel
                // was used to change the mode while the menu was hidden.
                _rootItem.DropDownOpening += (_, _) => RefreshCheckmarks();

                menu.Items.Add(_rootItem);
            }
            catch (Exception ex)
            {
                Log.Error("Failed to install settings menu", ex);
            }
        }

        private void RefreshCheckmarks()
        {
            var mode = Bootstrapper.Settings?.Mode ?? LanguageMode.Chinese;
            if (_chineseItem   != null) _chineseItem.Checked   = mode == LanguageMode.Chinese;
            if (_bilingualItem != null) _bilingualItem.Checked = mode == LanguageMode.Bilingual;
            if (_englishItem   != null) _englishItem.Checked   = mode == LanguageMode.English;
        }

        private static void ApplyMode(LanguageMode mode)
        {
            try
            {
                var settings = Bootstrapper.Settings;
                if (settings == null) return;
                settings.Mode = mode;
                // Push the new mode into the live translator. Re-applies
                // to every open document so the change is visible without
                // needing to close/reopen the file.
                Bootstrapper.NotifySettingsChanged();
                // Persist so the choice survives a Rhino restart.
                try
                {
                    var rhinoVersion = "7.0";
                    try { var v = Rhino.RhinoApp.Version; if (v != null && v.Major >= 8) rhinoVersion = "8.0"; } catch { }
                    SettingsStore.Save(PluginPaths.GetSettingsPath(rhinoVersion), settings);
                }
                catch (Exception ex) { Log.Error("Save settings after mode change failed", ex); }
            }
            catch (Exception ex)
            {
                Log.Error("ApplyMode failed", ex);
            }
        }

        public void Uninstall()
        {
            try
            {
                var menu = Grasshopper.Instances.DocumentEditor?.MainMenuStrip;
                if (menu != null && _rootItem != null) menu.Items.Remove(_rootItem);
            }
            catch { /* best-effort */ }
            _rootItem = null;
        }

        private static async Task TranslateCanvasAsync()
        {
            var pipeline = Bootstrapper.Pipeline;
            var dict = Bootstrapper.Dictionary;
            if (pipeline == null || dict == null)
            {
                Log.Warn("TranslateCanvas clicked before Bootstrapper finished initialising.");
                return;
            }

            var doc = Grasshopper.Instances.ActiveCanvas?.Document;
            if (doc == null)
            {
                MessageBox.Show("当前画布为空,无需翻译。", "GH-AITranslator",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var items = doc.Objects
                .Where(o => o?.Attributes != null)
                .Select(o => ComponentInfoBuilder.Build(new GhAdapter(o)))
                .Where(info => info != null && !string.IsNullOrEmpty(info.Key))
                .ToList();

            var total = items.Count;
            if (total == 0)
            {
                MessageBox.Show("当前画布上没有可翻译的组件。", "GH-AITranslator",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int hits = 0, misses = 0, failures = 0;
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            var tasks = items.Select(async info =>
            {
                try
                {
                    var before = pipeline.TryLookup(info!.Key);
                    var entry = await pipeline.EnsureTranslatedAsync(info!, cts.Token).ConfigureAwait(false);
                    if (entry == null || string.IsNullOrEmpty(entry.Name)) { failures++; return; }
                    if (before == entry.Name) hits++; else misses++;
                }
                catch (OperationCanceledException) { /* swallow — counted at end */ }
                catch (Exception ex) { Log.Error($"Translate failed for {info!.Key}", ex); failures++; }
            });
            await Task.WhenAll(tasks).ConfigureAwait(false);

            try { dict.Save(); } catch (Exception ex) { Log.Error("dict save after translate", ex); }

            // Force a repaint so newly cached labels show up immediately.
            Grasshopper.Instances.ActiveCanvas?.Refresh();

            MessageBox.Show(
                $"翻译完成: 共 {total} 个组件\n" +
                $"本地命中: {hits}\n" +
                $"AI 翻译: {misses}\n" +
                $"失败: {failures}",
                "GH-AITranslator", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void ReloadThirdPartyPacks()
        {
            var dict = Bootstrapper.Dictionary;
            if (dict == null) return;
            try
            {
                var added = 0;
                foreach (var p in ThirdPartyPacks.All) added += dict.AddPack(p);
                dict.Save();
                MessageBox.Show($"第三方词库已重载,新增/更新 {added} 条。", "GH-AITranslator",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log.Error("Reload packs failed", ex);
                MessageBox.Show("重载失败: " + ex.Message, "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Thin adapter for ComponentInfoBuilder — wraps an IGH_DocumentObject
        // in the IGhDocumentObject interface without going through the full
        // CanvasComponentSource enumeration (we already have the doc).
        private sealed class GhAdapter : IGhDocumentObject
        {
            private readonly IGH_DocumentObject _o;
            public GhAdapter(IGH_DocumentObject o) { _o = o; }
            public string Name => _o.Name ?? string.Empty;
            public string NickName => _o.NickName ?? string.Empty;
            public string Description => _o.Description ?? string.Empty;
            public string PluginName
            {
                get
                {
                    // Mirror ComponentTranslator.SafePlugin: route native
                    // GH components to the "Native" canonical key so AI
                    // translations land in the same namespace as BuiltinSeed;
                    // third-party plugins get a stable assembly-derived key.
                    try
                    {
                        var t = _o?.GetType();
                        var asmName = t?.Assembly?.GetName().Name ?? string.Empty;
                        if (!string.IsNullOrEmpty(asmName))
                        {
                            if (asmName.Equals("Grasshopper", System.StringComparison.OrdinalIgnoreCase)
                                || asmName.StartsWith("Grasshopper.", System.StringComparison.OrdinalIgnoreCase)
                                || asmName.Equals("RhinoCommon", System.StringComparison.OrdinalIgnoreCase))
                                return ComponentKey.FallbackPlugin;
                            var buf = new System.Text.StringBuilder(asmName.Length);
                            foreach (var c in asmName)
                                buf.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
                            return buf.ToString();
                        }
                    }
                    catch { }
                    var g = _o.ComponentGuid;
                    return g == Guid.Empty ? ComponentKey.FallbackPlugin : g.ToString("N");
                }
            }
            public bool Visible => _o.Attributes != null;
            public System.Collections.Generic.IEnumerable<IGhParam> InputParams
            {
                get
                {
                    if (_o is IGH_Component c)
                        return c.Params.Input.Select(p => (IGhParam)new ParamAdapter(p));
                    return System.Linq.Enumerable.Empty<IGhParam>();
                }
            }
            public System.Collections.Generic.IEnumerable<IGhParam> OutputParams
            {
                get
                {
                    if (_o is IGH_Component c)
                        return c.Params.Output.Select(p => (IGhParam)new ParamAdapter(p));
                    return System.Linq.Enumerable.Empty<IGhParam>();
                }
            }
        }

        private sealed class ParamAdapter : IGhParam
        {
            private readonly IGH_Param _p;
            public ParamAdapter(IGH_Param p) { _p = p; }
            public string Name => _p.Name ?? string.Empty;
            public string NickName => _p.NickName ?? string.Empty;
            public string Description => _p.Description ?? string.Empty;
        }

        private static void OpenSettings()
        {
            try
            {
                var settings = Bootstrapper.Settings;
                var dict = Bootstrapper.Dictionary;
                if (settings == null || dict == null) return;

                var rhinoVersion = "7.0";
                try { var v = Rhino.RhinoApp.Version; if (v != null && v.Major >= 8) rhinoVersion = "8.0"; } catch { }
                var dictPath = PluginPaths.GetDictionaryPath(rhinoVersion);

                var panel = new SettingsPanel(settings, dict, dictPath);
                panel.SettingsChanged += (_, _) => Bootstrapper.NotifySettingsChanged();

                var form = new Form
                {
                    Text = "GH-AITranslator 设置",
                    Width = 520,
                    Height = 600,
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.Sizable,
                    MinimizeBox = false,
                    MaximizeBox = true
                };
                form.Controls.Add(panel);
                panel.Dock = DockStyle.Fill;
                // R8 removed GH_DocumentEditor.ShowModelessPanel. Fall back to
                // standard WinForms Show(owner) — keeps the panel non-modal,
                // parented to the GH window so it doesn't get lost behind
                // other apps, and survives in R7 too because owner assignment
                // is optional.
                form.Show(Grasshopper.Instances.DocumentEditor);
                form.Owner = Grasshopper.Instances.DocumentEditor as Form;
            }
            catch (Exception ex)
            {
                Log.Error("OpenSettings failed", ex);
                MessageBox.Show("打开设置失败: " + ex.Message);
            }
        }

        public void Dispose() => Uninstall();
    }
}
