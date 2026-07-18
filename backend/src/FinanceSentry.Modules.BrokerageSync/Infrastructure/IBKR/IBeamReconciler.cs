using Microsoft.Extensions.Logging;

namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;

/// <summary>
/// No-op under the OAuth 1.0a model. IBKR requests are signed on demand from
/// stored keys, so there is no interactive per-user gateway session to respawn.
/// Retained as an inert implementation until the IBeam runtime is fully removed.
/// </summary>
public sealed class IBeamReconciler(ILogger<IBeamReconciler> logger) : IIBeamReconciler
{
    public Task ReconcileAllAsync(CancellationToken ct = default)
    {
        logger.LogDebug("IBeam reconcile skipped: OAuth model uses no per-user gateway containers.");
        return Task.CompletedTask;
    }
}
