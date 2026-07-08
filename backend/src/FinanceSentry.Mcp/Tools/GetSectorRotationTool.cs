using System.ComponentModel;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.Radar.Application.Queries;
using FinanceSentry.Modules.Radar.Domain.MarketStructure;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class GetSectorRotationTool(
    IQueryHandler<GetSectorRotationQuery, IReadOnlyList<SectorRotationRow>> handler,
    IIdentityResolver identity)
{
    [McpServerTool(Name = "get_sector_rotation")]
    [Description("Ranks the 11 SPDR sector ETFs by relative strength per window and reports each sector's rank plus its rank delta vs 21 trading days prior (rotation). Reads persisted bars only — never triggers ingestion.")]
    public async Task<IReadOnlyList<SectorRotationRow>> ExecuteAsync(
        [Description("Optional user GUID. Defaults to the authenticated MCP identity.")] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        _ = userId ?? identity.GetUserId();
        return await handler.Handle(new GetSectorRotationQuery(), cancellationToken);
    }
}
