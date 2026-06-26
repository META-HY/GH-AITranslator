// Plugin entry point. Multi-target: net48 (Rhino 7) / net7.0-windows (Rhino 8).
// Keeps the integration surface small: register events at Load, drop them at Shutdown.

using System;
using Grasshopper.Kernel;
using GHAITranslator.Core;
using GHAITranslator.Integration;

namespace GHAITranslator
{
    /// <summary>
    /// GH-AITranslator 插件唯一入口。Plugin GUID is fixed at design time
    /// (9decaf74-8009-461f-9ac5-8132b61bae21) — once shipped do NOT change it,
    /// or users will see "missing plugin" warnings.
    /// </summary>
    public class GHAITranslatorPlugin : GH_AssemblyInfo
    {
        public override Guid Id => new Guid("9decaf74-8009-461f-9ac5-8132b61bae21");
        public override string Name => "GH-AITranslator";
        public override string Version => "1.0.0";
        // R8 renamed Author → AuthorName; we still expose AuthorContact for users to file issues.
        public override string AuthorName => "GH-AITranslator Team";
        public override string AuthorContact => "https://github.com/gh-aitranslator";
        public override string Description => "AI-driven Chinese translation layer for Grasshopper components.";

        // Grasshopper invokes the empty constructor to discover metadata. Do
        // NOT do any heavy work in the constructor.
        public GHAITranslatorPlugin() : base() { }

        // R8 GH_AssemblyInfo removed LoadAtStartup(). We do the heavy lift in
        // the static initializer — Grasshopper instantiates this class once at
        // load time, before it touches anything else, and Bootstrapper is
        // idempotent so re-entrancy is safe.
        static GHAITranslatorPlugin()
        {
            // Visible diagnostic on first-load failure: GH swallows cctor
            // exceptions silently so a missing dependency or bad SDK call
            // would otherwise leave the user with "no menu, no clue". The
            // MessageBox is the cheapest way to surface the actual error
            // for support tickets.
            try
            {
                Bootstrapper.Initialize();
                Log.Info("GH-AITranslator loaded.");
            }
            catch (Exception ex)
            {
                Log.Error("Plugin static init failed", ex);
                try
                {
                    System.Windows.Forms.MessageBox.Show(
                        "GH-AITranslator 加载失败:\n" + ex.Message +
                        "\n\n查看 %APPDATA%\\McNeel\\Rhinoceros\\<ver>\\Plug-ins\\GH-AITranslator\\*.log 获取详细堆栈。",
                        "GH-AITranslator",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Error);
                }
                catch { /* MessageBox may fail in some hosts — keep quiet */ }
            }
        }

        // We don't override OnShutdown — GH_AssemblyInfo doesn't expose one
        // and Bootstrapper's menu/renderer hold their own event subscriptions
        // that get torn down when the host process exits.
    }
}
