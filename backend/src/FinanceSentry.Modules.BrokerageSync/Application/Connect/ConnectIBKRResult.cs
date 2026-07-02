namespace FinanceSentry.Modules.BrokerageSync.Application.Connect;

/// <summary>
/// Result of a successful IBKR connect + initial holdings sync. Emitted into
/// the session snapshot when Status transitions to Completed.
/// </summary>
public sealed record ConnectIBKRResult(int HoldingsCount, DateTime ConnectedAt, string AccountId);
