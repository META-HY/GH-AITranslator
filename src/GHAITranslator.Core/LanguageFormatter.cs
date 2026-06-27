using System;
using GHAITranslator.Core.Models;

namespace GHAITranslator.Core;

/// <summary>
/// Renders the five localized fields according to the active
/// <see cref="LanguageMode"/>. Pure function — no side effects.
///
/// Bilingual format: <c>中文|English</c> (half-width pipe, no spaces).
/// </summary>
public static class LanguageFormatter
{
    public const string BilingualSeparator = "|";

    public static string FormatName(TranslationEntry? entry, LanguageMode mode)
    {
        if (entry == null) return "";
        return mode switch
        {
            LanguageMode.English   => entry.NameEn ?? entry.Name,
            LanguageMode.Bilingual => Join(entry.Name, entry.NameEn),
            _                      => entry.Name,
        };
    }

    public static string FormatNick(TranslationEntry? entry, LanguageMode mode)
    {
        if (entry == null) return "";
        return mode switch
        {
            LanguageMode.English   => ShortenEnglish(entry.NameEn ?? entry.NickName),
            LanguageMode.Bilingual => JoinShort(entry.NickName, ShortenEnglish(entry.NameEn)),
            _                      => entry.NickName,
        };
    }

    public static string FormatDescription(TranslationEntry? entry, LanguageMode mode)
    {
        if (entry == null) return "";
        return mode switch
        {
            LanguageMode.English   => entry.DescriptionEn ?? entry.Description,
            LanguageMode.Bilingual => Join(entry.Description, entry.DescriptionEn),
            _                      => entry.Description,
        };
    }

    public static string FormatCategory(TranslationEntry? entry, LanguageMode mode)
    {
        if (entry == null) return "";
        return mode switch
        {
            LanguageMode.English   => entry.CategoryEn ?? entry.Category,
            LanguageMode.Bilingual => Join(entry.Category, entry.CategoryEn),
            _                      => entry.Category,
        };
    }

    private static string Join(string cn, string? en)
    {
        if (string.IsNullOrEmpty(en)) return cn;
        return cn + BilingualSeparator + en;
    }

    /// <summary>
    /// For NickName the English side is often long (e.g. <c>"Pt"</c> vs
    /// <c>"Point"</c>); for port labels we prefer the short abbreviation
    /// that GH itself uses. Falls back to the first letter of the English
    /// Name when no short form is known.
    /// </summary>
    private static string ShortenEnglish(string? en)
    {
        if (string.IsNullOrEmpty(en)) return "";
        // After IsNullOrEmpty, en is non-null.
        var s = en!;
        if (s.Length <= 3) return s;
        // Common GH convention: keep up to 3 chars.
        return s.Substring(0, Math.Min(3, s.Length));
    }

    private static string JoinShort(string cn, string? en)
    {
        if (string.IsNullOrEmpty(en)) return cn;
        return cn + BilingualSeparator + en;
    }
}