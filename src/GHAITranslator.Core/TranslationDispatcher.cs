using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GHAITranslator.Core.Models;

namespace GHAITranslator.Core;

/// <summary>
/// Orchestrates the dictionary-first / AI-fallback translation flow.
///
/// Per design doc §5.4:
///   1) iterate the components,
///   2) for each one, hit the local dictionary first,
///   3) on miss, fire one AI request and persist the result,
///   4) report progress + a final list of keys that still failed.
///
/// Concurrency: a bounded <see cref="SemaphoreSlim"/> caps parallel AI calls
/// (the design doc specifies max 3 concurrent). The dict layer is thread-safe,
/// so worker tasks can call <see cref="TranslationDictionary.AddOrUpdate"/>
/// concurrently.
/// </summary>
public sealed class TranslationDispatcher
{
    private readonly TranslationDictionary _dict;
    private readonly IAiClient _ai;
    private readonly int _maxConcurrency;

    /// <summary>Raised when one component's translation is produced (builtin or AI).</summary>
    public event Action<string, TranslationEntry>? Translated;

    /// <summary>Raised when an AI call fails for a single component. The dispatcher keeps going.</summary>
    public event Action<string, Exception>? TranslationFailed;

    public TranslationDispatcher(
        TranslationDictionary dictionary,
        IAiClient aiClient,
        int maxConcurrency = 3)
    {
        _dict = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        _ai = aiClient ?? throw new ArgumentNullException(nameof(aiClient));
        if (maxConcurrency < 1) maxConcurrency = 1;
        _maxConcurrency = maxConcurrency;
    }

    /// <summary>
    /// Translate every component in <paramref name="components"/>. Already-translated
    /// keys (Name non-empty) are skipped without invoking the AI.
    /// </summary>
    public async Task<DispatchResult> DispatchAsync(
        IReadOnlyList<ComponentInfo> components,
        CancellationToken cancellationToken = default)
    {
        if (components == null) throw new ArgumentNullException(nameof(components));

        var result = new DispatchResult();
        var gate = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);
        var tasks = new List<Task>(components.Count);

        foreach (var info in components)
        {
            if (info == null || string.IsNullOrEmpty(info.Key)) continue;
            tasks.Add(ProcessOneAsync(info, gate, result, cancellationToken));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
        return result;
    }

    private async Task ProcessOneAsync(
        ComponentInfo info,
        SemaphoreSlim gate,
        DispatchResult result,
        CancellationToken cancellationToken)
    {
        // 1) dictionary fast-path
        var existing = _dict.GetEntry(info.Key);
        if (existing != null && !string.IsNullOrEmpty(existing.Name))
        {
            result.HitCount++;
            Translated?.Invoke(info.Key, existing);
            return;
        }

        // 2) AI fallback
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            result.MissCount++;
            var entry = await _ai.TranslateAsync(info, cancellationToken).ConfigureAwait(false);
            if (entry == null || string.IsNullOrEmpty(entry.Name))
            {
                result.FailedCount++;
                TranslationFailed?.Invoke(info.Key, new InvalidOperationException("AI returned empty translation"));
                return;
            }

            entry.Source = TranslationSource.Ai;
            _dict.AddOrUpdate(info.Key, entry);
            result.TranslatedCount++;
            Translated?.Invoke(info.Key, entry);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            result.FailedCount++;
            TranslationFailed?.Invoke(info.Key, ex);
        }
        finally
        {
            gate.Release();
        }
    }
}

/// <summary>Summary of a dispatcher run.</summary>
public sealed class DispatchResult
{
    public int HitCount { get; internal set; }
    public int MissCount { get; internal set; }
    public int TranslatedCount { get; internal set; }
    public int FailedCount { get; internal set; }
}
