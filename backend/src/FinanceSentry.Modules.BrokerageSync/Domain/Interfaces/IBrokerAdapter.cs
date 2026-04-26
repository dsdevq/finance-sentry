namespace FinanceSentry.Modules.BrokerageSync.Domain.Interfaces;

/// <summary>
/// Abstraction over a brokerage adapter.
///
/// Authentication is intentionally absent: under the current single-tenant
/// IBKR-via-IBeam model the gateway sidecar owns the broker session lifecycle,
/// so the application code only needs to verify status and read data.
/// </summary>
public interface IBrokerAdapter
{
    string BrokerName { get; }
    Task EnsureSessionAsync(CancellationToken ct = default);
    Task<string> GetAccountIdAsync(CancellationToken ct = default);
    Task<IReadOnlyList<BrokerPosition>> GetPositionsAsync(string accountId, CancellationToken ct = default);
}

public sealed record BrokerPosition(
    string Symbol,
    string InstrumentType,
    decimal Quantity,
    decimal UsdValue);
