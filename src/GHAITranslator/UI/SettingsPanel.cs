// WinForms settings panel. Embedded as a UserControl so the host (a floating
// Grasshopper window) can dock it. All UI logic is local to this file; the
// control talks to Bootstrapper through the public Settings/Dictionary accessors
// and raises a single SettingsChanged event the bootstrapper subscribes to.
//
// Why a UserControl and not a Form: a Form would force a separate top-level
// window; a UserControl can be hosted in any container, which gives the
// Grasshopper side menu maximum flexibility.

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GHAITranslator.Core;
using GHAITranslator.Core.Packs;

namespace GHAITranslator.UI
{
    public sealed class SettingsPanel : UserControl
    {
        private readonly PluginSettings _settings;
        private readonly TranslationDictionary _dict;
        private readonly string _dictPath;

        private ComboBox _providerCombo;
        private TextBox _endpointBox;
        private TextBox _modelBox;
        private TextBox _apiKeyBox;
        private CheckBox _enableCheck;
        private NumericUpDown _fontSizeSpinner;
        private Button _fontColorBtn;
        private Button _bgColorBtn;
        private RadioButton _chineseRadio;
        private RadioButton _bilingualRadio;
        private RadioButton _englishRadio;
        private CheckBox _hoverCheck;
        private Label _dictCountLabel;
        private Button _exportBtn;
        private Button _importBtn;
        private Button _resetPacksBtn;
        private Button _saveBtn;
        private Button _closeBtn;

        private Color _fontColor;
        private Color _bgColor;

        public event EventHandler? SettingsChanged;

        public SettingsPanel(PluginSettings settings, TranslationDictionary dict, string dictPath)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _dict = dict ?? throw new ArgumentNullException(nameof(dict));
            _dictPath = dictPath ?? string.Empty;
            _fontColor = Color.FromArgb(settings.LabelTextColorArgb);
            _bgColor = Color.FromArgb(settings.LabelBackgroundArgb);
            InitializeComponent();
            BindToSettings();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            Dock = DockStyle.Fill;
            Padding = new Padding(12);
            BackColor = SystemColors.Control;

            var layout = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 14,
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // ── Provider ──
            AddLabel(layout, "服务商:", 0);
            _providerCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            foreach (var p in ProviderRegistry.All)
                _providerCombo.Items.Add(new ComboItem((int)p.Id, p.DisplayName));
            _providerCombo.SelectedIndexChanged += (_, _) =>
            {
                if (_providerCombo.SelectedItem is ComboItem ci)
                {
                    var desc = ProviderRegistry.Resolve((AiProvider)ci.Value);
                    _endpointBox.Text = desc.DefaultEndpoint;
                    _modelBox.Text = desc.DefaultModel;
                    SetPlaceholder(_apiKeyBox, desc.KeyPlaceholder);
                }
            };
            layout.Controls.Add(_providerCombo, 1, 0);

            // ── Endpoint ──
            AddLabel(layout, "API 地址:", 1);
            _endpointBox = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(_endpointBox, 1, 1);

            // ── Model ──
            AddLabel(layout, "模型名称:", 2);
            _modelBox = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(_modelBox, 1, 2);

            // ── API key ──
            AddLabel(layout, "API 密钥:", 3);
            _apiKeyBox = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
            layout.Controls.Add(_apiKeyBox, 1, 3);

            // ── Enable ──
            AddLabel(layout, "启用翻译:", 4);
            _enableCheck = new CheckBox { Text = "在画布上叠加中文标签", Dock = DockStyle.Fill };
            layout.Controls.Add(_enableCheck, 1, 4);

            // ── Language mode (radio buttons, mutually exclusive) ──
            AddLabel(layout, "显示语言(&L):", 5);
            var langPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.TopDown };
            _chineseRadio = new RadioButton { Text = "中文 (Curve → 曲线)", AutoSize = true };
            _bilingualRadio = new RadioButton { Text = "中英对照 (Curve / 曲线)", AutoSize = true };
            _englishRadio = new RadioButton { Text = "英文原文 (不翻译)", AutoSize = true };
            langPanel.Controls.Add(_chineseRadio);
            langPanel.Controls.Add(_bilingualRadio);
            langPanel.Controls.Add(_englishRadio);
            layout.Controls.Add(langPanel, 1, 5);

            // ── Font size ──
            AddLabel(layout, "标签字号:", 6);
            _fontSizeSpinner = new NumericUpDown
            {
                Minimum = 6,
                Maximum = 24,
                DecimalPlaces = 0,
                Dock = DockStyle.Fill
            };
            layout.Controls.Add(_fontSizeSpinner, 1, 6);

