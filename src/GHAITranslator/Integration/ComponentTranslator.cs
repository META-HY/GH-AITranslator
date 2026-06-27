// ComponentTranslator — the heart of the GHChinese-style translation pipeline.
//
// Where CanvasLabelRenderer just PAINTED a translated label on top of the
// component (overlay mode, leaving the real Name/NickName untouched), this
// class actually MUTATES the IGH_DocumentObject's Name/NickName properties
// so the translated text becomes the component's native text. This is what
// GHChinese does — when you flip translation on, the canvas looks exactly
// like a Chinese-localised Rhino build.
//
// State model
// ───────────
// * On the FIRST translation of a given component, we snapshot its
//   original Name + NickName into _originals. From then on, switching
//   between modes uses that snapshot — never reads the (possibly
//   already-rewritten) current values.
// * When a document closes, or when we see a component whose Guid we
//   have snapshotted but no longer appears in the active document, we
//   drop its snapshot via ForgetDocument() so the map can't grow forever.
// * If a user manually edits a translated component's Name (say, to
//   "My Curve" instead of "曲线"), we respect that edit on the next
//   switch — we never overwrite an existing NickName/Name whose value
//   is NOT the same as the one we last wrote. This protects user edits.
//
// Threading
// ─────────
// Must be called on the GH UI thread (the only thread that touches
// IGH_DocumentObject anyway). All public methods are synchronous.

using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using GHAITranslator.Core;
using GHAITranslator.Core.Models;

namespace GHAITranslator.Integration
{
    /// <summary>
    /// Rewrites the <c>Name</c>/<c>NickName</c> of every visible component in a
    /// Grasshopper document so the canvas displays the translated text as the
    /// component's native label. Holds a per-document snapshot of original
    /// English labels so flipping between Chinese / Bilingual / English is
    /// exact and reversible.
    /// </summary>
    public sealed class ComponentTranslator
    {
        private readonly TranslationDictionary _dict;

        /// <summary>
        /// Keyed by (Document.DocumentID, Component.ComponentGuid). The string
        /// payload is the original English label captured BEFORE the first
        /// rewrite — we use it to restore the component when the user picks
        /// English mode or disables translation.
        /// </summary>
        private sealed class OriginalLabels
        {
            // Plain `set` rather than `init` because net48 doesn't ship
            // System.Runtime.CompilerServices.IsExternalInit; switching
            // to init would require adding a PolySharp dependency just
            // for one keyword.
            public string Name { get; set; } = string.Empty;
            public string NickName { get; set; } = string.Empty;
            /// <summary>The exact text we last wrote to NickName. Used to detect user edits.</summary>
            public string LastWrittenNick { get; set; } = string.Empty;
        }

        private readonly Dictionary<(Guid DocSerial, Guid ComponentGuid), OriginalLabels> _originals = new();
        // The serial of the document we most recently translated — we use
        // it to cheaply decide whether ForgetDocument() needs to do work.
        // GH 8 GH_Document.DocumentID is a Guid, not an int.
        private Guid? _activeDocSerial;

        public ComponentTranslator(TranslationDictionary dict)
        {
            _dict = dict ?? throw new ArgumentNullException(nameof(dict));
        }

        /// <summary>
        /// Translate (or restore, depending on <paramref name="mode"/>) every
        /// visible component in <paramref name="document"/>. Safe to call
        /// repeatedly; idempotent under the same mode.
        /// </summary>
        /// <returns>The number of components whose labels were actually changed.</returns>
        public int ApplyToDocument(GH_Document document, LanguageMode mode)
        {
            if (document == null) return 0;
            var docSerial = document.DocumentID;
            _activeDocSerial = docSerial;

            // Forget snapshots that belong to other documents — keeps the
            // originals map from leaking across open/close cycles.
            EvictForeignSnapshots(docSerial);

            int changed = 0;
            foreach (var obj in document.Objects)
            {
                if (obj == null || obj.Attributes == null) continue;
                if (ApplyToObject(obj, docSerial, mode)) changed++;
            }
            return changed;
        }

        /// <summary>
        /// Translate (or restore) a single component. Public so DocumentHook
        /// can call it from component-added events without re-walking the
        /// whole document.
        /// </summary>
        public bool ApplyToObject(IGH_DocumentObject obj, LanguageMode mode)
            => ApplyToObject(obj, _activeDocSerial ?? Guid.Empty, mode);

