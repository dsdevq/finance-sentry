using System.ComponentModel;
using FinanceSentry.Core.Cqrs;
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
    ILogger<GetPortfolioSnapshotTool> logger) : IReadOnlyMcpTool
{
    private readonly IQueryHandler<GetBrokerageHoldingsQuery, BrokerageHoldingsResponse> _brokerageHandler = brokerageHandler;
    private readonly IQueryHandler<GetCryptoHoldingsQuery, CryptoHoldingsResponse> _cryptoHandler = cryptoHandler;
    private readonly ILogger<GetPortfolioSnapshotTool> _logger = logger;

    public string ToolName => "get_portfolio_snapshot";

    [McpServerTool(Name = "get_portfolio_snapshot")]
    [Description("Returns a unified portfolio snapshot combining IBKR brokerage positions and Binance crypto holdings for a given user. costBasis is null when not available.")]
    public async Task<IReadOnlyList<PortfolioSnapshotEntry>> ExecuteAsync(
        [Description("The user's unique identifier.")] Guid userId,
        CancellationToken cancellationToken = default)
    {
        var results = new List<PortfolioSnapshotEntry>();

        // IBKR brokerage positions
        try
        {
            var response = await _brokerageHandler.Handle(
                new GetBrokerageHoldingsQuery(userId),
                cancellationToken);

            results.AddRange(response.Positions.Select(p => new PortfolioSnapshotEntry(
                Symbol: p.Symbol,
                AssetClass: p.InstrumentType,
                Quantity: p.Quantity,
                CostBasis: null,
                CurrentValue: p.UsdValue,
                Provider: response.Provider)));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "BrokerageSync query unavailable for user {UserId}; contributing empty list.", userId);
        }

        // Binance crypto holdings
        try
        {
            var response = await _cryptoHandler.Handle(
                new GetCryptoHoldingsQuery(userId),
                cancellationToken);

            results.AddRange(response.Holdings.Select(h => new PortfolioSnapshotEntry(
                Symbol: h.Asset,
                AssetClass: "Crypto",
                Quantity: h.FreeQuantity + h.LockedQuantity,
                CostBasis: null,
                CurrentValue: h.UsdValue,
                Provider: response.Provider)));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CryptoSync query unavailable for user {UserId}; contributing empty list.", userId);
        }

        return results;
    }
}

public sealed record PortfolioSnapshotEntry(
    string Symbol,
    string AssetClass,
    decimal Quantity,
    decimal? CostBasis,
    decimal CurrentValue,
    string Provider);
