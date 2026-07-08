using System.ComponentModel;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.Radar.Application.Queries;
using FinanceSentry.Modules.Radar.Domain.MarketStructure;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class GetRadarSummaryTool(
    IQueryHandler<GetRadarSummaryQuery, RadarSummary> handler,
    IIdentityResolver identity)
{
    [McpServerTool(Name = "get_radar_summary")]
    [Description("One-call Radar snapshot for narration: today's sector leaders/laggards with rank deltas, universe breadth, and today's notable+ signals, plus a stale flag when computed over stale bars. Reads persisted bars only — never triggers ingestion.")]
    public async Task<RadarSummary> ExecuteAsync(
        [Description("Optional user GUID. Defaults to the authenticated MCP identity.")] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        _ = userId ?? identity.GetUserId();
        return await handler.Handle(new GetRadarSummaryQuery(), cancellationToken);
    }
}