            // ── Font color ──
            AddLabel(layout, "文字颜色:", 7);
            _fontColorBtn = new Button { Dock = DockStyle.Fill, Text = "选择..." };
            _fontColorBtn.Click += (_, _) =>
            {
                using var dlg = new ColorDialog { Color = _fontColor };
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                {
                    _fontColor = dlg.Color;
                    _fontColorBtn.BackColor = _fontColor;
                }
            };
            layout.Controls.Add(_fontColorBtn, 1, 7);

            // ── BG color ──
            AddLabel(layout, "背景颜色:", 8);
            _bgColorBtn = new Button { Dock = DockStyle.Fill, Text = "选择..." };
            _bgColorBtn.Click += (_, _) =>
            {
                using var dlg = new ColorDialog { Color = _bgColor };
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                {
                    _bgColor = dlg.Color;
                    _bgColorBtn.BackColor = _bgColor;
                }
            };
            layout.Controls.Add(_bgColorBtn, 1, 8);

            // ── Hover ──
            AddLabel(layout, "悬停提示:", 9);
            _hoverCheck = new CheckBox { Text = "悬停时显示中文描述", Dock = DockStyle.Fill };
            layout.Controls.Add(_hoverCheck, 1, 9);

            // ── Dictionary IO ──
            AddLabel(layout, "本地词库:", 10);
            var dictPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            _exportBtn = new Button { Text = "导出...", AutoSize = true };
            _exportBtn.Click += OnExport;
            _importBtn = new Button { Text = "导入...", AutoSize = true };
            _importBtn.Click += OnImport;
            _resetPacksBtn = new Button { Text = "重载第三方词库", AutoSize = true };
            _resetPacksBtn.Click += OnReloadPacks;
            dictPanel.Controls.AddRange(new Control[] { _exportBtn, _importBtn, _resetPacksBtn });
            layout.Controls.Add(dictPanel, 1, 10);

            // ── Dict count label ──
            AddLabel(layout, "词库规模:", 11);
            _dictCountLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            layout.Controls.Add(_dictCountLabel, 1, 11);

            // ── Action row ──
            AddLabel(layout, "", 12);
            var actionPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            _saveBtn = new Button { Text = "保存", AutoSize = true };
            _saveBtn.Click += OnSave;
            _closeBtn = new Button { Text = "关闭", AutoSize = true };
            _closeBtn.Click += (_, _) => FindForm()?.Close();
            actionPanel.Controls.AddRange(new Control[] { _saveBtn, _closeBtn });
            layout.Controls.Add(actionPanel, 1, 12);

            // ── Help row ──
            AddLabel(layout, "", 13);
            var helpLbl = new Label
            {
                Text = "提示:首次加载内置原生组件翻译零延迟;第三方组件首次出现时由 AI 翻译,后续本地命中。",
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray
            };
            layout.Controls.Add(helpLbl, 1, 13);

