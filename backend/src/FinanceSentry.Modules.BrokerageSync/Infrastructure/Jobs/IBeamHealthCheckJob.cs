using FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.Jobs;

/// <summary>
/// Recurring Hangfire job that runs the IBeam reconciler so any per-user
/// gateway container which has died, been removed, or is otherwise not
/// running gets respawned.
/// </summary>
public sealed class IBeamHealthCheckJob(
    IIBeamReconciler reconciler,
    ILogger<IBeamHealthCheckJob> logger)
{
    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        try
        {
            await reconciler.ReconcileAllAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "IBeam health-check reconciliation failed.");
            throw;
        }
    }
}
