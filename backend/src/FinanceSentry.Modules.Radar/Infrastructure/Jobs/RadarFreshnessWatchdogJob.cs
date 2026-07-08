namespace FinanceSentry.Modules.Radar.Infrastructure.Jobs;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Radar.Application.Services;
using FinanceSentry.Modules.Radar.Domain;
using FinanceSentry.Modules.Radar.Domain.Repositories;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// FR-017: flags stale Radar data. Structure reads already carry <c>stale=true</c> over stale bars;
/// this job additionally raises an <c>AlertType.MarketStructure</c> freshness Alert. To honour the
/// log-only launch (FR-015), the Alert is only raised in <see cref="Domain.ScannerMode.Alerting"/>;
/// staleness is always logged.
/// </summary>
public sealed class RadarFreshnessWatchdogJob(
    IRadarUniverseRepository universe,
    IDailyBarRepository bars,
    IBankingTotalsReader bankingTotals,
    IAlertGeneratorService alerts,
    IOptions<RadarOptions> options,
    ILogger<RadarFreshnessWatchdogJob> logger)
{
    private static readonly Guid FreshnessReferenceId = new("00000000-0000-0000-0000-0000000f8e54");

    private readonly RadarOptions _options = options.Value;

    [AutomaticRetry(Attempts = 1)]
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var members = await universe.ListActiveAsync(ct);
        if (members.Count == 0)
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var latestDates = await bars.GetLatestDatesAsync(members.Select(m => m.Ticker).ToArray(), ct);

        var stale = members
            .Where(m => FreshnessEvaluator.IsStale(
                latestDates.TryGetValue(m.Ticker, out var d) ? d : null,
                today,
                _options.FreshnessMaxTradingDays))
            .Select(m => m.Ticker)
            .ToList();

        if (stale.Count == 0)
        {
            return;
        }

        logger.LogWarning("Radar freshness watchdog: {Count} stale tickers ({Tickers}).",
            stale.Count, string.Join(",", stale));

        if (_options.ScannerMode != ScannerMode.Alerting)
        {
            return;
        }

        var reason = $"{stale.Count} universe tickers have bars older than {_options.FreshnessMaxTradingDays} trading days";
        var userIds = await bankingTotals.GetActiveUserIdsAsync(ct);
        foreach (var userId in userIds)
        {
            await alerts.GenerateMarketStructureFreshnessAlertAsync(userId, FreshnessReferenceId, reason, ct);
        }
    }
}
