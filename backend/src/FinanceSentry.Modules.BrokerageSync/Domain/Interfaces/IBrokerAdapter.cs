namespace FinanceSentry.Modules.BrokerageSync.Domain.Interfaces;

/// <summary>
/// Abstraction over a brokerage adapter.
///
/// Each method takes the id of the calling user's <c>IBKRCredential</c> so the
/// adapter can resolve the per-user IBeam gateway URL. Under the per-user
/// container model (stage 2) each user gets their own IBeam and the API talks
/// to it by DNS name on the shared Docker network.
/// </summary>
public interface IBrokerAdapter
{
    string BrokerName { get; }
    Task EnsureSessionAsync(Guid credentialId, CancellationToken ct = default);
    Task<string> GetAccountIdAsync(Guid credentialId, CancellationToken ct = default);
    Task<IReadOnlyList<BrokerPosition>> GetPositionsAsync(Guid credentialId, string accountId, CancellationToken ct = default);
}

public sealed record BrokerPosition(
    string Symbol,
    string InstrumentType,
    decimal Quantity,
    decimal UsdValue,
    decimal? AverageCostUsd = null);
