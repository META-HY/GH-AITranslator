// DocumentHook — bridges Grasshopper document events into the
// ComponentTranslator.
//
// Grasshopper 8.25 SDK (verified via metadata inspection of the
// 8.0.23304.9001 NuGet, same major+minor shape as 8.25.x):
//
//   Grasshopper.Kernel.GH_DocumentServer      — has events:
//     • DocumentAdded(GH_DocumentServer, GH_Document)
//     • DocumentRemoved(GH_DocumentServer, GH_Document)
//
//   Grasshopper.Kernel.GH_Document            — has events:
//     • ObjectsAdded(GH_Document, GH_DocObjectEventArgs)
//     • ObjectsDeleted(GH_Document, GH_DocObjectEventArgs)
//
// GH_DocObjectEventArgs exposes:
//     • Document     : GH_Document
//     • Object       : IGH_DocumentObject (single, indexed by [int])
//     • Objects      : IReadOnlyList<IGH_DocumentObject>
//     • ObjectCount  : int
//     • Attributes   : IReadOnlyList<IGH_Attributes>
//
// Delegate signatures (confirmed by Mono.Cecil reflection against the
// real 8.25.25314.11001 NuGet):
//   DocumentAddedEventHandler   : void (GH_DocumentServer, GH_Document)
//   DocumentRemovedEventHandler : void (GH_DocumentServer, GH_Document)
//   ObjectsAddedEventHandler    : void (object, GH_DocObjectEventArgs)
//   ObjectsDeletedEventHandler  : void (object, GH_DocObjectEventArgs)
//
// We subscribe to both layers: DocumentServer events fire when the user
// opens or closes a .gh file, and the per-document events fire when a
// new component is dragged onto an already-open canvas. Either way, we
// re-run ComponentTranslator.ApplyToDocument on the affected document
// so the labels stay in sync with the active LanguageMode.
//
// Threading: GH fires these events on the UI thread; we never block.
// All event handlers are synchronous and cheap — translator does one
// pass over visible objects.

using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using GHAITranslator.Core;

namespace GHAITranslator.Integration
{
    public sealed class DocumentHook : IDisposable
    {
        private readonly ComponentTranslator _translator;

        /// <summary>The mode the hook currently has the canvas rendered in.</summary>
        public LanguageMode CurrentMode { get; private set; } = LanguageMode.Chinese;

        private bool _disposed;
        private readonly HashSet<GH_Document> _subscribed = new();

        public DocumentHook(ComponentTranslator translator)
        {
            _translator = translator ?? throw new ArgumentNullException(nameof(translator));
        }

        public void Install()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DocumentHook));

            Instances.DocumentServer.DocumentAdded += OnDocumentAdded;
            Instances.DocumentServer.DocumentRemoved += OnDocumentRemoved;

            // Subscribe to documents that are already open when the GHA loads
            // (the user might have a .gh file open before our plugin loads).
            // GH_DocumentServer exposes a default indexer `this[int]` that
            // returns the GH_Document at that index (the property is
            // decorated with [DefaultMemberAttribute("Document")] in the
            // Grasshopper assembly, so C# resolves `ds[i]` to it).
            var ds = Instances.DocumentServer;
            for (var i = 0; i < ds.DocumentCount; i++)
            {
                var doc = ds[i];
                if (doc == null) continue;
                SubscribeToDocument(doc);
                try { _translator.ApplyToDocument(doc, CurrentMode); }
                catch (Exception ex) { Log.Error("Translate on install failed", ex); }
            }
        }

        public void Uninstall()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                Instances.DocumentServer.DocumentAdded -= OnDocumentAdded;
                Instances.DocumentServer.DocumentRemoved -= OnDocumentRemoved;
            }
            catch (Exception ex) { Log.Error("Unsubscribe DocumentServer events", ex); }

            foreach (var doc in _subscribed)
            {
                try { UnsubscribeFromDocument(doc); } catch { }
            }
            _subscribed.Clear();
        }

        /// <summary>
        /// Switch language mode. Re-translates every open document and forces
        /// a canvas redraw so the new labels show immediately.
        /// </summary>
        public void SetMode(LanguageMode mode)
        {
            if (_disposed) return;
            CurrentMode = mode;
            var ds = Instances.DocumentServer;
            for (var i = 0; i < ds.DocumentCount; i++)
            {
                var doc = ds[i];
                if (doc == null) continue;
                try { _translator.ApplyToDocument(doc, CurrentMode); }
                catch (Exception ex) { Log.Error($"Re-translate {doc.DocumentID} after mode change", ex); }
            }
            try { Instances.InvalidateCanvas(); }
            catch (Exception ex) { Log.Error("InvalidateCanvas after SetMode failed", ex); }
        }

        public void Dispose() => Uninstall();

        // ─── event handlers ─────────────────────────────────────────────────

        private void OnDocumentAdded(GH_DocumentServer sender, GH_Document doc)
        {
            if (doc == null) return;
            SubscribeToDocument(doc);
            try { _translator.ApplyToDocument(doc, CurrentMode); }
            catch (Exception ex) { Log.Error("Translate on doc added failed", ex); }
        }

        private void OnDocumentRemoved(GH_DocumentServer sender, GH_Document doc)
        {
            if (doc == null) return;
            try { UnsubscribeFromDocument(doc); }
            catch (Exception ex) { Log.Error("Unsubscribe on doc removed", ex); }
            try { _translator.ForgetDocument(doc.DocumentID); }
            catch (Exception ex) { Log.Error("ForgetDocument on close failed", ex); }
        }

        // Both ObjectsAdded/ObjectsDeleted declare their delegate as
        // (object sender, GH_DocObjectEventArgs e) — the sender is the
        // GH_Document itself but typed as object in the .NET event signature.
        // We pull the doc out of the event args instead.
        private void OnObjectsAdded(object sender, GH_DocObjectEventArgs e)
        {
            // GH_DocObjectEventArgs exposes .Objects (IList<IGH_DocumentObject>)
            // — translate each one in place. We pass doc.DocumentID explicitly
            // because the component itself may not yet be wired into GH's
            // object graph at the moment this event fires.
            if (e == null) return;
            var doc = e.Document;
            if (doc == null) return;
            var docSerial = doc.DocumentID;
            var items = e.Objects;
            if (items == null) return;
            foreach (var obj in items)
            {
                if (obj == null) continue;
                try { _translator.ApplyToObject(obj, docSerial, CurrentMode); }
                catch (Exception ex) { Log.Error($"Translate on object added failed for {obj.Name}", ex); }
            }
            try { Instances.InvalidateCanvas(); } catch { }
        }

        private void OnObjectsDeleted(object sender, GH_DocObjectEventArgs e)
        {
            // The components are gone — just refresh so any leftover overlay
            // or stale layout gets cleared.
            try { Instances.InvalidateCanvas(); } catch { }
        }

        // ─── helpers ────────────────────────────────────────────────────────

        private void SubscribeToDocument(GH_Document doc)
        {
            if (!_subscribed.Add(doc)) return;
            try
            {
                doc.ObjectsAdded += OnObjectsAdded;
                doc.ObjectsDeleted += OnObjectsDeleted;
            }
            catch (Exception ex)
            {
                Log.Warn($"Subscribe to {doc.DocumentID} failed: {ex.Message}");
                _subscribed.Remove(doc);
            }
        }

        private void UnsubscribeFromDocument(GH_Document doc)
        {
            if (!_subscribed.Remove(doc)) return;
            try { doc.ObjectsAdded   -= OnObjectsAdded;   } catch { }
            try { doc.ObjectsDeleted -= OnObjectsDeleted; } catch { }
        }
    }
}
