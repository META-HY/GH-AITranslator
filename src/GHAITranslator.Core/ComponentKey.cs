namespace GHAITranslator.Core;

/// <summary>
/// Builds the lookup key used by <see cref="TranslationDictionary"/>.
///
/// Format:
///   Built-in GH component:        <c>"Native_Point"</c>
///   Third-party assembly:          <c>"Heron_HeronGh"</c>  (assembly short name + class)
///   User-saved custom (unknown):  <c>"User_&lt;ClassName&gt;_&lt;guid-prefix&gt;"</c>
///
/// The key is stable across save/reload because it is derived from
/// <c>ComponentGuid</c> (via the user's dictionary file) or assembly name +
/// class (via BuiltinSeed). It is **not** derived from <c>obj.Name</c>, which
/// is the localized Chinese string after first translation and would break
/// reload.
/// </summary>
public static class ComponentKey
{
    public const string NativePrefix = "Native_";

    public static string Build(string pluginAssemblyName, string className)
    {
        if (string.IsNullOrEmpty(pluginAssemblyName))
            return $"{NativePrefix.TrimEnd('_')}_{className}";
        return pluginAssemblyName == "Grasshopper"
            ? $"{NativePrefix}{className}"
            : $"{pluginAssemblyName}_{className}";
    }
}