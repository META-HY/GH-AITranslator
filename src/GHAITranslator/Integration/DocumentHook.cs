using System;
using System.Collections.Generic;
using GHAITranslator.Core;
using GHAITranslator.Core.Models;
using GH = Grasshopper;
using Grasshopper;
using Grasshopper.Kernel;

namespace GHAITranslator.Integration;

/// <summary>
/// Hooks into Grasshopper's document lifecycle:
///   - on document open, translate every object on the canvas
///   - on object added, translate the new object
///
/// Attached in <see cref="Bootstrapper.Initialize"/>.
/// Detached in <see cref="Bootstrapper.Shutdown"/>.
/// </summary>
public sealed class DocumentHook : IDisposable
{
    private readonly Func<TranslationDictionary> _dictAccessor;
    private readonly Func<PluginSettings>       _settingsAccessor;
    private readonly Action<string, bool>       _persistCallback;
    private bool _disposed;

    public DocumentHook(
        Func<TranslationDictionary> dictAccessor,
        Func<PluginSettings> settingsAccessor,
        Action<string, bool> persistCallback)
    {
        _dictAccessor     = dictAccessor;
        _settingsAccessor = settingsAccessor;
        _persistCallback  = persistCallback;
    }

    public void Attach()
    {
        GH_DocumentServer.DocumentAdded += OnDocumentAdded;
    }

    public void Detach()
    {
        GH_DocumentServer.DocumentAdded -= OnDocumentAdded;
    }

    private void OnDocumentAdded(GH_DocumentServer sender, GH_Document doc)
    {
        if (doc == null) return;
        try
        {
            TranslateAllObjects(doc);
            doc.ObjectsAdded += OnObjectsAdded;
            doc.SolutionEnd += (s, e) => RefreshCanvas();
        }
        catch (Exception ex)
        {
            Log.Warn($"DocumentHook.OnDocumentAdded failed: {ex.Message}");
        }
    }

    private void OnObjectsAdded(object sender, GH_DocObjectEventArgs e)
    {
        try
        {
            if (e?.Objects == null) return;
            var settings = _settingsAccessor();
            if (!settings.AutoTranslateNew) return;
            var dict = _dictAccessor();
            var mode = settings.LanguageMode;

            foreach (var obj in e.Objects)
            {
                if (obj == null) continue;
                var key = GhAdapter.KeyFor(obj);
                var entry = dict.Get(key);
                if (entry == null) continue;
                if (ComponentTranslator.ApplyToObject(obj, entry, mode))
                {
                    _persistCallback(key, false);
                }
            }
            RefreshCanvas();
        }
        catch (Exception ex)
        {
            Log.Warn($"DocumentHook.OnObjectsAdded failed: {ex.Message}");
        }
    }

    private void TranslateAllObjects(GH_Document doc)
    {
        var settings = _settingsAccessor();
        if (!settings.TranslateOnCanvasOpen) return;

        var dict = _dictAccessor();
        var mode = settings.LanguageMode;
        var translated = 0;

        foreach (var obj in EnumerateAll(doc))
        {
            var key = GhAdapter.KeyFor(obj);
            var entry = dict.Get(key);
            if (entry == null) continue;
            if (ComponentTranslator.ApplyToObject(obj, entry, mode))
            {
                translated++;
            }
        }
        if (translated > 0) _persistCallback("__canvas_open__", true);
        RefreshCanvas();
        Log.Info($"DocumentHook translated {translated} object(s) on canvas open.");
    }

    /// <summary>
    /// Walk the full document graph: every object on the canvas, plus every
    /// parameter inside every group, plus every Cluster's child document.
    /// </summary>
    private static IEnumerable<IGH_DocumentObject> EnumerateAll(GH_Document doc)
    {
        foreach (var obj in doc.Objects)
        {
            yield return obj;
            if (obj is IGH_Param param && param.VolatileDataCount > 0)
            {
                // parameters don't have child objects, but their source wires do
            }
        }
        foreach (var obj in doc.Objects) // second pass for clusters
        {
            if (obj is GH_Cluster cluster && cluster.Document != null)
            {
                foreach (var child in EnumerateAll(cluster.Document))
                    yield return child;
            }
        }
    }

    private static void RefreshCanvas()
    {
        try
        {
            Instances.InvalidateCanvas();
            Instances.RedrawCanvas();
        }
        catch { /* GH not ready yet, ignore */ }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Detach();
    }
}