        /// <summary>
        /// Translation entry point that takes an explicit document serial —
        /// use this when the caller already knows which document the
        /// component lives in (e.g. from a GH_DocumentObjectsAddedEventArgs).
        /// </summary>
        public bool ApplyToObject(IGH_DocumentObject obj, Guid docSerial, LanguageMode mode)
        {
            if (obj == null) return false;
            var componentGuid = SafeComponentGuid(obj);
            if (componentGuid == Guid.Empty) return false;
            var key = (docSerial, componentGuid);

            // English mode → restore original labels (if we ever translated
            // this component). Skipping unknown components is fine — they
            // were never translated, so nothing to do.
            if (mode == LanguageMode.English)
            {
                if (!_originals.TryGetValue(key, out var snap)) return false;
                if (WriteLabels(obj, snap.Name, snap.NickName))
                {
                    // We've restored the English text — the "last written"
                    // marker is now the restored original.
                    snap.LastWrittenNick = snap.NickName;
                    return true;
                }
                return false;
            }

            // Chinese / Bilingual mode → compute the display text and write it.
            var plugin = SafePlugin(obj);
            var lookupKey = ComponentKey.Build(plugin, obj.Name);
            // When in Bilingual mode the user wants "En / Zh" — we already
            // have the English half from the component's original NickName
            // (snapshotted on first translation). Use that snapshot to
            // construct NameEn if the dictionary doesn't already know it.
            EnsureEnglishHalfInEntry(obj, lookupKey, key);
            var display = _dict.GetDisplayText(lookupKey, mode);
            if (string.IsNullOrEmpty(display)) return false;

            if (!_originals.TryGetValue(key, out var snap2))
            {
                // First time we touch this component — snapshot originals.
                _originals[key] = new OriginalLabels
                {
                    Name = obj.Name ?? string.Empty,
                    NickName = obj.NickName ?? string.Empty,
                };
                snap2 = _originals[key];
            }

            // Respect user edits: if the user has manually changed NickName
            // away from what we last wrote, leave it alone. (We still write
            // Name, which GH draws on hover via Description-style tooltips.)
            var currentNick = obj.NickName ?? string.Empty;
            if (!string.IsNullOrEmpty(snap2.LastWrittenNick)
                && currentNick != snap2.LastWrittenNick
                && currentNick != snap2.NickName)
            {
                // User edited — don't clobber.
                return false;
            }

            // In bilingual mode we want the whole "En / Zh" string on the
            // canvas nickname. In Chinese mode we write just the Zh half.
            var newName = mode == LanguageMode.Bilingual && !string.IsNullOrEmpty(snap2.NickName)
                ? $"{snap2.NickName} / {display}"
                : display;

            if (WriteLabels(obj, newName, newName))
            {
                snap2.LastWrittenNick = newName;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Drop all snapshots belonging to documents other than the one with
        /// serial <paramref name="keepSerial"/>. Called automatically from
        /// <see cref="ApplyToDocument"/>, but can also be called explicitly
        /// when a document is closed (so the map doesn't grow forever).
        /// </summary>
        public void ForgetDocument(Guid keepSerial)
        {
            EvictForeignSnapshots(keepSerial);
        }

        /// <summary>
        /// Drop EVERY snapshot. Used when the user clears the dictionary or
        /// when the translator is being torn down — after this, switching
        /// to English mode will be a no-op until components are re-snapshotted.
        /// </summary>
        public void ForgetAll() => _originals.Clear();

        // ─── internals ─────────────────────────────────────────────────────

        private void EvictForeignSnapshots(Guid keepSerial)
        {
            if (_originals.Count == 0) return;
            var stale = _originals.Keys.Where(k => k.DocSerial != keepSerial).ToList();
            foreach (var k in stale) _originals.Remove(k);
        }

        /// <summary>
        /// Populate the dictionary entry's NameEn/NickNameEn with the
        /// component's original English text the first time we touch it.
        /// Without this, bilingual mode would have no English half to show
        /// (the dictionary only ships Chinese from the seed/packs).
        /// Idempotent — never overwrites an existing English value.
        /// </summary>
        private void EnsureEnglishHalfInEntry(IGH_DocumentObject obj, string lookupKey, (Guid DocSerial, Guid ComponentGuid) snapshotKey)
        {
            // Skip if the dictionary entry already has the English half —
            // packs and user-imported data may have set it deliberately.
            var existing = _dict.GetEntry(lookupKey);
            if (existing != null
                && (!string.IsNullOrEmpty(existing.NameEn) || !string.IsNullOrEmpty(existing.NickNameEn)))
                return;

            // Use the snapshot's original labels when we have them; otherwise
            // read from the live component (first-ever pass, no snapshot yet).
            string origNick, origName;
            if (_originals.TryGetValue(snapshotKey, out var snap))
            {
                origNick = snap.NickName;
                origName = snap.Name;
            }
            else
            {
                origNick = obj.NickName ?? string.Empty;
                origName = obj.Name ?? string.Empty;
            }

            var entry = existing ?? new TranslationEntry { Source = TranslationSource.Builtin };
            if (string.IsNullOrEmpty(entry.NameEn))    entry.NameEn    = origName;
            if (string.IsNullOrEmpty(entry.NickNameEn)) entry.NickNameEn = origNick;
            if (string.IsNullOrEmpty(entry.Name))       entry.Name      = _dict.GetTranslation(lookupKey) ?? origNick;
            if (string.IsNullOrEmpty(entry.NickName))   entry.NickName  = entry.Name;
            // Don't mark Source = User here — built-in seed entries already
            // exist, we just augmented them in-memory.
            _dict.AddOrUpdate(lookupKey, entry);
        }

        private static bool WriteLabels(IGH_DocumentObject obj, string newName, string newNick)
        {
            try
            {
                var nameChanged = false;
                var nickChanged = false;
                if (obj.Name != newName)
                {
                    obj.Name = newName;
                    nameChanged = true;
                }
                if (obj.NickName != newNick)
                {
                    obj.NickName = newNick;
                    nickChanged = true;
                }
                if (nameChanged || nickChanged)
                {
                    // R8 dropped the bool overload of ExpireLayout — the
                    // parameterless version invalidates the layout cache
                    // and forces a recompute on the next paint, which is
                    // exactly what we need after rewriting Name/NickName.
                    obj.Attributes?.ExpireLayout();
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                // Some component types (params, clusters, groups) refuse to
                // be renamed — silently no-op rather than crash the host.
                return false;
            }
        }

        private static string SafePlugin(IGH_DocumentObject obj)
        {
            try
            {
                var g = obj?.ComponentGuid;
                if (g.HasValue && g.Value != Guid.Empty) return g.Value.ToString("N");
            }
            catch { }
            return ComponentKey.FallbackPlugin;
        }

        private static Guid SafeComponentGuid(IGH_DocumentObject obj)
        {
            try
            {
                var g = obj?.ComponentGuid;
                return g ?? Guid.Empty;
            }
            catch { return Guid.Empty; }
        }
    }
}
