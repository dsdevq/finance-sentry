using System.ComponentModel;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Application.Queries;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class GetNewsForTickerTool(
    IQueryHandler<GetNewsForTickerQuery, IReadOnlyList<NewsArticleDto>> handler)
{
    [McpServerTool(Name = "get_news_for_ticker")]
    [Description("Fetch recent news articles tagged with a specific ticker. Use this when answering \"why is X moving?\" — pull the last 24-48h of headlines for that ticker.")]
    public async Task<IReadOnlyList<NewsArticleDto>> ExecuteAsync(
        [Description("Ticker symbol (e.g. NVDA, BTC-USD).")] string ticker,
        [Description("Optional ISO-8601 cutoff. Only returns articles published at or after this instant.")] DateTimeOffset? since = null,
        [Description("Max results, default 25, max 100.")] int limit = 25,
        CancellationToken cancellationToken = default)
    {
        return await handler.Handle(new GetNewsForTickerQuery(ticker, since, limit), cancellationToken);
    }
}
