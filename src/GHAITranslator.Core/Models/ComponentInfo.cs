using System.Collections.Generic;

namespace GHAITranslator.Core.Models;

/// <summary>
/// Component metadata used as the translation source-of-truth.
/// Designed to be Rhino-free so it can be unit-tested on any platform.
/// </summary>
public class ComponentInfo
{
    /// <summary>Stable unique key: "{PluginName}_{ComponentName}".</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Original component name (internal).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Display nickname shown in Grasshopper.</summary>
    public string NickName { get; set; } = string.Empty;

    /// <summary>English description from the component author.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Source plugin / library name. Defaults to "Native" when missing.</summary>
    public string PluginName { get; set; } = "Native";

    public ParamInfo[] InputParams { get; set; } = System.Array.Empty<ParamInfo>();
    public ParamInfo[] OutputParams { get; set; } = System.Array.Empty<ParamInfo>();
}

public class ParamInfo
{
    public string Name { get; set; } = string.Empty;
    public string NickName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
