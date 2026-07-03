using System.ComponentModel;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Application.Commands;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class AddToWatchlistTool(
    ICommandHandler<AddWatchlistItemCommand, WatchlistItemDto> handler,
    IIdentityResolver identity) : IReadOnlyMcpTool
{
    public string ToolName => "add_to_watchlist";

    public bool IsReadOnly => false;

    [McpServerTool(Name = "add_to_watchlist")]
    [Description("Adds a ticker to the caller's watchlist. Idempotency: fails if the ticker is already present. Defaults to MCP_TOKEN identity.")]
    public async Task<WatchlistItemDto?> ExecuteAsync(
        [Description("Ticker symbol, e.g. AAPL, NVDA, BTC-USD.")] string ticker,
        [Description("Optional exchange code, e.g. NASDAQ, NYSE.")] string? exchange = null,
        [Description("Optional free-form note about why the ticker is being tracked.")] string? note = null,
        [Description("Optional user GUID. Defaults to MCP_TOKEN identity.")] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var effective = userId ?? identity.GetUserId();
        if (effective is null)
        {
            return null;
        }

        return await handler.Handle(
            new AddWatchlistItemCommand(effective.Value, ticker, exchange, note),
            cancellationToken);
    }
}
