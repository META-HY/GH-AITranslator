using GHAITranslator.Core;
using GHAITranslator.Core.Models;
using Xunit;

namespace GHAITranslator.Tests;

public class LanguageFormatterTests
{
    private static TranslationEntry Sample() => new TranslationEntry
    {
        Key = "Native_Point",
        Name = "点",
        NickName = "点",
        Description = "三维空间中的一个点。",
        Category = "参数",
        NameEn = "Point",
        DescriptionEn = "A point in 3D space.",
        CategoryEn = "Params",
    };

    [Fact]
    public void Chinese_mode_returns_chinese_only()
    {
        var e = Sample();
        Assert.Equal("点", LanguageFormatter.FormatName(e, LanguageMode.Chinese));
        Assert.Equal("点", LanguageFormatter.FormatNick(e, LanguageMode.Chinese));
        Assert.Equal("三维空间中的一个点。", LanguageFormatter.FormatDescription(e, LanguageMode.Chinese));
        Assert.Equal("参数", LanguageFormatter.FormatCategory(e, LanguageMode.Chinese));
    }

    [Fact]
    public void Bilingual_mode_uses_pipe_separator()
    {
        var e = Sample();
        Assert.Equal("点|Point", LanguageFormatter.FormatName(e, LanguageMode.Bilingual));
        Assert.Equal("三维空间中的一个点。|A point in 3D space.", LanguageFormatter.FormatDescription(e, LanguageMode.Bilingual));
        Assert.Equal("参数|Params", LanguageFormatter.FormatCategory(e, LanguageMode.Bilingual));
    }

    [Fact]
    public void English_mode_returns_english_only()
    {
        var e = Sample();
        Assert.Equal("Point", LanguageFormatter.FormatName(e, LanguageMode.English));
        Assert.Equal("A point in 3D space.", LanguageFormatter.FormatDescription(e, LanguageMode.English));
        Assert.Equal("Params", LanguageFormatter.FormatCategory(e, LanguageMode.English));
    }

    [Fact]
    public void Bilingual_separator_is_half_width_pipe()
    {
        Assert.Equal("|", LanguageFormatter.BilingualSeparator);
    }

    [Fact]
    public void Null_entry_returns_empty()
    {
        Assert.Equal("", LanguageFormatter.FormatName(null, LanguageMode.Chinese));
        Assert.Equal("", LanguageFormatter.FormatDescription(null, LanguageMode.English));
    }
}