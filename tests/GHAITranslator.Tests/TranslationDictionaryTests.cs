using System.Linq;
using GHAITranslator.Core;
using GHAITranslator.Core.Models;
using Xunit;

namespace GHAITranslator.Tests;

public class TranslationDictionaryTests
{
    [Fact]
    public void BuiltinSeed_merge_overlay_user_wins()
    {
        var dict = new TranslationDictionary(BuiltinSeed.All);
        Assert.True(dict.Contains("Native_Param_Point"));

        var overlay = new TranslationEntry
        {
            Key = "Native_Param_Point",
            Name = "POINT_OVERRIDE",
            NickName = "P",
            Description = "测试覆盖。",
            Category = "参数",
            NameEn = "Point",
        };
        dict.MergeOverlay(new[] { overlay });

        var e = dict.Get("Native_Param_Point");
        Assert.NotNull(e);
        Assert.Equal("POINT_OVERRIDE", e!.Name);
    }

    [Fact]
    public void Unknown_key_returns_null()
    {
        var dict = new TranslationDictionary(BuiltinSeed.All);
        Assert.Null(dict.Get("Native_NoSuchThing"));
    }

    [Fact]
    public void AddOrUpdate_ignores_empty_key()
    {
        var dict = new TranslationDictionary(BuiltinSeed.All);
        dict.AddOrUpdate(new TranslationEntry { Key = "", Name = "x" });
        Assert.Empty(dict.All().Where(e => e.Name == "x"));
    }
}