// Rhino-side adapter for the dictionary model. The translation pipeline talks
// to IComponentSource, not to IGH_DocumentObject directly, so we can swap the
// real GH canvas in production and a fake list in tests.

using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using GHAITranslator.Core;

namespace GHAITranslator.Integration
{
    /// <summary>Minimal slice of an IGH_DocumentObject that the translator cares about.</summary>
    public interface IGhDocumentObject
    {
        string Name { get; }
        string NickName { get; }
        string Description { get; }
        string PluginName { get; }
        bool Visible { get; }
        IEnumerable<IGhParam> InputParams { get; }
        IEnumerable<IGhParam> OutputParams { get; }
    }

    public interface IGhParam
    {
        string Name { get; }
        string NickName { get; }
        string Description { get; }
    }

    /// <summary>Source of canvas objects. Lets the renderer be tested without a GH canvas.</summary>
    public interface IComponentSource
    {
        IEnumerable<IGhDocumentObject> All { get; }
    }

    /// <summary>Real implementation backed by a Grasshopper canvas.</summary>
    internal sealed class CanvasComponentSource : IComponentSource
    {
        public IEnumerable<IGhDocumentObject> All
        {
            get
            {
                var doc = Grasshopper.Instances.ActiveCanvas?.Document;
                if (doc == null) return Enumerable.Empty<IGhDocumentObject>();
                return doc.Objects.Select(o => new GhAdapter(o)).OfType<IGhDocumentObject>();
            }
        }

        private sealed class GhAdapter : IGhDocumentObject
        {
            private readonly IGH_DocumentObject _o;
            public GhAdapter(IGH_DocumentObject o) { _o = o; }
            public string Name => _o.Name ?? string.Empty;
            public string NickName => _o.NickName ?? string.Empty;
            public string Description => _o.Description ?? string.Empty;
            public string PluginName => SafePluginName(_o);
            // R8 dropped IGH_Attributes.Visible — visibility is now a document-
            // level concept, and any object with a non-null Attributes is paintable.
            public bool Visible => _o.Attributes != null;

            public IEnumerable<IGhParam> InputParams
            {
                get
                {
                    if (_o is IGH_Component c)
                        return c.Params.Input.Select(p => (IGhParam)new ParamAdapter(p));
                    return Enumerable.Empty<IGhParam>();
                }
            }
            public IEnumerable<IGhParam> OutputParams
            {
                get
                {
                    if (_o is IGH_Component c)
                        return c.Params.Output.Select(p => (IGhParam)new ParamAdapter(p));
                    return Enumerable.Empty<IGhParam>();
                }
            }

            private static string SafePluginName(IGH_DocumentObject obj)
            {
                try
                {
                    // Match ComponentTranslator.SafePlugin: native Grasshopper
                    // components fall under the "Native" canonical key
                    // (their entries live in BuiltinSeed); third-party plugins
                    // get a stable key from their assembly name so the AI
                    // translation round-trip writes a key that future runs
                    // can look up without burning a GUID per component.
                    var t = obj?.GetType();
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
                // R8 dropped IGH_DocumentObject.Library — fall back to the
                // component GUID as a stable per-plugin key.
                try
                {
                    var g = obj?.ComponentGuid;
                    if (g.HasValue && g.Value != Guid.Empty) return g.Value.ToString("N");
                }
                catch { }
                return ComponentKey.FallbackPlugin;
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
    }

    /// <summary>
    /// Pure function: build a <see cref="Core.Models.ComponentInfo"/> from a
    /// <see cref="IGhDocumentObject"/>. Pulled out so it can be tested with
    /// a fake implementation.
    /// </summary>
    internal static class ComponentInfoBuilder
    {
        public static Core.Models.ComponentInfo Build(IGhDocumentObject obj)
        {
            if (obj == null) return null;
            var info = new Core.Models.ComponentInfo
            {
                Name = obj.Name ?? string.Empty,
                NickName = obj.NickName ?? string.Empty,
                Description = obj.Description ?? string.Empty,
                PluginName = string.IsNullOrEmpty(obj.PluginName) ? ComponentKey.FallbackPlugin : obj.PluginName,
                InputParams = obj.InputParams.Select(p => new Core.Models.ParamInfo
                {
                    Name = p.Name ?? string.Empty,
                    NickName = p.NickName ?? string.Empty,
                    Description = p.Description ?? string.Empty
                }).ToArray(),
                OutputParams = obj.OutputParams.Select(p => new Core.Models.ParamInfo
                {
                    Name = p.Name ?? string.Empty,
                    NickName = p.NickName ?? string.Empty,
                    Description = p.Description ?? string.Empty
                }).ToArray()
            };
            info.Key = ComponentKey.Build(info);
            return info;
        }
    }
}
