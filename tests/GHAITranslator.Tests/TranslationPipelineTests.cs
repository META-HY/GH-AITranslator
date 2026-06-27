using System.Threading;
using System.Threading.Tasks;
using GHAITranslator.Core;
using GHAITranslator.Core.Models;
using Xunit;

namespace GHAITranslator.Tests;

public class TranslationPipelineTests
{
    private sealed class FakeAi : IAiClient
    {
        public int Calls;
        public Task<TranslationEntry?> TranslateAsync(ComponentInfo info, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult<TranslationEntry?>(new TranslationEntry
            {
                Key = info.LookupKey,
                Name = "测试",
                NickName = "测",
                Description = "测试描述。",
                Category = "特殊",
                NameEn = info.OriginalName,
            });
        }
    }

    [Fact]
    public async Task Dictionary_hit_skips_AI()
    {
        var dict = new TranslationDictionary(BuiltinSeed.All);
        var ai = new FakeAi();
        var pipeline = new TranslationPipeline(dict, ai);

        var info = new ComponentInfo
        {
            ClassName = "Param_Point",
            FullName  = "Grasshopper.Kernel.Parameters.Param_Point",
            Assembly  = "Grasshopper",
            OriginalName = "Point",
            LookupKey = "Native_Param_Point",
        };
        var entry = await pipeline.ResolveAsync(info);
        Assert.NotNull(entry);
        Assert.Equal("点", entry!.Name);
        Assert.Equal(0, ai.Calls);
    }

    [Fact]
    public async Task Dictionary_miss_falls_through_to_AI()
    {
        var dict = new TranslationDictionary(BuiltinSeed.All);
        var ai = new FakeAi();
        var pipeline = new TranslationPipeline(dict, ai);

        var info = new ComponentInfo
        {
            ClassName = "SomeUnknownComp",
            FullName  = "Foo.Bar.SomeUnknownComp",
            Assembly  = "Foo",
            OriginalName = "SomeUnknownComp",
            LookupKey = "Native_SomeUnknownComp",
        };
        var entry = await pipeline.ResolveAsync(info);
        Assert.NotNull(entry);
        Assert.Equal("测试", entry!.Name);
        Assert.Equal(1, ai.Calls);
        // Persisted back into dictionary
        Assert.NotNull(dict.Get("Native_SomeUnknownComp"));
    }
}