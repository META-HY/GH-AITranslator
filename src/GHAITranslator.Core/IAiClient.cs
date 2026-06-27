using System.Threading;
using System.Threading.Tasks;
using GHAITranslator.Core.Models;

namespace GHAITranslator.Core;

/// <summary>
/// Abstraction over an AI translation provider. Implementations must be
/// thread-safe (multiple components may translate concurrently).
/// </summary>
public interface IAiClient
{
    /// <summary>
    /// Translate a single component's metadata into a
    /// <see cref="TranslationEntry"/>. Implementations should retry on
    /// transient errors and never throw for non-AI reasons (return null on
    /// unrecoverable failure).
    /// </summary>
    Task<TranslationEntry?> TranslateAsync(ComponentInfo info, CancellationToken ct = default);
}