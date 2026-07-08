namespace FinanceSentry.Modules.Radar.Infrastructure.Jobs;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Radar.Application.Commands;
using FinanceSentry.Modules.Radar.Domain.MarketStructure;
using Hangfire;
using Microsoft.Extensions.Logging;

/// <summary>Daily post-close ingestion of the universe's daily bars.</summary>
public sealed class RadarIngestionJob(
    ICommandHandler<IngestDailyBarsCommand, IngestRunSummary> handler,
    ILogger<RadarIngestionJob> logger)
{
    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var summary = await handler.Handle(new IngestDailyBarsCommand(), ct);
        logger.LogInformation(
            "Radar ingestion complete: {Ingested} tickers, {BarsAdded} bars added, {Errors} errors ({Failed}).",
            summary.TickersIngested, summary.BarsAdded, summary.Errors, string.Join(",", summary.FailedTickers));
    }
}
