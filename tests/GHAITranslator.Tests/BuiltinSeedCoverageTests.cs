using System.Linq;
using GHAITranslator.Core;
using GHAITranslator.Core.Models;
using Xunit;

namespace GHAITranslator.Tests;

/// <summary>
/// Enforces the publish-quality rules from <c>DictionarySpec.md</c>.
/// Every entry in <see cref="BuiltinSeed.All"/> must:
///   - be complete (5 mandatory fields non-empty)
///   - end its Description with <c>。</c>
///   - have a NickName ≤ 2 Chinese characters
///   - have a unique Key
///   - have a non-empty Category from the canonical list
/// </summary>
public class BuiltinSeedCoverageTests
{
    private static readonly string[] CanonicalCategories =
    {
        "参数", "几何", "数学", "向量", "曲线", "曲面", "网格",
        "相交", "变换", "显示", "逻辑", "脚本", "输入", "输出",
        "集合", "树形数据", "特殊",
    };

    [Fact]
    public void Minimum_250_entries()
    {
        Assert.True(BuiltinSeed.All.Count >= 250,
            $"BuiltinSeed must contain ≥250 entries, has {BuiltinSeed.All.Count}.");
    }

    [Fact]
    public void All_keys_unique()
    {
        var keys = BuiltinSeed.All.Select(e => e.Key).ToList();
        var dups = keys.GroupBy(k => k).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Assert.Empty(dups);
    }

    [Fact]
    public void All_keys_have_native_prefix()
    {
        foreach (var e in BuiltinSeed.All)
        {
            Assert.StartsWith("Native_", e.Key);
        }
    }

    [Fact]
    public void All_entries_complete()
    {
        var incomplete = BuiltinSeed.All.Where(e => !e.IsComplete).Select(e => e.Key).ToList();
        Assert.True(incomplete.Count == 0,
            $"Incomplete entries: {string.Join(", ", incomplete)}");
    }

    [Fact]
    public void All_descriptions_end_with_period_chinese()
    {
        var bad = BuiltinSeed.All.Where(e => !e.Description.EndsWith("。")).Select(e => e.Key).ToList();
        Assert.True(bad.Count == 0,
            $"Description not ending with 。 : {string.Join(", ", bad)}");
    }

    [Fact]
    public void All_nicknames_short()
    {
        var longNames = BuiltinSeed.All
            .Where(e => e.NickName.Length > 2)
            .Select(e => $"{e.Key}={e.NickName}")
            .ToList();
        Assert.True(longNames.Count == 0,
            $"NickName > 2 chars: {string.Join(", ", longNames)}");
    }

    [Fact]
    public void All_categories_canonical()
    {
        var bad = BuiltinSeed.All
            .Where(e => !CanonicalCategories.Contains(e.Category))
            .Select(e => $"{e.Key}={e.Category}")
            .ToList();
        Assert.True(bad.Count == 0,
            $"Non-canonical Category: {string.Join(", ", bad)}");
    }

    [Fact]
    public void All_entries_have_english_mirror()
    {
        var bad = BuiltinSeed.All.Where(e => string.IsNullOrEmpty(e.NameEn)).Select(e => e.Key).ToList();
        Assert.True(bad.Count == 0,
            $"Missing NameEn: {string.Join(", ", bad)}");
    }

    [Fact]
    public void Names_contain_no_english()
    {
        // Allow-list of technical proper nouns that must stay in Latin script.
        var allowed = new[] { "NURBS", "B-rep", "3D", "C#", "VB", "Python", "Python 3", "GH", "XYZ" };
        var bad = BuiltinSeed.All
            .Where(e => ContainsLatinNotInAllowList(e.Name, allowed))
            .Select(e => $"{e.Key}={e.Name}")
            .ToList();
        Assert.True(bad.Count == 0,
            $"Chinese Name contains Latin chars (excluding technical proper nouns): {string.Join(", ", bad)}");
    }

    private static bool ContainsLatinNotInAllowList(string s, string[] allowed)
    {
        // A "latin run" is a contiguous sequence of letters/digits/spaces/#
        // /- We flush at CJK boundaries and check each run against the allow-list.
        // Examples:
        //   "Python 3 脚本"  → run "Python 3 " before "脚" → matches "Python 3"
        //   "转 B-rep"       → run " B-rep" before " (" → matches "B-rep"
        //   "GH 脚本"        → run "GH " before "脚" → matches "GH"
        var latinSeq = "";
        var allowedSet = new System.Collections.Generic.HashSet<string>(allowed);
        foreach (var c in s)
        {
            bool isAsciiLetter = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
            bool isAsciiDigit  = (c >= '0' && c <= '9');
            if (isAsciiLetter || isAsciiDigit || c == '#' || c == '-' || c == ' ')
                latinSeq += c;
            else if (latinSeq.Length > 0)
            {
                if (!allowedSet.Contains(latinSeq.Trim())) return true;
                latinSeq = "";
            }
        }
        if (latinSeq.Length > 0 && !allowedSet.Contains(latinSeq.Trim())) return true;
        return false;
    }
}