namespace FinanceSentry.Modules.BrokerageSync.Application.Connect;

/// <summary>
/// Immutable projection of an in-flight IBKR connect session, returned by
/// GET /brokerage/ibkr/connect/{sessionId}. The session store returns a new
/// snapshot on every read so callers can never observe torn state.
/// </summary>
public sealed record IBKRConnectSessionSnapshot(
    Guid SessionId,
    IBKRConnectStatus Status,
    string? ErrorCode,
    string? ErrorMessage,
    ConnectIBKRResult? Result,
    DateTime CreatedAt,
    DateTime UpdatedAt);
