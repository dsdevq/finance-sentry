namespace FinanceSentry.Modules.BrokerageSync.Application.Connect;

/// <summary>
/// Runs a single IBKR connect request end-to-end: persist credentials, spawn
/// the per-user IBeam container, wait for CPG to authenticate, and sync
/// holdings. Returns the initial sync result on success; throws
/// <see cref="IBKRConnectException"/> with a stable error code on failure.
/// On any failure or cancellation, the credential row is deactivated and the
/// container is torn down before the exception propagates — the caller never
/// has to reason about half-applied state.
/// </summary>
public interface IIBKRConnector
{
    Task<ConnectIBKRResult> ConnectAsync(Guid userId, string username, string password, CancellationToken ct);
}
