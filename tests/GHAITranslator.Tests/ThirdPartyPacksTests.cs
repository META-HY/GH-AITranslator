using System;
using System.IO;
using GHAITranslator.Core;
using GHAITranslator.Core.Models;
using GHAITranslator.Core.Packs;
using Xunit;

namespace GHAITranslator.Tests
{
    public class ThirdPartyPacksTests : IDisposable
    {
        private readonly string _dir;
        private readonly string _file;

        public ThirdPartyPacksTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "ghaip-packs", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _file = Path.Combine(_dir, "dictionary.json");
        }

        public void Dispose() { try { Directory.Delete(_dir, true); } catch { } }

        [Fact]
        public void All_FourPacksRegistered()
        {
            Assert.Contains(ThirdPartyPacks.All, p => p.PluginName == "Kangaroo2");
            Assert.Contains(ThirdPartyPacks.All, p => p.PluginName == "Weaverbird");
            Assert.Contains(ThirdPartyPacks.All, p => p.PluginName == "LunchBox");
            Assert.Contains(ThirdPartyPacks.All, p => p.PluginName == "OpenNest");
        }

        [Fact]
        public void AllPacks_HaveAtLeastThreeEntries()
        {
            foreach (var p in ThirdPartyPacks.All)
                Assert.True(p.Entries.Count >= 3, $"{p.PluginName} has too few entries");
        }

        [Fact]
        public void AllPacks_KeysMatchCanonicalRule()
        {
            foreach (var p in ThirdPartyPacks.All)
            {
                foreach (var k in p.Entries.Keys)
                    Assert.StartsWith($"{p.PluginName}_", k);
                foreach (var e in p.Entries.Values)
                {
                    Assert.False(string.IsNullOrWhiteSpace(e.Name), $"{p.PluginName} has empty translation");
                    Assert.False(string.IsNullOrWhiteSpace(e.Description), $"{p.PluginName} has empty description");
                }
            }
        }

        [Fact]
        public void AddPack_PopulatesDictionary()
        {
            var dict = new TranslationDictionary(_file);
            dict.Load();
            var kangaroo = ThirdPartyPacks.FindByPlugin("Kangaroo2");
            Assert.NotNull(kangaroo);
            var added = dict.AddPack(kangaroo!);
            Assert.True(added > 0);
            Assert.NotNull(dict.GetEntry("Kangaroo2_BendGoal"));
            Assert.Equal("弯曲目标", dict.GetTranslation("Kangaroo2_BendGoal"));
        }

        [Fact]
        public void AddPack_DoesNotOverwriteUserEditedEntries()
        {
            var dict = new TranslationDictionary(_file);
            dict.Load();
            // User edits a Kangaroo entry to a custom value
            dict.AddOrUpdate("Kangaroo2_BendGoal", new TranslationEntry
            {
                Name = "我的弯曲",
                Source = TranslationSource.User
            });
            var kangaroo = ThirdPartyPacks.FindByPlugin("Kangaroo2")!;
            dict.AddPack(kangaroo);
            Assert.Equal("我的弯曲", dict.GetTranslation("Kangaroo2_BendGoal"));
        }

        [Fact]
        public void AddPack_OverwritesBuiltinSourceEntries()
        {
            // a builtin entry exists already; pack should overwrite it
            var dict = new TranslationDictionary(_file);
            dict.Load();
            var kangaroo = ThirdPartyPacks.FindByPlugin("Kangaroo2")!;
            dict.AddPack(kangaroo);
            dict.AddPack(kangaroo); // second time: should re-add (still not user)
            var entry = dict.GetEntry("Kangaroo2_BendGoal");
            Assert.NotNull(entry);
            Assert.Equal("弯曲目标", entry!.Name);
        }

        [Fact]
        public void FindByPlugin_NullOrUnknown_ReturnsNull()
        {
            Assert.Null(ThirdPartyPacks.FindByPlugin(null));
            Assert.Null(ThirdPartyPacks.FindByPlugin(""));
            Assert.Null(ThirdPartyPacks.FindByPlugin("Nonexistent"));
        }
    }
}
