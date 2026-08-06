using System.ComponentModel;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Application.Queries;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

[McpServerToolType]
public sealed class GetValuationSnapshotTool(
    IQueryHandler<GetValuationSnapshotQuery, ValuationSnapshotResult> handler)
{
    [McpServerTool(Name = "get_valuation_snapshot")]
    [Description("Valuation snapshot for one EQUITY ticker: current trailing P/E, forward P/E, EV/EBITDA and dividend yield, each with the ticker's own trailing-P/E 5-year average where reconstructable (EDGAR EPS × prices); plus consensus price target and implied upside vs the current price, and a named peer set (defaulted from sector/industry, overridable). Missing metrics come back as null with a reason flag — NEVER zero-filled or fabricated. Non-equity tickers (crypto) return notApplicable=true. History we cannot source freely (forward P/E, EV/EBITDA, dividend yield) is flagged historyUnavailable and grows its own window as snapshots accrue.")]
    public async Task<ValuationSnapshotResult> ExecuteAsync(
        [Description("Equity ticker (e.g. MCD). Crypto returns an explicit not-applicable result.")] string ticker,
        [Description("Optional peer tickers to override the default sector/industry peer set.")] string[]? peers = null,
        CancellationToken cancellationToken = default)
    {
        var peerList = peers is { Length: > 0 } ? peers : null;
        return await handler.Handle(new GetValuationSnapshotQuery(ticker, peerList), cancellationToken);
    }
}
