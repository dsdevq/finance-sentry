using System.ComponentModel;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Application.Queries;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class GetQuotesTool(
    IQueryHandler<GetQuotesQuery, IReadOnlyList<QuoteDto>> handler)
{
    [McpServerTool(Name = "get_quotes")]
    [Description("Fetches current price + previous close + intraday %change for one or more tickers. Uses a 5-minute Postgres cache in front of Yahoo Finance to stay rate-limit-safe. Ticker examples: AAPL, NVDA, ^GSPC, BTC-USD, EURUSD=X.")]
    public async Task<IReadOnlyList<QuoteDto>> ExecuteAsync(
        [Description("Ticker symbols to quote.")] IReadOnlyList<string> tickers,
        CancellationToken cancellationToken = default)
    {
        return await handler.Handle(new GetQuotesQuery(tickers ?? []), cancellationToken);
    }
}
