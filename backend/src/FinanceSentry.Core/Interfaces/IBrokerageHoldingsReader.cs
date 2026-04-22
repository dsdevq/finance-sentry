namespace FinanceSentry.Core.Interfaces;

public interface IBrokerageHoldingsReader
{
    Task<IReadOnlyList<BrokerageHoldingSummary>> GetHoldingsAsync(Guid userId, CancellationToken ct = default);
}

public sealed record BrokerageHoldingSummary(
    string Symbol,
    string InstrumentType,
    decimal Quantity,
    decimal UsdValue,
    DateTime SyncedAt,
    string Provider);
