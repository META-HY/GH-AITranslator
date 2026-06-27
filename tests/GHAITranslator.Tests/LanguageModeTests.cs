using GHAITranslator.Core;
using GHAITranslator.Core.Models;
using Xunit;

namespace GHAITranslator.Tests
{
    /// <summary>
    /// Step-1 tests: dictionary's GetDisplayText + TranslationEntry's new
    /// NickName / NickNameEn / NameEn / HasChinese fields + backwards-
    /// compatible loading of v1 dictionaries that don't have these fields.
    /// </summary>
    public class LanguageModeTests : System.IDisposable
    {
        private readonly string _dir;
        private readonly string _file;

        public LanguageModeTests()
        {
            _dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ghaip-langmode", System.Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(_dir);
            _file = System.IO.Path.Combine(_dir, "dict.json");
        }

        public void Dispose() { try { System.IO.Directory.Delete(_dir, true); } catch { } }

        private TranslationDictionary NewDict()
        {
            var d = new TranslationDictionary(_file);
            d.Load();
            return d;
        }

        // ── HasChinese ──────────────────────────────────────────────────────

        [Fact]
        public void HasChinese_True_WhenNameSet()
        {
            var e = new TranslationEntry { Name = "点" };
            Assert.True(e.HasChinese);
        }

        [Fact]
        public void HasChinese_True_WhenOnlyNickNameSet()
        {
            var e = new TranslationEntry { NickName = "点" };
            Assert.True(e.HasChinese);
        }

        [Fact]
        public void HasChinese_False_WhenAllEmpty()
        {
            var e = new TranslationEntry { NickNameEn = "Point", NameEn = "Point" };
            Assert.False(e.HasChinese);
        }

        // ── GetDisplayText: Chinese mode ────────────────────────────────────

        [Fact]
        public void Display_Chinese_ReturnsNickNameWhenPresent()
        {
            var d = NewDict();
            d.AddOrUpdate("Native_Test", new TranslationEntry { Name = "测试组件", NickName = "测试" });
            Assert.Equal("测试", d.GetDisplayText("Native_Test", LanguageMode.Chinese));
        }

        [Fact]
        public void Display_Chinese_FallsBackToNameWhenNickEmpty()
        {
            var d = NewDict();
            d.AddOrUpdate("Native_Test", new TranslationEntry { Name = "测试组件", NickName = "" });
            Assert.Equal("测试组件", d.GetDisplayText("Native_Test", LanguageMode.Chinese));
        }

        // ── GetDisplayText: Bilingual mode ──────────────────────────────────

        [Fact]
        public void Display_Bilingual_CombinesNickNames()
        {
            var d = NewDict();
            d.AddOrUpdate("Native_Curve", new TranslationEntry
            {
                Name = "曲线",
                NickName = "曲线",
                NameEn = "Curve",
                NickNameEn = "Curve",
            });
            Assert.Equal("Curve / 曲线", d.GetDisplayText("Native_Curve", LanguageMode.Bilingual));
        }

        [Fact]
        public void Display_Bilingual_UsesFullNamesWhenNickEnMissing()
        {
            var d = NewDict();
            d.AddOrUpdate("Native_Curve", new TranslationEntry
            {
                Name = "曲线",
                NickName = "曲线",
                NameEn = "Curve",
                // NickNameEn intentionally missing
            });
            Assert.Equal("Curve / 曲线", d.GetDisplayText("Native_Curve", LanguageMode.Bilingual));
        }

        [Fact]
        public void Display_Bilingual_FallsBackToChineseWhenNoEnglish()
        {
            var d = NewDict();
            d.AddOrUpdate("Native_Curve", new TranslationEntry { Name = "曲线", NickName = "曲线" });
            Assert.Equal("曲线", d.GetDisplayText("Native_Curve", LanguageMode.Bilingual));
        }

        // ── GetDisplayText: English mode ────────────────────────────────────

        [Fact]
        public void Display_English_ReturnsNull()
        {
            var d = NewDict();
            d.AddOrUpdate("Native_Curve", new TranslationEntry { Name = "曲线", NickName = "曲线" });
            Assert.Null(d.GetDisplayText("Native_Curve", LanguageMode.English));
        }

        // ── GetDisplayText: edge cases ──────────────────────────────────────

        [Fact]
        public void Display_ReturnsNullOnUnknownKey()
        {
            var d = NewDict();
            Assert.Null(d.GetDisplayText("Native_DoesNotExist", LanguageMode.Chinese));
            Assert.Null(d.GetDisplayText("Native_DoesNotExist", LanguageMode.Bilingual));
            Assert.Null(d.GetDisplayText("Native_DoesNotExist", LanguageMode.English));
        }

