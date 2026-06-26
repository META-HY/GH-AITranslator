using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GHAITranslator.Core;
using GHAITranslator.Core.Models;
using Xunit;

namespace GHAITranslator.Tests
{
    /// <summary>
    /// The most important test: P0 acceptance criterion.
    /// Local dictionary hit MUST NOT call the AI client.
    /// </summary>
    public class TranslationDispatcherP0Tests : System.IDisposable
    {
        private readonly string _file;
        private readonly string _dir;

        public TranslationDispatcherP0Tests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "ghaip-disp", System.Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(_dir);
            _file = System.IO.Path.Combine(_dir, "dictionary.json");
        }

        public void Dispose() { try { System.IO.Directory.Delete(_dir, true); } catch { } }

        private class CountingAi : IAiClient
        {
            public int Calls;
            public Task<TranslationEntry?> TranslateAsync(ComponentInfo c, CancellationToken ct = default)
            {
                Interlocked.Increment(ref Calls);
                return Task.FromResult<TranslationEntry?>(null);
            }
        }

        [Fact]
        public async Task LocalHit_NeverInvokesAi()
        {
            var dict = new TranslationDictionary(_file);
            dict.Load();
            var ai = new CountingAi();
            var dispatcher = new TranslationDispatcher(dict, ai);

            var comp = new ComponentInfo
            {
                Key = "Native_Point", // built-in
                Name = "Point",
                PluginName = "Native"
            };

            var result = await dispatcher.DispatchAsync(new[] { comp });

            Assert.Equal(1, result.HitCount);
            Assert.Equal(0, result.MissCount);
            Assert.Equal(0, ai.Calls);
        }

        [Fact]
        public async Task LocalMiss_InvokesAiAndPersists()
        {
            var dict = new TranslationDictionary(_file);
            dict.Load();
            var ai = new CountingAi();
            var dispatcher = new TranslationDispatcher(dict, ai);

            var comp = new ComponentInfo
            {
                Key = "Kangaroo2_SomeNewSolver",
                Name = "SomeNewSolver",
                PluginName = "Kangaroo2"
            };

            // Manually simulate the AI returning a real translation (we swap the
            // CountingAi for one that returns a value).
            var realAi = new TranslateReturningAi(new TranslationEntry { Name = "新求解器", Source = "ai" });
            var d2 = new TranslationDispatcher(dict, realAi);

            var result = await d2.DispatchAsync(new[] { comp });

            Assert.Equal(1, result.MissCount);
            Assert.Equal(1, result.TranslatedCount);
            Assert.Equal("新求解器", dict.GetTranslation("Kangaroo2_SomeNewSolver"));
        }

        [Fact]
        public async Task AiFailure_DoesNotThrow_DoesNotPersist()
        {
            var dict = new TranslationDictionary(_file);
            dict.Load();
            var ai = new TranslateReturningAi(null); // simulate API failure
            var dispatcher = new TranslationDispatcher(dict, ai);

            var comp = new ComponentInfo
            {
                Key = "Kangaroo2_OtherSolver",
                Name = "OtherSolver",
                PluginName = "Kangaroo2"
            };

            var result = await dispatcher.DispatchAsync(new[] { comp });
            Assert.Equal(1, result.FailedCount);
            Assert.Null(dict.GetTranslation("Kangaroo2_OtherSolver"));
        }

        private class TranslateReturningAi : IAiClient
        {
            private readonly TranslationEntry? _entry;
            public TranslateReturningAi(TranslationEntry? e) { _entry = e; }
            public Task<TranslationEntry?> TranslateAsync(ComponentInfo c, CancellationToken ct = default)
                => Task.FromResult(_entry);
        }
    }
}