            Controls.Add(layout);
            ResumeLayout(false);
        }

        private static void AddLabel(TableLayoutPanel layout, string text, int row)
        {
            var lbl = new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            };
            layout.Controls.Add(lbl, 0, row);
        }

        private void BindToSettings()
        {
            // provider
            for (var i = 0; i < _providerCombo.Items.Count; i++)
            {
                if (((ComboItem)_providerCombo.Items[i]).Value == (int)_settings.Provider)
                {
                    _providerCombo.SelectedIndex = i;
                    break;
                }
            }
            _endpointBox.Text = _settings.ApiEndpoint;
            _modelBox.Text = _settings.ModelName;
            _apiKeyBox.Text = _settings.ApiKey;
            _enableCheck.Checked = _settings.EnableTranslation;
            _fontSizeSpinner.Value = (decimal)Math.Max(6, Math.Min(24, _settings.LabelFontSize));
            _fontColorBtn.BackColor = _fontColor;
            _bgColorBtn.BackColor = _bgColor;
            _hoverCheck.Checked = _settings.ShowDescriptionOnHover;
            // Language mode → radio button (mutually exclusive).
            switch (_settings.Mode)
            {
                case LanguageMode.Bilingual: _bilingualRadio.Checked = true; break;
                case LanguageMode.English:   _englishRadio.Checked   = true; break;
                default:                      _chineseRadio.Checked   = true; break;
            }
            _dictCountLabel.Text = $"共 {_dict.Count} 条 (路径: {_dictPath})";
        }

        private void OnSave(object? sender, EventArgs e)
        {
            try
            {
                if (_providerCombo.SelectedItem is ComboItem ci)
                    _settings.Provider = (AiProvider)ci.Value;
                _settings.ApiEndpoint = _endpointBox.Text.Trim();
                _settings.ModelName = _modelBox.Text.Trim();
                if (!string.IsNullOrEmpty(_apiKeyBox.Text) && _apiKeyBox.Text != _settings.ApiKey)
                {
                    // Only overwrite the key when the user typed a new value.
                    // The password-mask makes it impossible to "see" the old value
                    // back, so we use a sentinel: if the box equals the masked
                    // ApiKey (or is empty), leave the original untouched.
                }
                // The masked TextBox returns empty when UseSystemPasswordChar is
                // true AND the user didn't edit it — so we treat empty as
                // "keep existing key". This matches 1Password-style UX.
                if (!string.IsNullOrEmpty(_apiKeyBox.Text))
                    _settings.ApiKey = _apiKeyBox.Text.Trim();

                _settings.EnableTranslation = _enableCheck.Checked;
                _settings.LabelFontSize = (float)_fontSizeSpinner.Value;
                _settings.LabelTextColorArgb = unchecked((int)_fontColor.ToArgb());
                _settings.LabelBackgroundArgb = unchecked((int)_bgColor.ToArgb());
                _settings.ShowDescriptionOnHover = _hoverCheck.Checked;
                // Radio buttons: pick the checked one and write back to
                // LanguageMode. RadioButtons auto-exclude siblings, so exactly
                // one of these three is true after any user click.
                if (_chineseRadio.Checked)        _settings.Mode = LanguageMode.Chinese;
                else if (_bilingualRadio.Checked) _settings.Mode = LanguageMode.Bilingual;
                else if (_englishRadio.Checked)   _settings.Mode = LanguageMode.English;

                SettingsStore.Save(GetSettingsPath(), _settings);
                SettingsChanged?.Invoke(this, EventArgs.Empty);
                MessageBox.Show(FindForm(), "设置已保存", "GH-AITranslator",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), "保存失败: " + ex.Message, "GH-AITranslator",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnExport(object? sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog
            {
                Title = "导出词库",
                Filter = "JSON 文件 (*.json)|*.json",
                FileName = $"ghaip-dictionary-{DateTime.Now:yyyyMMdd-HHmm}.json"
            };
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
            try
            {
                var r = DictionaryIo.Export(_dict, dlg.FileName);
                MessageBox.Show(FindForm(),
                    $"已导出 {r.Total} 条\n(内置 {r.Builtin}, 用户 {r.User}, AI {r.Ai})",
                    "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), "导出失败: " + ex.Message, "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnImport(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "导入词库",
                Filter = "JSON 文件 (*.json)|*.json"
            };
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

            var mode = PromptImportMode();
            if (mode == null) return;
            try
            {
                var r = DictionaryIo.Import(_dict, dlg.FileName, mode.Value);
                _dict.Save();
                _dictCountLabel.Text = $"共 {_dict.Count} 条 (路径: {_dictPath})";
                SettingsChanged?.Invoke(this, EventArgs.Empty);
                MessageBox.Show(FindForm(),
                    $"导入完成\n新增: {r.Imported}\n覆盖: {r.Overwritten}\n跳过: {r.Skipped}",
                    "导入成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), "导入失败: " + ex.Message, "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static DictionaryIo.ImportMode? PromptImportMode()
        {
            var r = MessageBox.Show(
                "选择导入模式:\n\n是 = 合并(覆盖已存在)\n否 = 合并(保留已有)\n取消 = 替换(清空后导入)",
                "导入模式", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            return r switch
            {
                DialogResult.Yes => DictionaryIo.ImportMode.MergeOverwrite,
                DialogResult.No => DictionaryIo.ImportMode.MergeKeep,
                DialogResult.Cancel => DictionaryIo.ImportMode.Replace,
                _ => null
            };
        }

        private void OnReloadPacks(object? sender, EventArgs e)
        {
            try
            {
                var added = 0;
                foreach (var p in ThirdPartyPacks.All) added += _dict.AddPack(p);
                _dict.Save();
                _dictCountLabel.Text = $"共 {_dict.Count} 条 (路径: {_dictPath})";
                SettingsChanged?.Invoke(this, EventArgs.Empty);
                MessageBox.Show(FindForm(), $"第三方词库已重载,新增/更新 {added} 条",
                    "重载成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), "重载失败: " + ex.Message, "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetSettingsPath() => Path.Combine(
            Path.GetDirectoryName(_dictPath) ?? string.Empty, "settings.json");

        private sealed class ComboItem
        {
            public int Value { get; }
            public string Text { get; }
            public ComboItem(int v, string t) { Value = v; Text = t; }
            public override string ToString() => Text;
        }

        // TextBox.PlaceholderText is .NET Core 2.1+ only. On net48 we have
        // to P/Invoke EM_SETCUEBANNER. Both branches converge into one
        // single call site so the rest of the file stays clean.
        private static void SetPlaceholder(TextBox box, string text)
        {
            if (box == null) return;
#if NET48
            NativeMethods.SendMessage(box.Handle, NativeMethods.EM_SETCUEBANNER, IntPtr.Zero, text ?? string.Empty);
#else
            box.PlaceholderText = text ?? string.Empty;
#endif
        }

#if NET48
        private static class NativeMethods
        {
            public const int EM_SETCUEBANNER = 0x1501;

            [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
            public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);
        }
#endif
    }
}
