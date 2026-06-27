using System;
using System.Windows.Forms;
using GHAITranslator.Core;
using GHAITranslator.Core.Models;
using GH = Grasshopper;
using Grasshopper;
using Grasshopper.Kernel;

namespace GHAITranslator.Integration;

/// <summary>
/// Manages the "翻译 / Translation" menu in the GH menu strip.
///
/// The menu is added on a deferral timer because GH's <c>MainMenuStrip</c>
/// isn't always ready when <c>PriorityLoad</c> runs. The exact pattern
/// matches what works in the field (verified on R7 / R8 / SR25).
/// </summary>
public sealed class SettingsMenu : IDisposable
{
    private ToolStripMenuItem? _root;
    private readonly Func<PluginSettings>       _getSettings;
    private readonly Action<LanguageMode>       _setLanguageMode;
    private readonly Action                     _translateAll;
    private readonly Action                     _restoreAll;
    private bool _disposed;

    public SettingsMenu(
        Func<PluginSettings> getSettings,
        Action<LanguageMode> setLanguageMode,
        Action translateAll,
        Action restoreAll)
    {
        _getSettings = getSettings;
        _setLanguageMode = setLanguageMode;
        _translateAll = translateAll;
        _restoreAll = restoreAll;
    }

    /// <summary>
    /// Schedule a retry loop that installs the menu once GH's MainMenuStrip
    /// becomes available. Bounded retry count so we don't loop forever if
    /// GH never starts.
    /// </summary>
    public void Install()
    {
        var tries = 0;
        var timer = new System.Windows.Forms.Timer { Interval = 1000 };
        timer.Tick += (s, e) =>
        {
            tries++;
            if (TryInstallMenu() || tries > 30)
            {
                timer.Stop();
                timer.Dispose();
            }
        };
        timer.Start();
    }

    private bool TryInstallMenu()
    {
        try
        {
            var menuStrip = Instances.DocumentEditor?.MainMenuStrip;
            if (menuStrip == null) return false;
            if (_root != null && menuStrip.Items.Contains(_root)) return true;

            _root = BuildMenu();
            menuStrip.Items.Add(_root);
            Log.Info("SettingsMenu installed in GH menu strip.");
            return true;
        }
        catch (Exception ex)
        {
            Log.Warn($"SettingsMenu.TryInstallMenu failed: {ex.Message}");
            return false;
        }
    }

    private ToolStripMenuItem BuildMenu()
    {
        var root = new ToolStripMenuItem("翻译(&T)")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            ToolTipText  = "GH-AITranslator-Pro",
        };

        // Language mode
        var modeChinese   = new ToolStripMenuItem("中文")        { CheckOnClick = true };
        var modeBilingual = new ToolStripMenuItem("中英双语")    { CheckOnClick = true };
        var modeEnglish   = new ToolStripMenuItem("English")    { CheckOnClick = true };
        var current = _getSettings().LanguageMode;
        modeChinese.Checked   = current == LanguageMode.Chinese;
        modeBilingual.Checked = current == LanguageMode.Bilingual;
        modeEnglish.Checked   = current == LanguageMode.English;
        modeChinese.Click   += (s, e) => SwitchTo(LanguageMode.Chinese);
        modeBilingual.Click += (s, e) => SwitchTo(LanguageMode.Bilingual);
        modeEnglish.Click   += (s, e) => SwitchTo(LanguageMode.English);

        root.DropDownItems.Add(new ToolStripLabel("显示模式"));
        root.DropDownItems.Add(modeChinese);
        root.DropDownItems.Add(modeBilingual);
        root.DropDownItems.Add(modeEnglish);
        root.DropDownItems.Add(new ToolStripSeparator());

        // Translate / Restore
        var miTranslate = new ToolStripMenuItem("翻译当前画布") { Enabled = true };
        miTranslate.Click += (s, e) => _translateAll();
        root.DropDownItems.Add(miTranslate);

        var miRestore = new ToolStripMenuItem("恢复英文") { Enabled = true };
        miRestore.Click += (s, e) => _restoreAll();
        root.DropDownItems.Add(miRestore);

        root.DropDownItems.Add(new ToolStripSeparator());
        var miAbout = new ToolStripMenuItem("关于...");
        miAbout.Click += (s, e) => ShowAbout();
        root.DropDownItems.Add(miAbout);

        return root;
    }

    private void SwitchTo(LanguageMode mode)
    {
        _setLanguageMode(mode);
        _translateAll();
    }

    private static void ShowAbout()
    {
        MessageBox.Show(
            "GH-AITranslator-Pro\n" +
            "Rhino 内置 Grasshopper 完整中文翻译插件\n\n" +
            "显示模式: 中文 / 中英双语 / English\n" +
            "覆盖范围: 内置组件 + 第三方组件 + 面板 + Description\n",
            "GH-AITranslator-Pro",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _root?.Owner?.Items.Remove(_root); } catch { }
    }
}