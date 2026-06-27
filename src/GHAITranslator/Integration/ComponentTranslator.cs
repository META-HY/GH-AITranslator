using System;
using GHAITranslator.Core;
using GHAITranslator.Core.Models;
using Grasshopper.Kernel;

namespace GHAITranslator.Integration;

/// <summary>
/// Applies a <see cref="TranslationEntry"/> to a live <c>IGH_DocumentObject</c>.
///
/// Writes the five localized fields:
///   <c>Name</c>, <c>NickName</c>, <c>Description</c>, <c>Category</c>, <c>SubCategory</c>
///
/// Rendered through <see cref="LanguageFormatter"/> so the same code path
/// handles Chinese, Bilingual, and English modes.
/// </summary>
public static class ComponentTranslator
{
    /// <summary>
    /// Returns <c>true</c> if any field changed. Caller is responsible for
    /// expiring layout / refreshing canvas if so.
    /// </summary>
    public static bool ApplyToObject(IGH_DocumentObject obj, TranslationEntry entry, LanguageMode mode)
    {
        if (obj == null || entry == null) return false;
        try
        {
            var newName  = LanguageFormatter.FormatName(entry, mode);
            var newNick  = LanguageFormatter.FormatNick(entry, mode);
            var newDesc  = LanguageFormatter.FormatDescription(entry, mode);
            var newCat   = LanguageFormatter.FormatCategory(entry, mode);

            var any = false;
            if (!string.IsNullOrEmpty(newName) && obj.Name != newName)
            {
                obj.Name = newName; any = true;
            }
            if (!string.IsNullOrEmpty(newNick) && obj.NickName != newNick)
            {
                obj.NickName = newNick; any = true;
            }
            if (!string.IsNullOrEmpty(newDesc) && obj.Description != newDesc)
            {
                obj.Description = newDesc; any = true;
            }
            if (!string.IsNullOrEmpty(newCat) && obj.Category != newCat)
            {
                obj.Category = newCat; any = true;
            }
            return any;
        }
        catch (Exception ex)
        {
            Log.Warn($"ComponentTranslator.ApplyToObject failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Reverse of <see cref="ApplyToObject"/>: restore the original English
    /// strings stored on <see cref="TranslationEntry.NameEn"/> etc. Used by
    /// the "Restore English" menu item.
    /// </summary>
    public static bool RestoreEnglish(IGH_DocumentObject obj, TranslationEntry entry)
    {
        if (obj == null || entry == null) return false;
        try
        {
            var any = false;
            if (!string.IsNullOrEmpty(entry.NameEn) && obj.Name != entry.NameEn)
            {
                obj.Name = entry.NameEn; any = true;
            }
            if (!string.IsNullOrEmpty(entry.DescriptionEn) && obj.Description != entry.DescriptionEn)
            {
                obj.Description = entry.DescriptionEn; any = true;
            }
            if (!string.IsNullOrEmpty(entry.CategoryEn) && obj.Category != entry.CategoryEn)
            {
                obj.Category = entry.CategoryEn; any = true;
            }
            // NickName has no English mirror stored; we keep current nick.
            return any;
        }
        catch (Exception ex)
        {
            Log.Warn($"ComponentTranslator.RestoreEnglish failed: {ex.Message}");
            return false;
        }
    }
}