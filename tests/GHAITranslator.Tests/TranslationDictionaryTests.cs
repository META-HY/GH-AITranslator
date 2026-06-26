using System.IO;
using GHAITranslator.Core;
using Xunit;

namespace GHAITranslator.Tests
{
    public class TranslationDictionaryTests : System.IDisposable
    {
        private readonly string _dir;
        private readonly string _file;

        public TranslationDictionaryTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "ghaip-tests", System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _file = Path.Combine(_dir, "dictionary.json");
        }

        public void Dispose() { try { Directory.Delete(_dir, true); } catch { } }

        [Fact]
        public void Load_OnMissingFile_SeedsBuiltin_AndPersists()
        {
            var dict = new TranslationDictionary(_file);
            dict.Load();
            Assert.True(dict.Count > 0);
            Assert.True(File.Exists(_file));
            // built-in 'Point' should be in there
            Assert.Equal("点", dict.GetTranslation("Native_Point"));
        }

        [Fact]
        public void GetTranslation_ReturnsNullOnUnknownKey()
        {
            var dict = new TranslationDictionary(_file);
            dict.Load();
            Assert.Null(dict.GetTranslation("Native_NotAComponent"));
        }

        [Fact]
        public void AddOrUpdate_OverridesAndPersists()
        {
            var dict = new TranslationDictionary(_file);
            dict.Load();
            dict.AddOrUpdate("Native_Point", new Core.Models.TranslationEntry
            {
                Name = "点(自定义)",
                Source = TranslationSource.User
            });
            dict.Save();

            var dict2 = new TranslationDictionary(_file);
            dict2.Load();
            Assert.Equal("点(自定义)", dict2.GetTranslation("Native_Point"));
        }

        [Fact]
        public void Load_OnCorruptFile_FallsBackToSeed_AndDoesNotThrow()
        {
            File.WriteAllText(_file, "{ this is not json");
            var dict = new TranslationDictionary(_file);
            dict.Load();
            Assert.True(dict.Count > 0);
        }

        [Fact]
        public void Merge_RespectsOverwriteFlag()
        {
            var dict = new TranslationDictionary(_file);
            dict.Load();
            var original = dict.GetTranslation("Native_Point");

            var updates = new[] {
                new System.Collections.Generic.KeyValuePair<string, Core.Models.TranslationEntry>(
                    "Native_Point",
                    new Core.Models.TranslationEntry { Name = "should-not-apply", Source = "user" })
            };
            dict.Merge(updates, overwrite: false);
            Assert.Equal(original, dict.GetTranslation("Native_Point"));

            dict.Merge(updates, overwrite: true);
            Assert.Equal("should-not-apply", dict.GetTranslation("Native_Point"));
        }

        [Fact]
        public void BuiltinSeed_CoversP0Components()
        {
            var dict = new TranslationDictionary(_file);
            dict.Load();
            // sanity-check the components the doc explicitly calls out
            Assert.Equal("点", dict.GetTranslation("Native_Point"));
            Assert.Equal("线", dict.GetTranslation("Native_Line"));
            Assert.Equal("圆", dict.GetTranslation("Native_Circle"));
            Assert.Equal("面板", dict.GetTranslation("Native_Panel"));
        }
    }
}
