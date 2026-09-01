namespace FinanceSentry.Modules.Radar.Infrastructure.Jobs;

using System.Text.Json;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Radar.Application.Services;
using FinanceSentry.Modules.Radar.Domain;
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

            var driftSignals = await signals.ListAsync(
                new SignalFilter(
                    Since: DateTimeOffset.UtcNow - SignalLookback,
                    Scanner: RadarScanners.Portfolio,
                    SignalType: RadarSignalTypes.AllocationDrift,
                    UserId: userId,
                    Severity: "Notable"),
                ct);

            var (headline, body) = BuildMessage(result, driftSignals);
            await alerts.GeneratePerformanceBriefAlertAsync(userId, headline, body, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Performance brief failed for user {UserId}", userId);
        }
    }

    public static (string Headline, string Body) BuildMessage(
        BookPerformanceResult result,
        IReadOnlyList<RadarSignal> driftSignals)
    {
        var weekly = result.Periods.FirstOrDefault(p => p.Period == BookPerformancePeriod.OneWeek);
        var verdict = weekly?.Verdict ?? result.Periods[0].Verdict ?? "N/A";
        var delta = weekly?.Delta;

        var headline = delta.HasValue
            ? $"Weekly brief: {CapitalizeFirst(verdict)} ({delta.Value:+0.##%;-0.##%;0%} vs SPY)"
            : $"Weekly brief: {CapitalizeFirst(verdict)}";

        var lines = new List<string>();
        foreach (var p in result.Periods)
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

            lines.Add($"{label}: Book {book} | SPY {spy}{diff}");
        }

        // Cap: ≤12 lines total; reserve space after the scoreboard for a blank separator + trend lines.
        const int MaxTotal = 12;
        const int MaxTrendLines = 4;
        var available = MaxTotal - lines.Count;
        if (available > 1 && driftSignals.Count > 0)
        {
            // One slot for blank separator; remaining for trend lines.
            var trendLines = BuildDriftTrendLines(driftSignals, Math.Min(MaxTrendLines, available - 1));
            if (trendLines.Count > 0)
            {
                lines.Add(string.Empty);
                lines.AddRange(trendLines);
            }
        }

        return (headline, string.Join("\n", lines));
    }

    private static IReadOnlyList<string> BuildDriftTrendLines(
        IReadOnlyList<RadarSignal> signals, int maxLines)
    {
        // One line per asset class (Subject); take the most recent per class, sorted by |driftPct| desc.
        var bySubject = signals
            .GroupBy(s => s.Subject)
            .Select(g => g.OrderByDescending(s => s.Timestamp).First())
            .OrderByDescending(s => Math.Abs(ExtractDriftPct(s)))
            .Take(maxLines)
            .ToList();

        var result = new List<string>(bySubject.Count);
        foreach (var s in bySubject)
        {
            var status = ExtractString(s, "status");
            var drift = ExtractDriftPct(s);
            var sign = drift >= 0 ? "+" : string.Empty;
            result.Add($"Drift: {s.Subject} {status} ({sign}{drift:P1} vs target)");
        }

        return result;
    }

    private static decimal ExtractDriftPct(RadarSignal signal)
    {
        if (!signal.Payload.TryGetValue("driftPct", out var raw))
            return 0m;

        return raw switch
        {
            JsonElement el when el.ValueKind == JsonValueKind.Number => (decimal)el.GetDouble(),
            double d => (decimal)d,
            decimal d => d,
            int i => i,
            long l => l,
            _ => 0m,
        };
    }

    private static string ExtractString(RadarSignal signal, string key)
    {
        if (!signal.Payload.TryGetValue(key, out var raw))
            return string.Empty;

        return raw switch
        {
            JsonElement el => el.GetString() ?? string.Empty,
            string s => s,
            _ => raw.ToString() ?? string.Empty,
        };
    }

    private static string CapitalizeFirst(string? s)
        => s is null or "" ? string.Empty : char.ToUpperInvariant(s[0]) + s[1..];
}
