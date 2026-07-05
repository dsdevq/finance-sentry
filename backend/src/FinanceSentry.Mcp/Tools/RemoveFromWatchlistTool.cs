using System.ComponentModel;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.Research.Application.Commands;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class RemoveFromWatchlistTool(
    ICommandHandler<RemoveWatchlistItemCommand, bool> handler,
    IIdentityResolver identity)
{
    [McpServerTool(Name = "remove_from_watchlist")]
    [Description("Removes a watchlist entry by its item id. Returns true when a row was deleted.")]
    public async Task<bool> ExecuteAsync(
        [Description("Watchlist item id (returned by list_watchlist / add_to_watchlist).")] Guid itemId,
        [Description("Optional user GUID. Defaults to the authenticated MCP identity.")] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var effective = userId ?? identity.GetUserId();
        if (effective is null)
        {
            return false;
        }

        return await handler.Handle(new RemoveWatchlistItemCommand(effective.Value, itemId), cancellationToken);
    }
}
