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
    [Description("Search ingested market news (Yahoo Finance RSS per ticker + Fed press releases + registered market-wide and thesis sources). Filter by keyword, tickers, thesis, and cutoff date. Returns most-recent-first. Ticker/market-wide sources are ingested every 30 minutes; Fed press every 6 hours. Pass thesisId to see only articles tagged to a specific thesis (e.g. TrendForce DRAM coverage).")]
    public async Task<IReadOnlyList<NewsArticleDto>> ExecuteAsync(
        [Description("Optional free-text query — matched against title + summary (case-insensitive).")] string? query = null,
        [Description("Optional list of tickers to filter by (e.g. NVDA, AAPL). Matches articles tagged with any of them.")] IReadOnlyList<string>? tickers = null,
        [Description("Optional thesis GUID. Returns only articles tagged with that thesis (via a registered thesis source or keyword match).")] Guid? thesisId = null,
        [Description("Optional ISO-8601 cutoff. Only returns articles published at or after this instant.")] DateTimeOffset? since = null,
        [Description("Max results, default 25, max 100.")] int limit = 25,
        CancellationToken cancellationToken = default)
    {
        return await handler.Handle(new SearchMarketNewsQuery(query, tickers, thesisId, since, limit), cancellationToken);
    }
}
