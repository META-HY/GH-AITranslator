namespace GHAITranslator.Core.Models;

/// <summary>
/// Display mode the user has selected. Drives every UI string in the plugin.
/// </summary>
public enum LanguageMode
{
    /// <summary>纯中文 — 画布/Description/Category 全部显示中文。</summary>
    Chinese = 0,

    /// <summary>中英双语 — 格式 <c>中文|English</c>,半角竖线分隔。</summary>
    Bilingual = 1,

    /// <summary>纯英文 — 完全不翻译,等同插件未安装。</summary>
    English = 2,
}

public static class LanguageModeExtensions
{
    public static string ToDisplayName(this LanguageMode mode) => mode switch
    {
        LanguageMode.Chinese   => "中文",
        LanguageMode.Bilingual => "中英双语",
        LanguageMode.English   => "English",
        _ => "中文",
    };

    /// <summary>
    /// Round-trip parse from <see cref="ToDisplayName"/> output. Falls back to
    /// Chinese on unknown input so a corrupt settings file never breaks the plugin.
    /// </summary>
    public static LanguageMode FromDisplayName(string s) => s switch
    {
        "中英双语" => LanguageMode.Bilingual,
        "English"  => LanguageMode.English,
        "中文"     => LanguageMode.Chinese,
        _          => LanguageMode.Chinese,
    };
}