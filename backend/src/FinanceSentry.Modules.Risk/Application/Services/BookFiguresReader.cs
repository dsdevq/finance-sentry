namespace FinanceSentry.Modules.Risk.Application.Services;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Core.Utils;
using Microsoft.Extensions.Logging;

/// <summary>
/// The single canonical source for book-level figures. Reads crypto, brokerage, and banking
/// sources; splits idle brokerage currency balances (CASH instrument type) out of invested
/// positions so cash is never double-counted. Degrades gracefully: each source is wrapped in a
/// try/catch so a single unavailable provider only marks the snapshot stale rather than aborting.
/// </summary>
public sealed class BookFiguresReader(
    ICryptoHoldingsReader cryptoReader,
    IBrokerageHoldingsReader brokerageReader,
    IBankingTotalsReader bankingReader,
    ILogger<BookFiguresReader> logger) : IBookFiguresReader
{
    public async Task<BookFigures> ReadAsync(Guid userId, CancellationToken ct = default)
    {
        var positions = new List<BookFiguresPosition>();
        var staleSources = new List<string>();
        var brokerageCashUsd = 0m;
        var bankingCashUsd = 0m;

        try
        {
            foreach (var h in await cryptoReader.GetHoldingsAsync(userId, ct))
            {
                var qty = h.FreeQuantity + h.LockedQuantity;
                positions.Add(new BookFiguresPosition(
                    h.Asset,
                    AssetClassNormalizer.Crypto,
                    h.Provider,
                    qty,
                    h.UsdValue,
                    h.CostBasisUsd,
                    WeightPct: 0m));
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Crypto holdings unavailable for book-figures read; marking stale.");
            staleSources.Add("crypto");
        }

        try
        {
            foreach (var h in await brokerageReader.GetHoldingsAsync(userId, ct))
            {
                // Idle currency balances (CASH instrument type) are cash-in-brokerage, not invested
                // positions — bucketing them under cash keeps investedValueUsd and cashUsd consistent
                // across all three aggregation surfaces (portfolio snapshot, allocation drift, risk).
                if (AssetClassNormalizer.Normalize(h.InstrumentType) == AssetClassNormalizer.Cash)
                {
                    brokerageCashUsd += h.UsdValue;
                }
                else
                {
                    positions.Add(new BookFiguresPosition(
                        h.Symbol,
                        AssetClassNormalizer.Normalize(h.InstrumentType),
                        h.Provider,
                        h.Quantity,
                        h.UsdValue,
                        h.CostBasisUsd,
                        WeightPct: 0m));
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Brokerage holdings unavailable for book-figures read; marking stale.");
            staleSources.Add("brokerage");
        }

        try
        {
            bankingCashUsd = await bankingReader.GetTotalUsdAsync(userId, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Banking totals unavailable for book-figures read; marking stale.");
            staleSources.Add("banking");
        }

        var cashUsd = bankingCashUsd + brokerageCashUsd;
        var investedValueUsd = positions.Sum(p => p.UsdValue);
        var totalUsd = investedValueUsd + cashUsd;

        var weighted = positions
            .Select(p => p with { WeightPct = totalUsd > 0 ? p.UsdValue / totalUsd : 0m })
            .ToList();

        return new BookFigures(
            TotalUsd: totalUsd,
            CashUsd: cashUsd,
            BankingCashUsd: bankingCashUsd,
            BrokerageCashUsd: brokerageCashUsd,
            InvestedValueUsd: investedValueUsd,
            Positions: weighted,
            IsStale: staleSources.Count > 0,
            StaleSources: staleSources);
    }
}
