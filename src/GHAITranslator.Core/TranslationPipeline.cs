using System;
using System.Threading;
using System.Threading.Tasks;
using GHAITranslator.Core.Models;

namespace GHAITranslator.Core;

/// <summary>
/// High-level translation orchestration. Decides whether to hit the AI or
/// return an existing dictionary entry, and persists AI results back to the
/// user overlay file.
/// </summary>
public sealed class TranslationPipeline
{
    private readonly TranslationDictionary _dict;
    private readonly IAiClient _ai;
    private readonly Func<string, bool>? _persist;

    public TranslationPipeline(TranslationDictionary dict, IAiClient ai, Func<string, bool>? persist = null)
    {
        _dict = dict;
        _ai = ai;
        _persist = persist;
    }

    /// <summary>
    /// Resolve a component's translation. Order:
    ///   1. Dictionary (BuiltinSeed + user overlay).
    ///   2. AI translate + AddOrUpdate + persist.
    ///   3. Fall back to the original English Name if AI fails.
    /// </summary>
    public async Task<TranslationEntry?> ResolveAsync(ComponentInfo info, CancellationToken ct = default)
    {
        if (info == null) return null;

        var existing = _dict.Get(info.LookupKey);
        if (existing != null) return existing;

        var translated = await _ai.TranslateAsync(info, ct).ConfigureAwait(false);
        if (translated == null || !translated.IsComplete) return null;

        _dict.AddOrUpdate(translated);
        _persist?.Invoke(info.LookupKey);
        return translated;
    }
}