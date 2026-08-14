using System.ComponentModel;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Mcp.Abstractions;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class GetPortfolioSnapshotTool(
    IBookFiguresReader bookFiguresReader,
    IIdentityResolver identity)
{
    [McpServerTool(Name = "get_portfolio_snapshot")]
    [Description("Unified portfolio snapshot: IBKR brokerage positions + Binance crypto holdings, each with unrealized P&L (USD and %) when cost basis is known, PLUS total cash (USD). cashUsd counts banking balances AND idle brokerage cash (uninvested currency balances) — the same definition the allocation-drift tool uses; bankingCashUsd/brokerageCashUsd give the split. Idle brokerage cash is NOT listed under positions or investedValueUsd. Returns per-position rows and book-level totals (invested value, cash, total value, total cost basis, total unrealized P&L). unrealizedPnlUsd/Pct are null when cost basis is unavailable. Defaults to the authenticated MCP identity when userId is omitted.")]
    public async Task<PortfolioSnapshot> ExecuteAsync(
        [Description("Optional user GUID. Defaults to the authenticated MCP identity.")] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var effective = userId ?? identity.GetUserId();
        if (effective is null)
        {
            return PortfolioSnapshot.Empty;
        }

        var figures = await bookFiguresReader.ReadAsync(effective.Value, cancellationToken);

        var entries = figures.Positions
            .OrderByDescending(p => p.UsdValue)
            .Select(p => BuildEntry(p.Symbol, p.AssetClass, p.Quantity, p.CostBasisUsd, p.UsdValue, p.Provider))
            .ToList();

        // P&L totals only cover positions whose cost basis is known.
        var withBasis = entries.Where(e => e.CostBasis is > 0m).ToList();
        decimal? totalCostBasisUsd = withBasis.Count > 0 ? withBasis.Sum(e => e.CostBasis!.Value) : null;
        decimal? totalPnlUsd = withBasis.Count > 0 ? withBasis.Sum(e => e.CurrentValue - e.CostBasis!.Value) : null;
        decimal? totalPnlPct = totalCostBasisUsd is > 0m
            ? Math.Round(totalPnlUsd!.Value / totalCostBasisUsd.Value * 100m, 2)
            : null;

        return new PortfolioSnapshot(
            Positions: entries,
            CashUsd: figures.CashUsd,
            BankingCashUsd: figures.BankingCashUsd,
            BrokerageCashUsd: figures.BrokerageCashUsd,
            InvestedValueUsd: figures.InvestedValueUsd,
            TotalValueUsd: figures.TotalUsd,
            TotalCostBasisUsd: totalCostBasisUsd,
            TotalUnrealizedPnlUsd: totalPnlUsd,
            TotalUnrealizedPnlPct: totalPnlPct);
    }

    private static PortfolioSnapshotEntry BuildEntry(
        string symbol, string assetClass, decimal quantity, decimal? costBasis, decimal currentValue, string provider)
    {
        decimal? pnlUsd = costBasis is > 0m ? currentValue - costBasis.Value : null;
        decimal? pnlPct = costBasis is > 0m ? Math.Round((currentValue - costBasis.Value) / costBasis.Value * 100m, 2) : null;

        return new PortfolioSnapshotEntry(
            Symbol: symbol,
            AssetClass: assetClass,
            Quantity: quantity,
            CostBasis: costBasis,
            CurrentValue: currentValue,
            UnrealizedPnlUsd: pnlUsd,
            UnrealizedPnlPct: pnlPct,
            Provider: provider);
    }
}

public sealed record PortfolioSnapshotEntry(
    string Symbol,
    string AssetClass,
    decimal Quantity,
    decimal? CostBasis,
    decimal CurrentValue,
    decimal? UnrealizedPnlUsd,
    decimal? UnrealizedPnlPct,
    string Provider);

public sealed record PortfolioSnapshot(
    IReadOnlyList<PortfolioSnapshotEntry> Positions,
    decimal CashUsd,
    decimal BankingCashUsd,
    decimal BrokerageCashUsd,
    decimal InvestedValueUsd,
    decimal TotalValueUsd,
    decimal? TotalCostBasisUsd,
    decimal? TotalUnrealizedPnlUsd,
    decimal? TotalUnrealizedPnlPct)
{
    public static PortfolioSnapshot Empty { get; } = new([], 0m, 0m, 0m, 0m, 0m, null, null, null);
}
