using System.ComponentModel;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Application.Queries;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class SearchMarketNewsTool(
    IQueryHandler<SearchMarketNewsQuery, IReadOnlyList<NewsArticleDto>> handler)
{
    [McpServerTool(Name = "search_market_news")]
    [Description("Search ingested market news (Yahoo Finance RSS per ticker + Fed press releases). Filter by keyword, tickers, and cutoff date. Returns most-recent-first. Sources are ingested every 30 minutes for tickers Denys holds or watches, and every 6 hours for Fed press.")]
    public async Task<IReadOnlyList<NewsArticleDto>> ExecuteAsync(
        [Description("Optional free-text query — matched against title + summary (case-insensitive).")] string? query = null,
        [Description("Optional list of tickers to filter by (e.g. NVDA, AAPL). Matches articles tagged with any of them.")] IReadOnlyList<string>? tickers = null,
        [Description("Optional ISO-8601 cutoff. Only returns articles published at or after this instant.")] DateTimeOffset? since = null,
        [Description("Max results, default 25, max 100.")] int limit = 25,
        CancellationToken cancellationToken = default)
    {
        return await handler.Handle(new SearchMarketNewsQuery(query, tickers, since, limit), cancellationToken);
    }
}
