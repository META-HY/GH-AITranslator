using GHAITranslator.Core.Models;
using Xunit;

namespace GHAITranslator.Tests;

public class LanguageModeTests
{
    [Fact]
    public void DisplayName_roundtrip()
    {
        foreach (var m in new[] { LanguageMode.Chinese, LanguageMode.Bilingual, LanguageMode.English })
        {
            var s = m.ToDisplayName();
            var back = LanguageModeExtensions.FromDisplayName(s);
            Assert.Equal(m, back);
        }
    }

    [Fact]
    public void Unknown_string_defaults_to_Chinese()
    {
        Assert.Equal(LanguageMode.Chinese, LanguageModeExtensions.FromDisplayName("bogus"));
    }
}