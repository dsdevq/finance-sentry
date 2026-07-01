namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;

/// <summary>
/// Walks every active IBKR credential and ensures its per-user IBeam container
/// is running. Used both at API startup and on a periodic Hangfire schedule so
/// containers survive Docker restarts, crashes, and API restarts.
/// </summary>
public interface IIBeamReconciler
{
    Task ReconcileAllAsync(CancellationToken ct = default);
}
