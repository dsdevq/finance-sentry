using System.ComponentModel;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.BrokerageSync.Application.Queries;
using FinanceSentry.Modules.CryptoSync.Application.Queries;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class GetPortfolioSnapshotTool(
    IQueryHandler<GetBrokerageHoldingsQuery, BrokerageHoldingsResponse> brokerageHandler,
    IQueryHandler<GetCryptoHoldingsQuery, CryptoHoldingsResponse> cryptoHandler,
    IBankingAccountsReader bankingReader,
    IIdentityResolver identity,
    ILogger<GetPortfolioSnapshotTool> logger)
{
    private readonly IQueryHandler<GetBrokerageHoldingsQuery, BrokerageHoldingsResponse> _brokerageHandler = brokerageHandler;
    private readonly IQueryHandler<GetCryptoHoldingsQuery, CryptoHoldingsResponse> _cryptoHandler = cryptoHandler;
    private readonly IBankingAccountsReader _bankingReader = bankingReader;
    private readonly IIdentityResolver _identity = identity;
    private readonly ILogger<GetPortfolioSnapshotTool> _logger = logger;

    [McpServerTool(Name = "get_portfolio_snapshot")]
    [Description("Unified portfolio snapshot: IBKR brokerage positions + Binance crypto holdings, each with unrealized P&L (USD and %) when cost basis is known, PLUS cash held in banking accounts (USD). Returns per-position rows and book-level totals (invested value, cash, total value, total cost basis, total unrealized P&L). unrealizedPnlUsd/Pct are null when cost basis is unavailable. Defaults to the authenticated MCP identity when userId is omitted.")]
    public async Task<PortfolioSnapshot> ExecuteAsync(
        [Description("Optional user GUID. Defaults to the authenticated MCP identity.")] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var effective = userId ?? _identity.GetUserId();
        if (effective is null)
        {
            return PortfolioSnapshot.Empty;
        }

        var userIdVal = effective.Value;
        var positions = new List<PortfolioSnapshotEntry>();

        // IBKR brokerage positions
        try
        {
            var response = await _brokerageHandler.Handle(new GetBrokerageHoldingsQuery(userIdVal), cancellationToken);
            positions.AddRange(response.Positions.Select(p => BuildEntry(
                p.Symbol, p.InstrumentType, p.Quantity, p.CostBasisUsd, p.UsdValue, response.Provider)));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "BrokerageSync query unavailable for user {UserId}; contributing empty list.", userIdVal);
        }

        // Binance crypto holdings
        try
        {
            var response = await _cryptoHandler.Handle(new GetCryptoHoldingsQuery(userIdVal), cancellationToken);
            positions.AddRange(response.Holdings.Select(h => BuildEntry(
                h.Asset, "Crypto", h.FreeQuantity + h.LockedQuantity, h.CostBasisUsd, h.UsdValue, response.Provider)));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CryptoSync query unavailable for user {UserId}; contributing empty list.", userIdVal);
        }

        // Cash held across banking accounts (USD).
        var cashUsd = 0m;
        try
        {
            var accounts = await _bankingReader.GetAccountSummariesAsync(userIdVal, cancellationToken);
            cashUsd = accounts.Sum(a => a.BalanceUsd ?? 0m);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "BankSync provider unavailable for user {UserId}; cash contributes 0.", userIdVal);
        }

        var investedValueUsd = positions.Sum(p => p.CurrentValue);

        // Totals for unrealized P&L only cover positions whose cost basis is known.
        var withBasis = positions.Where(p => p.CostBasis is > 0m).ToList();
        decimal? totalCostBasisUsd = withBasis.Count > 0 ? withBasis.Sum(p => p.CostBasis!.Value) : null;
        decimal? totalPnlUsd = withBasis.Count > 0 ? withBasis.Sum(p => p.CurrentValue - p.CostBasis!.Value) : null;
        decimal? totalPnlPct = totalCostBasisUsd is > 0m
            ? Math.Round(totalPnlUsd!.Value / totalCostBasisUsd.Value * 100m, 2)
            : null;

        return new PortfolioSnapshot(
            Positions: positions.OrderByDescending(p => p.CurrentValue).ToList(),
            CashUsd: cashUsd,
            InvestedValueUsd: investedValueUsd,
            TotalValueUsd: investedValueUsd + cashUsd,
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
    decimal InvestedValueUsd,
    decimal TotalValueUsd,
    decimal? TotalCostBasisUsd,
    decimal? TotalUnrealizedPnlUsd,
    decimal? TotalUnrealizedPnlPct)
{
    public static PortfolioSnapshot Empty { get; } = new([], 0m, 0m, 0m, null, null, null);
}
