using System;
using System.Reflection;
using Grasshopper.Kernel;

namespace GHAITranslator;

/// <summary>
/// Resolves a <c>ComponentKey</c> for a given <c>IGH_DocumentObject</c>.
///
/// The key MUST be derived from the type's assembly + class name (or
/// <c>ComponentGuid</c>), never from <c>obj.Name</c> — because after the
/// first translation <c>obj.Name</c> is the Chinese string, and reopening a
/// saved file would then look up <c>"Native_点"</c> instead of
/// <c>"Native_Point"</c>.
/// </summary>
public static class GhAdapter
{
    /// <summary>
    /// Get the assembly short name for the type. <c>Grasshopper.dll</c>
    /// returns <c>"Grasshopper"</c> (matching <see cref="ComponentKey.NativePrefix"/>);
    /// everything else returns its actual assembly name.
    /// </summary>
    public static string PluginName(Type type)
    {
        if (type == null) return "User";
        var asm = type.Assembly.GetName().Name ?? "";
        if (asm.StartsWith("Grasshopper", StringComparison.OrdinalIgnoreCase))
            return "Grasshopper";
        return asm;
    }

    /// <summary>
    /// Stable lookup key for any <c>IGH_DocumentObject</c>.
    /// </summary>
    public static string KeyFor(IGH_DocumentObject obj)
    {
        if (obj == null) return "";
        var t = obj.GetType();
        var asm = PluginName(t);
        return ComponentKey.Build(asm, t.Name);
    }
}