        [Fact]
        public void Display_ReturnsNullWhenEntryHasNoChinese()
        {
            var d = NewDict();
            // An entry with only English fields shouldn't crash — Chinese mode
            // returns null and the caller leaves the component's original
            // English text alone.
            d.AddOrUpdate("Native_English", new TranslationEntry
            {
                Name = "",
                NickName = "",
                NameEn = "English Only",
                NickNameEn = "English Only",
            });
            Assert.Null(d.GetDisplayText("Native_English", LanguageMode.Chinese));
            Assert.Null(d.GetDisplayText("Native_English", LanguageMode.Bilingual));
        }

        [Fact]
        public void Display_HandlesEmptyKey()
        {
            var d = NewDict();
            Assert.Null(d.GetDisplayText("", LanguageMode.Chinese));
            Assert.Null(d.GetDisplayText("", LanguageMode.Bilingual));
            Assert.Null(d.GetDisplayText("", LanguageMode.English));
        }

        // ── v1 dictionary backwards compat ─────────────────────────────────

        [Fact]
        public void Load_V1Dictionary_WithoutNewFields_StillWorks()
        {
            // Write a v1-shaped dictionary: only Name + Description + Source.
            // Newtonsoft must leave the new fields at their "" defaults.
            var v1 = @"{
                ""Version"": ""1.0"",
                ""Entries"": {
                    ""Native_Curve"": {
                        ""Name"": ""曲线"",
                        ""Description"": ""curve thing"",
                        ""Inputs"": {},
                        ""Outputs"": {},
                        ""Source"": ""builtin"",
                        ""UpdatedAt"": ""2025-01-01T00:00:00Z""
                    }
                }
            }";
            System.IO.File.WriteAllText(_file, v1);
            var d = NewDict();
            Assert.Equal("曲线", d.GetDisplayText("Native_Curve", LanguageMode.Chinese));
            // Bilingual mode falls back to Chinese when no English is known.
            Assert.Equal("曲线", d.GetDisplayText("Native_Curve", LanguageMode.Bilingual));
        }

        [Fact]
        public void Load_V2Dictionary_WithAllFields_Roundtrips()
        {
            var v2 = @"{
                ""Version"": ""1.0"",
                ""Entries"": {
                    ""Native_Curve"": {
                        ""Name"": ""曲线"",
                        ""NickName"": ""曲线"",
                        ""NameEn"": ""Curve"",
                        ""NickNameEn"": ""Curve"",
                        ""Description"": ""curve thing"",
                        ""Inputs"": {},
                        ""Outputs"": {},
                        ""Source"": ""user"",
                        ""UpdatedAt"": ""2026-01-01T00:00:00Z""
                    }
                }
            }";
            System.IO.File.WriteAllText(_file, v2);
            var d = NewDict();
            Assert.Equal("Curve / 曲线", d.GetDisplayText("Native_Curve", LanguageMode.Bilingual));
        }

        // ── PluginSettings roundtrip ────────────────────────────────────────

        [Fact]
        public void Settings_Roundtrip_PreservesMode()
        {
            var s = new PluginSettings { Mode = LanguageMode.Bilingual };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(s);
            var s2 = Newtonsoft.Json.JsonConvert.DeserializeObject<PluginSettings>(json);
            Assert.Equal(LanguageMode.Bilingual, s2!.Mode);
        }

        [Fact]
        public void Settings_Default_IsChinese()
        {
            // The default must be Chinese so a first-run user sees the
            // GHChinese-style translated components straight away.
            Assert.Equal(LanguageMode.Chinese, new PluginSettings().Mode);
        }

        // ── BuiltinSeed integration ─────────────────────────────────────────

        [Fact]
        public void BuiltinSeed_Has_Native_Curve()
        {
            // The seed is internal so we reach in via a fresh dictionary
            // (Load() with no file installed the seed for us).
            var d = NewDict();
            var entry = d.GetEntry("Native_Curve");
            Assert.NotNull(entry);
            // Curve isn't in BuiltinSeed yet — sanity check that the seed
            // load didn't crash and that entries DO have Chinese content.
            if (entry != null)
                Assert.False(string.IsNullOrEmpty(entry.Name + entry.NickName));
        }

        [Fact]
        public void BuiltinSeed_Has_Common_Native_Components()
        {
            // These are the components a user reaches for in their first
            // session. Each must round-trip cleanly through the dictionary
            // and produce a non-empty Chinese display string.
            var d = NewDict();
            string[] keys =
            {
                "Native_Point", "Native_Line", "Native_Circle", "Native_Arc",
                "Native_Polyline", "Native_Rectangle", "Native_Plane",
                "Native_Number", "Native_Range", "Native_Random",
                "Native_Panel", "Native_Number_Slider", "Native_Boolean_Toggle",
                "Native_Colour_Swatch"
            };
            foreach (var k in keys)
            {
                var display = d.GetDisplayText(k, LanguageMode.Chinese);
                Assert.False(string.IsNullOrEmpty(display),
                    $"Built-in key {k} must have a non-empty Chinese display string.");
            }
        }
    }
}
