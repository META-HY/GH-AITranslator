using System;
using System.Windows.Forms;
using GHAITranslator.Core;
using GHAITranslator.Core.Models;

namespace GHAITranslator.UI;

/// <summary>
/// Floating settings panel for the AI provider and language mode. Opened
/// from the GH menu. Uses native WinForms (no Eto dependency to keep
/// net48 happy).
/// </summary>
public sealed class SettingsPanel : IDisposable
{
    private Form? _form;

    public void Show(PluginSettings settings, Action<PluginSettings> onSave)
    {
        if (_form != null && !_form.IsDisposed)
        {
            _form.BringToFront();
            return;
        }

        _form = new Form
        {
            Text = "GH-AITranslator-Pro 设置",
            Width = 520,
            Height = 380,
            StartPosition = FormStartPosition.CenterScreen,
            FormBorderStyle = FormBorderStyle.Sizable,
            MinimizeBox = true,
            MaximizeBox = true,
        };

        var lblMode = new System.Windows.Forms.Label { Text = "显示模式", Left = 16, Top = 20, Width = 100 };
        var cmbMode = new ComboBox { Left = 130, Top = 16, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbMode.Items.AddRange(new object[] { "中文", "中英双语", "English" });
        cmbMode.SelectedItem = settings.LanguageMode.ToDisplayName();

        var lblProvider = new System.Windows.Forms.Label { Text = "AI 提供商", Left = 16, Top = 60, Width = 100 };
        var cmbProvider = new ComboBox { Left = 130, Top = 56, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbProvider.Items.AddRange(new object[] { "OpenAI", "DeepSeek", "Qwen", "Custom" });
        cmbProvider.SelectedItem = string.IsNullOrEmpty(settings.Ai.Provider) ? "OpenAI" : settings.Ai.Provider;

        var lblKey = new System.Windows.Forms.Label { Text = "API Key", Left = 16, Top = 100, Width = 100 };
        var txtKey = new TextBox { Left = 130, Top = 96, Width = 340, UseSystemPasswordChar = true, Text = settings.Ai.ApiKey };

        var lblEndpoint = new System.Windows.Forms.Label { Text = "Endpoint", Left = 16, Top = 140, Width = 100 };
        var txtEndpoint = new TextBox { Left = 130, Top = 136, Width = 340, Text = settings.Ai.Endpoint };

        var lblModel = new System.Windows.Forms.Label { Text = "Model", Left = 16, Top = 180, Width = 100 };
        var txtModel = new TextBox { Left = 130, Top = 176, Width = 200, Text = settings.Ai.Model };

        var chkOnOpen = new CheckBox { Left = 16, Top = 220, Width = 250, Text = "打开画布时自动翻译", Checked = settings.TranslateOnCanvasOpen };
        var chkOnNew  = new CheckBox { Left = 16, Top = 248, Width = 250, Text = "新增组件时自动翻译", Checked = settings.AutoTranslateNew };

        var btnSave = new Button { Text = "保存", Left = 380, Top = 290, Width = 90, DialogResult = DialogResult.OK };
        var btnCancel = new Button { Text = "取消", Left = 290, Top = 290, Width = 80, DialogResult = DialogResult.Cancel };

        _form.AcceptButton = btnSave;
        _form.CancelButton = btnCancel;

        _form.Controls.AddRange(new Control[]
        {
            lblMode, cmbMode,
            lblProvider, cmbProvider,
            lblKey, txtKey,
            lblEndpoint, txtEndpoint,
            lblModel, txtModel,
            chkOnOpen, chkOnNew,
            btnSave, btnCancel,
        });

        _form.FormClosed += (s, e) => { _form?.Dispose(); _form = null; };

        cmbProvider.SelectedIndexChanged += (s, e) =>
        {
            var preset = ProviderRegistry.Find(cmbProvider.SelectedItem?.ToString() ?? "");
            if (preset == null) return;
            if (string.IsNullOrEmpty(txtEndpoint.Text) ||
                MessageBox.Show("替换为预设的 Endpoint / Model?", "GH-AITranslator",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                txtEndpoint.Text = preset.Endpoint;
                txtModel.Text = preset.Model;
            }
        };

        if (_form.ShowDialog() == DialogResult.OK)
        {
            settings.LanguageMode = LanguageModeExtensions.FromDisplayName(cmbMode.SelectedItem?.ToString() ?? "中文");
            settings.Ai.Provider = cmbProvider.SelectedItem?.ToString() ?? "OpenAI";
            settings.Ai.ApiKey   = txtKey.Text;
            settings.Ai.Endpoint = txtEndpoint.Text;
            settings.Ai.Model    = txtModel.Text;
            settings.TranslateOnCanvasOpen = chkOnOpen.Checked;
            settings.AutoTranslateNew = chkOnNew.Checked;
            onSave(settings);
        }
    }

    public void Dispose()
    {
        try { _form?.Dispose(); } catch { }
        _form = null;
    }
}