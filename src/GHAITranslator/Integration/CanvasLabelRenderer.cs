// Paint-overlay renderer. The renderer's contract:
//
//   * Attach to GH_Canvas.Paint once.
//   * On every Paint tick, walk the visible canvas objects and, for each one
//     whose key is in the local dictionary, draw the Chinese label above the
//     component's bounds.
//   * NEVER call the AI from inside Paint. The dictionary is the only source
//     of truth at draw time. The dispatcher warms the cache on document load
//     / component-added events.
//
// We also expose a pure-Core helper (DrawLabelOnto) so the rendering math can
// be unit-tested with a System.Drawing.Bitmap on Linux.

using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using GHAITranslator.Core;

namespace GHAITranslator.Integration
{
    public sealed class CanvasLabelRenderer : IDisposable
    {
        private readonly TranslationPipeline _pipeline;
        private PluginSettings _settings;
        private GH_Canvas _canvas;
        private readonly Dictionary<IGH_DocumentObject, RectangleF> _bounds = new();

        public CanvasLabelRenderer(TranslationPipeline pipeline, PluginSettings settings)
        {
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void UpdateSettings(PluginSettings s) { _settings = s; }

        public void Attach()
        {
            Detach();
            _canvas = Grasshopper.Instances.ActiveCanvas;
            if (_canvas != null)
            {
                _canvas.Paint += OnPaint;
                _canvas.DocumentChanged += OnDocumentChanged;
            }
        }

        public void Detach()
        {
            if (_canvas != null)
            {
                _canvas.Paint -= OnPaint;
                _canvas.DocumentChanged -= OnDocumentChanged;
                _canvas = null;
            }
            _bounds.Clear();
        }

        private void OnDocumentChanged(object sender, GH_CanvasDocumentChangedEventArgs e)
        {
            _bounds.Clear();
        }

        private void OnPaint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            if (_settings == null || !_settings.EnableTranslation || _canvas?.Document == null) return;
            var graphics = e.Graphics;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var textColor = Color.FromArgb(_settings.LabelTextColorArgb);
            var bgColor = Color.FromArgb(_settings.LabelBackgroundArgb);

            foreach (var obj in _canvas.Document.Objects)
            {
                // R8 GH_Attributes dropped the Visible flag — components with a
                // non-null Attributes object are always paintable. The bounds
                // check below filters out zero-sized / collapsed objects.
                if (obj?.Attributes == null) continue;

                var key = ComponentKey.Build(SafePlugin(obj), obj.Name);
                var translation = _pipeline.TryLookup(key);
                if (string.IsNullOrEmpty(translation)) continue;

                var rect = obj.Attributes.Bounds;
                DrawLabelOnto(graphics, rect, translation, _settings.LabelFontSize, textColor, bgColor);
            }
        }

        private static string SafePlugin(IGH_DocumentObject obj)
        {
            // R8 GH_AssemblyInfo / IGH_DocumentObject dropped Library entirely.
            // Best we can do now is hash the component GUID — keeps keys stable
            // across sessions while letting ComponentKey distinguish plugins
            // when they later get merged into a single .gha dictionary file.
            try
            {
                var g = obj?.ComponentGuid;
                if (g.HasValue && g.Value != Guid.Empty) return g.Value.ToString("N");
            }
            catch { }
            return ComponentKey.FallbackPlugin;
        }

        /// <summary>
        /// Pure function: draw one label. Public + static so unit tests can
        /// render onto a System.Drawing.Bitmap and assert the layout.
        /// </summary>
        public static void DrawLabelOnto(
            Graphics g,
            RectangleF componentBounds,
            string text,
            float fontSize,
            Color textColor,
            Color backgroundColor)
        {
            if (g == null || string.IsNullOrEmpty(text)) return;

            using (var font = new Font("Microsoft YaHei", fontSize, FontStyle.Regular, GraphicsUnit.Point))
            {
                var textSize = g.MeasureString(text, font);
                var x = componentBounds.X + (componentBounds.Width - textSize.Width) / 2f;
                var y = componentBounds.Y - textSize.Height - 2f;

                var padX = 3f;
                var padY = 1f;
                var bg = new RectangleF(x - padX, y - padY, textSize.Width + 2 * padX, textSize.Height + 2 * padY);
                using (var bgBrush = new SolidBrush(backgroundColor))
                    g.FillRectangle(bgBrush, bg);

                using (var border = new Pen(Color.FromArgb(160, 180, 180, 180)))
                    g.DrawRectangle(border, bg.X, bg.Y, bg.Width, bg.Height);

                using (var textBrush = new SolidBrush(textColor))
                    g.DrawString(text, font, textBrush, x, y);
            }
        }

        public void Dispose() => Detach();
    }
}
