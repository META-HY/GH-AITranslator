namespace GHAITranslator.Core.Models;

/// <summary>
/// Metadata that the AI translation pipeline needs to produce a
/// <see cref="TranslationEntry"/>. Captured from a real GH_Component instance
/// before calling the AI; never persisted.
/// </summary>
public sealed class ComponentInfo
{
    public string ClassName { get; init; } = "";
    public string FullName  { get; init; } = "";   // Namespace.ClassName
    public string Assembly  { get; init; } = "";   // e.g. "Grasshopper" or "Heron"
    public string Category  { get; init; } = "";
    public string SubCategory { get; init; } = "";
    public string OriginalName { get; init; } = "";
    public string OriginalNickName { get; init; } = "";
    public string OriginalDescription { get; init; } = "";
    public string ComponentGuid { get; init; } = "";

    /// <summary>
    /// Stable lookup key. Format:
    ///   Built-in GH: <c>"Native_Point"</c>
    ///   Third-party: <c>"Heron_HeronGh"</c>
    ///   User-saved:  <c>"User_HeronGh_&lt;guid&gt;"</c>
    /// </summary>
    public string LookupKey { get; init; } = "";
}