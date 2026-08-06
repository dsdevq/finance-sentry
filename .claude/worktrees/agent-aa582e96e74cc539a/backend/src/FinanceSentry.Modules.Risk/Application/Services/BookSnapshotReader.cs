namespace FinanceSentry.Modules.Risk.Application.Services;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Risk.Domain;
using Microsoft.Extensions.Logging;

/// <summary>
/// Aggregates crypto/brokerage/banking sources into a <see cref="BookSnapshot"/>, degrading
/// gracefully (per-source try/catch → staleness flag) when one sync is unavailable — mirrors
/// Research's <c>GetAllocationDriftQueryHandler</c> established pattern.
/// </summary>
public sealed class BookSnapshotReader(
    ICryptoHoldingsReader cryptoReader,
    IBrokerageHoldingsReader brokerageReader,
    IBankingTotalsReader bankingReader,
    ILogger<BookSnapshotReader> logger) : IBookSnapshotReader
{
    public async Task<BookSnapshot> ReadAsync(Guid userId, CancellationToken ct = default)
    {
        var positions = new List<BookPosition>();
        var staleSources = new List<string>();
        var cashUsd = 0m;

        try
        {
            foreach (var h in await cryptoReader.GetHoldingsAsync(userId, ct))
            {
                var qty = h.FreeQuantity + h.LockedQuantity;
                positions.Add(new BookPosition(h.Asset, RiskSleeve.Crypto, qty, h.UsdValue, 0m));
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Crypto holdings unavailable for risk check; marking stale.");
            staleSources.Add("crypto");
        }

        try
        {
            foreach (var h in await brokerageReader.GetHoldingsAsync(userId, ct))
            {
                positions.Add(new BookPosition(h.Symbol, RiskSleeve.Brokerage, h.Quantity, h.UsdValue, 0m));
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Brokerage holdings unavailable for risk check; marking stale.");
            staleSources.Add("brokerage");
        }

        try
        {
            cashUsd = await bankingReader.GetTotalUsdAsync(userId, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Banking totals unavailable for risk check; marking stale.");
            staleSources.Add("banking");
        }

        var total = positions.Sum(p => p.UsdValue) + cashUsd;

        var weighted = positions
            .Select(p => p with { WeightPct = total > 0 ? p.UsdValue / total : 0m })
            .ToList();

        return new BookSnapshot(total, cashUsd, weighted, staleSources.Count > 0, staleSources);
    }
}
