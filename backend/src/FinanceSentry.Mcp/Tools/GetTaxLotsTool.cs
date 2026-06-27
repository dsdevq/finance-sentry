using System.ComponentModel;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.BrokerageSync.Application.Queries;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class GetTaxLotsTool(
    IQueryHandler<GetTaxLotsQuery, TaxLotsResponse> taxLotsHandler,
    IIdentityResolver identity,
    ILogger<GetTaxLotsTool> logger) : IReadOnlyMcpTool
{
    private readonly IQueryHandler<GetTaxLotsQuery, TaxLotsResponse> _taxLotsHandler = taxLotsHandler;
    private readonly IIdentityResolver _identity = identity;
    private readonly ILogger<GetTaxLotsTool> _logger = logger;

    public string ToolName => "get_tax_lots";

    [McpServerTool(Name = "get_tax_lots")]
    [Description("Returns brokerage tax lots — one lot per current position, with average cost basis sourced from IBKR. AverageCost is null for positions where IBKR has not yet reported avgPrice/avgCost. Defaults to the MCP_TOKEN identity when userId is omitted.")]
    public async Task<IReadOnlyList<TaxLotEntry>> ExecuteAsync(
        [Description("Optional user GUID. Defaults to the identity baked into MCP_TOKEN.")] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var effective = userId ?? _identity.GetUserId();
        if (effective is null) return [];
        var userIdVal = effective.Value;

        TaxLotsResponse response;
        try
        {
            response = await _taxLotsHandler.Handle(new GetTaxLotsQuery(userIdVal), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tax lots query unavailable for user {UserId}; returning empty list.", userIdVal);
            return [];
        }

        return response.Items
            .Select(i => new TaxLotEntry(
                i.Symbol,
                i.InstrumentType,
                i.Quantity,
                i.CurrentValueUsd,
                i.AverageCostUsd,
                i.CostBasisUsd,
                i.UnrealizedPnlUsd,
                i.UnrealizedPnlPercent,
                i.AcquiredAt,
                i.IsLongTerm,
                response.Provider))
            .ToList();
    }
}

public sealed record TaxLotEntry(
    string Symbol,
    string InstrumentType,
    decimal Quantity,
    decimal CurrentValueUsd,
    decimal? AverageCostUsd,
    decimal? CostBasisUsd,
    decimal? UnrealizedPnlUsd,
    decimal? UnrealizedPnlPercent,
    DateTime? AcquiredAt,
    bool IsLongTerm,
    string Provider);
