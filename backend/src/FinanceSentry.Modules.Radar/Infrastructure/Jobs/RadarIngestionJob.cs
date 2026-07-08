namespace FinanceSentry.Modules.Radar.Infrastructure.Jobs;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Radar.Application.Commands;
using FinanceSentry.Modules.Radar.Domain.MarketStructure;
using Hangfire;
using Microsoft.Extensions.Logging;

/// <summary>Daily post-close ingestion of the universe's daily bars.</summary>
public sealed class RadarIngestionJob(
    ICommandHandler<IngestDailyBarsCommand, IngestRunSummary> handler,
    IBankingTotalsReader bankingTotals,
    IAlertGeneratorService alerts,
    ILogger<RadarIngestionJob> logger)
{
    private static readonly Guid IngestionFailureReferenceId = new("00000000-0000-0000-0000-0000000f8e55");

    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        IngestRunSummary summary;
        try
        {
            summary = await handler.Handle(new IngestDailyBarsCommand(), ct);
        }
        catch (Exception ex)
        {
            // FR-017: an ingestion run failing outright is an operational alert in every mode.
            await RaiseFailureAlertAsync($"Radar ingestion run failed: {ex.Message}", ct);
            throw;
        }

        logger.LogInformation(
            "Radar ingestion complete: {Ingested} tickers, {BarsAdded} bars added, {Errors} errors ({Failed}).",
            summary.TickersIngested, summary.BarsAdded, summary.Errors, string.Join(",", summary.FailedTickers));

        if (summary.Errors > 0 && summary.TickersIngested == 0)
        {
            await RaiseFailureAlertAsync(
                $"Radar ingestion produced no data: all {summary.Errors} tickers failed", ct);
        }
    }

    private async Task RaiseFailureAlertAsync(string reason, CancellationToken ct)
    {
        var userIds = await bankingTotals.GetActiveUserIdsAsync(ct);
        foreach (var userId in userIds)
        {
            await alerts.GenerateMarketStructureFreshnessAlertAsync(
                userId, IngestionFailureReferenceId, reason, ct);
        }
    }
}
