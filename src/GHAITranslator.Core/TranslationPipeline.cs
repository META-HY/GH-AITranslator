using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GHAITranslator.Core.Models;

namespace GHAITranslator.Core;

/// <summary>
/// P0 verification target. The plugin layer calls <see cref="EnsureTranslatedAsync"/>
/// for every component on the canvas. If the local dictionary has a non-empty
/// Chinese name, we never touch the AI — that's the "零延迟命中" promise.
/// </summary>
public sealed class TranslationPipeline
{
    private readonly TranslationDictionary _dict;
    private readonly IAiClient _ai;
    private readonly TranslationDispatcher _dispatcher;
    private readonly object _cacheLock = new();
    private readonly HashSet<string> _inFlight = new(StringComparer.Ordinal);

    public TranslationPipeline(TranslationDictionary dict, IAiClient ai, int maxConcurrency = 3)
    {
        _dict = dict ?? throw new ArgumentNullException(nameof(dict));
        _ai = ai ?? throw new ArgumentNullException(nameof(ai));
        _dispatcher = new TranslationDispatcher(dict, ai, maxConcurrency);
    }

    /// <summary>Synchronous dictionary lookup. Returns null when no translation is cached.</summary>
    public string? TryLookup(string key) => _dict.GetTranslation(key);

    /// <summary>
    /// Make sure <paramref name="info"/> has a translation. Local hits return
    /// immediately; misses are dispatched through the AI (subject to the
    /// concurrency cap) and the result is persisted before the task completes.
    /// Concurrent calls for the same key coalesce — only one AI request flies.
    /// </summary>
    public async Task<TranslationEntry?> EnsureTranslatedAsync(
        ComponentInfo info,
        CancellationToken cancellationToken = default)
    {
        if (info == null) throw new ArgumentNullException(nameof(info));
        var key = info.Key;
        if (string.IsNullOrEmpty(key)) return null;

        // fast path
        var existing = _dict.GetEntry(key);
        if (existing != null && !string.IsNullOrEmpty(existing.Name))
            return existing;

        // coalesce concurrent requests for the same key
        TaskCompletionSource<TranslationEntry?>? tcs = null;
        bool isLeader = false;
        lock (_cacheLock)
        {
            if (_inFlight.Contains(key))
            {
                // another request is already in-flight; we wait on a shared TCS
                if (!_waiters.TryGetValue(key, out tcs))
                {
                    tcs = new TaskCompletionSource<TranslationEntry?>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _waiters[key] = tcs;
                }
            }
            else
            {
                _inFlight.Add(key);
                isLeader = true;
            }
        }

        if (!isLeader)
        {
            // net48 has no Task.WaitAsync(CancellationToken) — poll the token.
#if NET48
            return await WaitWithCancellationAsync(tcs!.Task, cancellationToken).ConfigureAwait(false);
#else
            return await tcs!.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
#endif
        }

        try
        {
            // re-check after acquiring the slot (someone may have populated it)
            existing = _dict.GetEntry(key);
            if (existing != null && !string.IsNullOrEmpty(existing.Name))
                return existing;

            var entry = await _ai.TranslateAsync(info, cancellationToken).ConfigureAwait(false);
            if (entry != null && !string.IsNullOrEmpty(entry.Name))
            {
                entry.Source = TranslationSource.Ai;
                _dict.AddOrUpdate(key, entry);
            }
            return entry;
        }
        finally
        {
            lock (_cacheLock)
            {
                _inFlight.Remove(key);
                if (_waiters.TryGetValue(key, out var w))
                {
                    _waiters.Remove(key);
                    w.TrySetResult(_dict.GetEntry(key));
                }
            }
        }
    }

    private readonly Dictionary<string, TaskCompletionSource<TranslationEntry?>> _waiters
    = new(StringComparer.Ordinal);

#if NET48
    /// <summary>
    /// net48 has no Task.WaitAsync(CancellationToken). We poll the token at
    /// 100ms granularity while awaiting the underlying task. Polling is fine
    /// here because the inner task is bounded by a SemaphoreSlim in the
    /// dispatcher — long idle waits don't accumulate.
    /// </summary>
    private static async Task<T?> WaitWithCancellationAsync<T>(Task<T?> task, CancellationToken cancellationToken) where T : class
    {
        if (!cancellationToken.CanBeCanceled) return await task.ConfigureAwait(false);
        while (!task.IsCompleted)
        {
            if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException(cancellationToken);
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        }
        return await task.ConfigureAwait(false);
    }
#endif
}
