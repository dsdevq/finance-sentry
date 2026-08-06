namespace FinanceSentry.Core.Interfaces;

public interface ICryptoHoldingsReader
{
    Task<IReadOnlyList<CryptoHoldingSummary>> GetHoldingsAsync(Guid userId, CancellationToken ct = default);
}

public sealed record CryptoHoldingSummary(
    string Asset,
    decimal FreeQuantity,
    decimal LockedQuantity,
    decimal UsdValue,
    DateTime SyncedAt,
    string Provider);
