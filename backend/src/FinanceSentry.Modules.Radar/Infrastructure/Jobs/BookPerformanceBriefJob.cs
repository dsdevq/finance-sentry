namespace FinanceSentry.Modules.Radar.Infrastructure.Jobs;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Radar.Application.Services;
using FinanceSentry.Modules.Radar.Domain;
using FinanceSentry.Modules.Radar.Domain.Ports;
using FinanceSentry.Modules.Radar.Domain.Repositories;
using Hangfire;
using Microsoft.Extensions.Logging;

/// <summary>
/// Weekly cron (Monday 08:00 UTC, feature 414) that composes a verdict-first brief of book vs SPY
/// for every active user and stores a PerformanceBrief alert. The Companion dispatch pipeline picks
/// up the alert and delivers it to Telegram via list_active_alerts.
/// </summary>
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class BookPerformanceBriefJob(
    IBankingTotalsReader bankingTotals,
    IBookPerformanceService performance,
    IRadarSignalRepository signals,
    ITrackRecordSource trackRecord,
    IAlertGeneratorService alerts,
    ILogger<BookPerformanceBriefJob> logger)
{
    private static readonly IReadOnlyList<BookPerformancePeriod> DefaultPeriods =
    [
        BookPerformancePeriod.OneWeek,
        BookPerformancePeriod.OneMonth,
        BookPerformancePeriod.ThreeMonths,
        BookPerformancePeriod.OneYear,
    ];

    private static readonly TimeSpan SignalLookback = TimeSpan.FromDays(30);

    [AutomaticRetry(Attempts = 1)]
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var userIds = await bankingTotals.GetActiveUserIdsAsync(ct);

        foreach (var userId in userIds)
        {
            await RunForUserAsync(userId, ct);
        }
    }

    private async Task RunForUserAsync(Guid userId, CancellationToken ct)
    {
        try
        {
            var result = await performance.GetAsync(userId, DefaultPeriods, ct);
            if (result.Periods.Count == 0)
            {
                return;
            }

            // Every Notable portfolio signal, not just drift: the suggested action also weighs the
            // cash floor and the position cap, and the composer partitions by signal type.
            var portfolioSignals = await signals.ListAsync(
                new SignalFilter(
                    Since: DateTimeOffset.UtcNow - SignalLookback,
                    Scanner: RadarScanners.Portfolio,
                    UserId: userId,
                    Severity: nameof(SignalSeverity.Notable)),
                ct);

            var delta = await trackRecord.GetDeltaAsync(userId, ct);

            var brief = PerformanceBriefComposer.Compose(result, portfolioSignals, delta);
            await alerts.GeneratePerformanceBriefAlertAsync(userId, brief.Headline, brief.Body, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Performance brief failed for user {UserId}", userId);
        }
    }
}
