using System.Threading;
using System.Threading.Tasks;
using GHAITranslator.Core.Models;

namespace GHAITranslator.Core;

/// <summary>
/// LLM transport. Defined in Core (not in the plugin) so the dispatcher can be
/// unit-tested with a fake implementation; the real HTTP client lives in the
/// Plugin layer.
/// </summary>
public interface IAiClient
{
    /// <summary>
    /// Translate one component. Implementations MUST NOT throw on a 4xx/5xx —
    /// return <c>null</c> and let the caller decide whether to log/continue.
    /// </summary>
    Task<TranslationEntry?> TranslateAsync(
        ComponentInfo component,
        CancellationToken cancellationToken = default);
}
