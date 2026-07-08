using System.ComponentModel;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.Radar.Application.Queries;
using FinanceSentry.Modules.Radar.Domain.MarketStructure;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class GetMarketBreadthTool(
    IQueryHandler<GetMarketBreadthQuery, BreadthResult> handler,
    IIdentityResolver identity)
{
    [McpServerTool(Name = "get_market_breadth")]
    [Description("Returns Radar universe breadth: the percentage of tickers trading above their 20/50/200-day moving averages, plus the evaluated count. Reads persisted bars only — never triggers ingestion.")]
    public async Task<BreadthResult> ExecuteAsync(
        [Description("Optional user GUID. Defaults to the authenticated MCP identity.")] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        _ = userId ?? identity.GetUserId();
        return await handler.Handle(new GetMarketBreadthQuery(), cancellationToken);
    }
}
