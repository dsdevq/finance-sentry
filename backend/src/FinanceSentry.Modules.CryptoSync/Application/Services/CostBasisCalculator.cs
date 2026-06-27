using FinanceSentry.Modules.CryptoSync.Domain.Interfaces;

namespace FinanceSentry.Modules.CryptoSync.Application.Services;

public sealed record CostBasisResult(
    decimal CostBasisUsd,
    decimal AverageBuyPriceUsd,
    decimal RealizedPnlUsd,
    DateTime? LastTradeAt,
    long LastTradeId,
    int TradeCount);

/// <summary>
/// Weighted-average cost-basis tracker for a single asset's trade history.
/// Buys add to running cost; sells consume cost at the current weighted-average buy price
/// and accumulate realized P&amp;L.
/// </summary>
public sealed class CostBasisCalculator
{
    public CostBasisResult Compute(IEnumerable<CryptoTrade> trades, CostBasisResult? seed = null)
    {
        ArgumentNullException.ThrowIfNull(trades);

        decimal runningQty = 0m;
        decimal runningCostUsd = 0m;
        decimal realizedPnl = seed?.RealizedPnlUsd ?? 0m;
        DateTime? lastAt = seed?.LastTradeAt;
        long lastId = seed?.LastTradeId ?? 0L;
        int tradeCount = seed?.TradeCount ?? 0;

        if (seed is not null && seed.AverageBuyPriceUsd > 0m)
        {
            runningCostUsd = seed.CostBasisUsd;
            runningQty = seed.AverageBuyPriceUsd > 0m
                ? seed.CostBasisUsd / seed.AverageBuyPriceUsd
                : 0m;
        }

        foreach (var trade in trades.OrderBy(t => t.Timestamp).ThenBy(t => t.TradeId))
        {
            tradeCount++;
            lastAt = trade.Timestamp;
            lastId = trade.TradeId;

            if (trade.IsBuyer)
            {
                runningCostUsd += trade.QuoteQuantityUsd;
                runningQty += trade.Quantity;
            }
            else
            {
                if (runningQty <= 0m)
                {
                    continue;
                }

                var avg = runningCostUsd / runningQty;
                var sellQty = Math.Min(trade.Quantity, runningQty);
                realizedPnl += (trade.PriceUsd - avg) * sellQty;
                runningCostUsd -= avg * sellQty;
                runningQty -= sellQty;
            }
        }

        var avgBuyPrice = runningQty > 0m ? runningCostUsd / runningQty : 0m;
        return new CostBasisResult(
            CostBasisUsd: Math.Round(runningCostUsd, 4),
            AverageBuyPriceUsd: Math.Round(avgBuyPrice, 8),
            RealizedPnlUsd: Math.Round(realizedPnl, 4),
            LastTradeAt: lastAt,
            LastTradeId: lastId,
            TradeCount: tradeCount);
    }
}
