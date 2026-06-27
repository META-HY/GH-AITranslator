using System;
using System.IO;
using GHAITranslator.Core;
using Newtonsoft.Json;
using Xunit;

namespace GHAITranslator.Tests;

public class DictionaryIoTests
{
    [Fact]
    public void Load_missing_file_returns_empty_overlay()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
        var ovl = DictionaryIo.Load(path);
        Assert.NotNull(ovl);
        Assert.Empty(ovl.Entries);
    }

    [Fact]
    public void Save_then_load_round_trip()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
        var ovl = new DictionaryIo.OverlayFile
        {
            Version = 2,
            Entries =
            {
                new GHAITranslator.Core.Models.TranslationEntry
                {
                    Key = "Native_X", Name = "X", NickName = "X",
                    Description = "测试。", Category = "特殊",
                }
            }
        };
        Assert.True(DictionaryIo.Save(path, ovl));

        var loaded = DictionaryIo.Load(path);
        Assert.Single(loaded.Entries);
        Assert.Equal("Native_X", loaded.Entries[0].Key);
        Assert.Equal("X", loaded.Entries[0].Name);

        File.Delete(path);
    }

    [Fact]
    public void Load_corrupt_file_returns_empty_overlay()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
        File.WriteAllText(path, "{ this is not valid json");
        var ovl = DictionaryIo.Load(path);
        Assert.Empty(ovl.Entries);
        File.Delete(path);
    }
}