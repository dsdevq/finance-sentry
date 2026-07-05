using System.ComponentModel;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Application.Queries;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class ListWatchlistTool(
    IQueryHandler<GetWatchlistQuery, IReadOnlyList<WatchlistItemDto>> handler,
    IIdentityResolver identity)
{
    [McpServerTool(Name = "list_watchlist")]
    [Description("Returns the caller's watchlist (tickers tracked but not necessarily held). Defaults to the authenticated MCP identity when userId is omitted.")]
    public async Task<IReadOnlyList<WatchlistItemDto>> ExecuteAsync(
        [Description("Optional user GUID. Defaults to the authenticated MCP identity.")] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var effective = userId ?? identity.GetUserId();
        if (effective is null)
        {
            return [];
        }

        return await handler.Handle(new GetWatchlistQuery(effective.Value), cancellationToken);
    }
}
