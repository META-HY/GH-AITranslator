using System;
using System.IO;
using System.Linq;
using GHAITranslator.Core;
using GHAITranslator.Core.Models;
using Xunit;

namespace GHAITranslator.Tests
{
    public class DictionaryIoTests : IDisposable
    {
        private readonly string _dir;
        private readonly string _dictFile;
        private readonly TranslationDictionary _dict;

        public DictionaryIoTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "ghaip-io", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _dictFile = Path.Combine(_dir, "dictionary.json");
            _dict = new TranslationDictionary(_dictFile);
            _dict.Load();
        }

        public void Dispose() { try { Directory.Delete(_dir, true); } catch { } }

        [Fact]
        public void Export_WritesFile_WithAllEntries()
        {
            var target = Path.Combine(_dir, "export.json");
            var r = DictionaryIo.Export(_dict, target);
            Assert.True(File.Exists(target));
            Assert.Equal(_dict.Count, r.Total);
            Assert.True(r.Builtin > 0);
        }

        [Fact]
        public void Import_MergeKeep_SkipsExisting()
        {
            _dict.AddOrUpdate("Test_Component", new TranslationEntry
            {
                Name = "原始值",
                Source = TranslationSource.User
            });

            var foreign = Path.Combine(_dir, "foreign.json");
            File.WriteAllText(foreign, @"{
                ""version"": ""1.0"",
                ""entries"": {
                    ""Test_Component"": { ""name"": ""新值"", ""source"": ""ai"" },
                    ""Test_NewOne"":   { ""name"": ""新的"", ""source"": ""ai"" }
                }
            }");

            var r = DictionaryIo.Import(_dict, foreign, DictionaryIo.ImportMode.MergeKeep);
            Assert.Equal(1, r.Imported);
            Assert.Equal(0, r.Overwritten);
            Assert.Equal(1, r.Skipped);
            // original value preserved
            Assert.Equal("原始值", _dict.GetTranslation("Test_Component"));
            // new entry added
            Assert.Equal("新的", _dict.GetTranslation("Test_NewOne"));
        }

        [Fact]
        public void Import_MergeOverwrite_OverwritesExisting()
        {
            _dict.AddOrUpdate("Test_Component", new TranslationEntry
            {
                Name = "原始值",
                Source = TranslationSource.User
            });
            var foreign = Path.Combine(_dir, "foreign.json");
            File.WriteAllText(foreign, @"{
                ""version"": ""1.0"",
                ""entries"": {
                    ""Test_Component"": { ""name"": ""新值"", ""source"": ""ai"" }
                }
            }");

            var r = DictionaryIo.Import(_dict, foreign, DictionaryIo.ImportMode.MergeOverwrite);
            Assert.Equal(0, r.Imported);
            Assert.Equal(1, r.Overwritten);
            Assert.Equal("新值", _dict.GetTranslation("Test_Component"));
        }

        [Fact]
        public void Import_Replace_DropsAllAndReloads()
        {
            // builtins should be gone after Replace
            Assert.Equal("点", _dict.GetTranslation("Native_Point"));
            var foreign = Path.Combine(_dir, "foreign.json");
            File.WriteAllText(foreign, @"{
                ""version"": ""1.0"",
                ""entries"": {
                    ""Custom_Alpha"": { ""name"": ""甲"", ""source"": ""user"" }
                }
            }");
            DictionaryIo.Import(_dict, foreign, DictionaryIo.ImportMode.Replace);
            Assert.Null(_dict.GetTranslation("Native_Point"));
            Assert.Equal("甲", _dict.GetTranslation("Custom_Alpha"));
        }

        [Fact]
        public void RoundTrip_ExportThenImport_MergeOverwrite_PreservesAll()
        {
            // 1) export
            var path = Path.Combine(_dir, "roundtrip.json");
            DictionaryIo.Export(_dict, path);

            // sanity: original dict should be untouched by export
            Assert.True(_dict.Count > 0);
            var sourceCount = _dict.Count;

            // 2) into a fresh dictionary that already has the built-in seed
            var fresh = new TranslationDictionary(Path.Combine(_dir, "fresh.json"));
            fresh.Load();
            Assert.Equal(sourceCount, fresh.Count);  // both have the same built-in seed

            // 3) import the export — should leave count identical
            var r = DictionaryIo.Import(fresh, path, DictionaryIo.ImportMode.MergeOverwrite);
            Assert.Equal(0, r.Imported);   // all keys already present
            Assert.Equal(sourceCount, r.Overwritten);
            Assert.Equal(sourceCount, fresh.Count);
        }

        [Fact]
        public void Import_MissingFile_Throws()
        {
            Assert.Throws<FileNotFoundException>(() =>
                DictionaryIo.Import(_dict, "/nonexistent/foo.json", DictionaryIo.ImportMode.MergeKeep));
        }
    }
}
