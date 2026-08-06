namespace FinanceSentry.Modules.BrokerageSync.Application.Connect;

/// <summary>
/// Persists a user's IBKR OAuth 1.0a artifacts (encrypting the secret material
/// at rest) so the sync pipeline can derive a live session token and sign IBKR
/// requests. Throws <see cref="IBKRConnectException"/> with a stable error code
/// on failure (e.g. a duplicate active connection).
/// </summary>
public interface IIBKRConnector
{
    Task<ConnectIBKRResult> ConnectAsync(Guid userId, ConnectIBKRArtifacts artifacts, CancellationToken ct);
}
