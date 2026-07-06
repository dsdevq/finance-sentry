using System.ComponentModel;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Application.Queries;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class GetFundamentalsTool(
    IQueryHandler<GetFundamentalsQuery, IReadOnlyList<FundamentalFactDto>> handler)
{
    private const int DefaultMaxPerConcept = 5;

    [McpServerTool(Name = "get_fundamentals")]
    [Description("Reported financial fundamentals for a US-listed ticker from SEC EDGAR XBRL (live, no key): Revenue, GrossProfit, OperatingIncome, NetIncome, DilutedEPS, StockholdersEquity — the most recent periods, newest first, tagged with periodEnd / fiscalPeriod / form. Values are raw as reported (USD, or USD/shares for EPS); derive ratios like gross margin yourself. Use this to evaluate a thesis's invalidation triggers against real numbers after an earnings report. Returns empty for non-EDGAR tickers.")]
    public async Task<IReadOnlyList<FundamentalFactDto>> ExecuteAsync(
        [Description("Ticker symbol, e.g. AAPL, NVDA.")] string ticker,
        [Description("Max datapoints per concept, default 5, max 20.")] int maxPerConcept = DefaultMaxPerConcept,
        CancellationToken cancellationToken = default)
    {
        return await handler.Handle(new GetFundamentalsQuery(ticker, maxPerConcept), cancellationToken);
    }
}
