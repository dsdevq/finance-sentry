namespace FinanceSentry.Modules.Radar.Infrastructure.Jobs;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Radar.Application.Services;
using FinanceSentry.Modules.Radar.Domain;
using Hangfire;
using Microsoft.Extensions.Logging;

/// <summary>
/// Weekly cron (Monday 08:00 UTC, feature 412) that computes the book-vs-SPY TWR scoreboard
/// for every active user and stores a verdict-first PerformanceBrief alert. The Companion
/// dispatch pipeline picks up the alert and delivers it to Telegram.
/// </summary>
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class BookPerformanceBriefJob(
    IBankingTotalsReader bankingTotals,
    IBookPerformanceService performance,
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

            var (headline, body) = BuildMessage(result);
            await alerts.GeneratePerformanceBriefAlertAsync(userId, headline, body, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Performance brief failed for user {UserId}", userId);
        }
    }

    private static (string Headline, string Body) BuildMessage(BookPerformanceResult result)
    {
        // Lead with the 1-week verdict for the headline.
        var weekly = result.Periods.FirstOrDefault(p => p.Period == BookPerformancePeriod.OneWeek);
        var verdict = weekly?.Verdict ?? result.Periods[0].Verdict ?? "N/A";
        var delta = weekly?.Delta;

        var headline = delta.HasValue
            ? $"Weekly brief: {CapitalizeFirst(verdict)} ({delta.Value:+0.##%;-0.##%;0%} vs SPY)"
            : $"Weekly brief: {CapitalizeFirst(verdict)}";

        var lines = result.Periods.Select(p =>
        {
            var label = p.Period switch
            {
                BookPerformancePeriod.OneWeek => "1W",
                BookPerformancePeriod.OneMonth => "1M",
                BookPerformancePeriod.ThreeMonths => "3M",
                BookPerformancePeriod.OneYear => "1Y",
                _ => p.Period.ToString(),
            };

            var book = p.BookTwr.HasValue ? $"{p.BookTwr.Value:+0.##%;-0.##%;0%}" : "N/A";
            var spy = p.SpyTwr.HasValue ? $"{p.SpyTwr.Value:+0.##%;-0.##%;0%}" : "N/A";
            var diff = p.Delta.HasValue ? $" (Δ {p.Delta.Value:+0.##%;-0.##%;0%})" : string.Empty;

            return $"{label}: Book {book} | SPY {spy}{diff}";
        });

        var body = string.Join("\n", lines);
        return (headline, body);
    }

    private static string CapitalizeFirst(string? s)
        => s is null or "" ? string.Empty : char.ToUpperInvariant(s[0]) + s[1..];
}
