namespace FinanceSentry.Modules.Radar.Application.Services;

using System.Text.Json;
using FinanceSentry.Modules.Radar.Domain;
using FinanceSentry.Modules.Radar.Domain.Ports;

/// <summary>The Telegram-bound weekly brief: <c>Headline</c> is the alert title, <c>Body</c> the message.</summary>
public sealed record PerformanceBrief(string Headline, string Body);

/// <summary>
/// Composes the weekly verdict-first brief (feature 414) from the TWR scoreboard, the Notable
/// portfolio-scanner signals, and the thesis track record. Pure — no I/O, no clock.
///
/// Layout, in reservation order so the highest-value lines survive the budget:
/// headline · scoreboard · blank · track record · drift trends · blank · one suggested action.
/// </summary>
public static class PerformanceBriefComposer
{
    /// <summary>Ledger message-format rule: the delivered message, headline included, stays ≤12 lines.</summary>
    private const int MaxMessageLines = 12;

    /// <summary>Blank separator + the action line itself.</summary>
    private const int ActionSectionLines = 2;

    private const int MaxTrendLines = 4;

    private const string Benchmark = "SPY";
    private const decimal PercentDivisor = 100m;
    private const decimal ThousandThreshold = 1_000m;
    private const decimal MillionThreshold = 1_000_000m;

    private const string StatusOverBand = "OverBand";
    private const string StatusUnderBand = "UnderBand";

    public static PerformanceBrief Compose(
        BookPerformanceResult result,
        IReadOnlyList<RadarSignal> portfolioSignals,
        TrackRecordDelta? trackRecord)
    {
        var headline = BuildHeadline(result);
        var lines = BuildScoreboard(result);

        var action = BuildActionLine(portfolioSignals);

        // Budget: everything below the scoreboard shares what the headline and the reserved
        // action section leave behind.
        var available = MaxMessageLines - 1 - lines.Count - (action is null ? 0 : ActionSectionLines);

        var context = new List<string>();
        if (available > 1)
        {
            var trackLine = BuildTrackRecordLine(trackRecord);
            if (trackLine is not null)
            {
                context.Add(trackLine);
            }

            var trendBudget = Math.Min(MaxTrendLines, available - 1 - context.Count);
            if (trendBudget > 0)
            {
                context.AddRange(BuildDriftTrendLines(NotableDrift(portfolioSignals), trendBudget));
            }
        }

        if (context.Count > 0)
        {
            lines.Add(string.Empty);
            lines.AddRange(context);
        }

        if (action is not null)
        {
            lines.Add(string.Empty);
            lines.Add(action);
        }

        return new PerformanceBrief(headline, string.Join("\n", lines));
    }

    private static string BuildHeadline(BookPerformanceResult result)
    {
        var weekly = result.Periods.FirstOrDefault(p => p.Period == BookPerformancePeriod.OneWeek);
        var verdict = weekly?.Verdict ?? result.Periods[0].Verdict ?? "N/A";
        var delta = weekly?.Delta;

        return delta.HasValue
            ? $"Weekly brief: {CapitalizeFirst(verdict)} ({delta.Value:+0.##%;-0.##%;0%} vs {Benchmark})"
            : $"Weekly brief: {CapitalizeFirst(verdict)}";
    }

    private static List<string> BuildScoreboard(BookPerformanceResult result)
    {
        var lines = new List<string>(result.Periods.Count);

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

            lines.Add($"{label}: Book {book} | {Benchmark} {spy}{diff}");
        }

        return lines;
    }

    /// <summary>
    /// The track-record delta: how Denys' own calls fared against the benchmark. Terminal and open
    /// records are reported separately, never blended (feature 020 R4).
    /// </summary>
    private static string? BuildTrackRecordLine(TrackRecordDelta? record)
    {
        if (record is null || record.Count == 0 ||
            (record.HitRatePct is null && record.AverageExcessReturnPct is null))
        {
            return null;
        }

        var bucket = record.IsTerminal ? "closed" : "open";
        var verb = record.IsTerminal ? "beat" : "ahead of";
        var parts = new List<string>(2);

        if (record.HitRatePct.HasValue)
        {
            parts.Add($"{record.HitRatePct.Value:0.#}% of {record.Count} {bucket} {verb} {Benchmark}");
        }
        else
        {
            parts.Add($"{record.Count} {bucket}");
        }

        if (record.AverageExcessReturnPct.HasValue)
        {
            parts.Add($"avg Δ {record.AverageExcessReturnPct.Value:+0.#;-0.#;0}%");
        }

        var caveat = record.LowSample ? " (low sample)" : string.Empty;
        return $"Calls: {string.Join(", ", parts)}{caveat}";
    }

    /// <summary>
    /// At most one action, judged against the policy boundary the scanner already encodes:
    /// IPS allocation bands first, then the cash floor, then the single-position cap. No breach on
    /// file means no line — the brief stays silent rather than inventing something to say.
    /// </summary>
    private static string? BuildActionLine(IReadOnlyList<RadarSignal> signals)
        => DriftAction(NotableDrift(signals)) ?? CashBufferAction(signals) ?? ConcentrationAction(signals);

    private static string? DriftAction(IReadOnlyList<RadarSignal> driftSignals)
    {
        var worst = MostRecentPerSubject(driftSignals)
            .OrderByDescending(s => Math.Abs(ReadDecimal(s, "driftPct")))
            .FirstOrDefault();

        if (worst is null)
        {
            return null;
        }

        var drift = ReadDecimal(worst, "driftPct");
        var target = ReadDecimal(worst, "targetPct");
        var swing = FormatUsd(Math.Abs(drift) / PercentDivisor * ReadDecimal(worst, "totalUsd"));

        return ReadString(worst, "status") == StatusUnderBand
            ? $"Action: Add ~{Math.Abs(drift):0.#}pp (~{swing}) to {worst.Subject} to reach its {target:0.#}% IPS target."
            : $"Action: Trim {worst.Subject} by ~{Math.Abs(drift):0.#}pp (~{swing}) to its {target:0.#}% IPS target.";
    }

    private static string? CashBufferAction(IReadOnlyList<RadarSignal> signals)
    {
        var breach = MostRecent(signals, RadarSignalTypes.CashBuffer, s => !ReadBool(s, "compliant"));
        if (breach is null)
        {
            return null;
        }

        var cashPct = ReadDecimal(breach, "cashPct");
        var floorPct = ReadDecimal(breach, "minCashBufferPct");
        var shortfall = FormatUsd((floorPct - cashPct) / PercentDivisor * ReadDecimal(breach, "totalUsd"));

        return $"Action: Rebuild cash to the {floorPct:0.#}% floor — now {cashPct:0.#}% (~{shortfall} short).";
    }

    private static string? ConcentrationAction(IReadOnlyList<RadarSignal> signals)
    {
        var breach = MostRecent(signals, RadarSignalTypes.ConcentrationWeight, s => ReadBool(s, "overLimit"));
        if (breach is null)
        {
            return null;
        }

        return $"Action: Trim {breach.Subject} to the {ReadDecimal(breach, "limitPct"):0.#}% cap — " +
               $"now {ReadDecimal(breach, "weightPct"):0.#}% of the book.";
    }

    private static IReadOnlyList<string> BuildDriftTrendLines(IReadOnlyList<RadarSignal> signals, int maxLines)
    {
        var bySubject = MostRecentPerSubject(signals)
            .OrderByDescending(s => Math.Abs(ReadDecimal(s, "driftPct")))
            .Take(maxLines);

        return bySubject
            .Select(s => $"Drift: {s.Subject} {ReadString(s, "status")} " +
                         $"({ReadDecimal(s, "driftPct"):+0.#;-0.#;0}pp vs target)")
            .ToList();
    }

    private static IReadOnlyList<RadarSignal> NotableDrift(IReadOnlyList<RadarSignal> signals)
        => signals
            .Where(s => s.SignalType == RadarSignalTypes.AllocationDrift)
            .Where(s => ReadString(s, "status") is StatusOverBand or StatusUnderBand)
            .ToList();

    /// <summary>One signal per subject — the most recent, so a resolved older reading never wins.</summary>
    private static IEnumerable<RadarSignal> MostRecentPerSubject(IReadOnlyList<RadarSignal> signals)
        => signals
            .GroupBy(s => s.Subject)
            .Select(g => g.OrderByDescending(s => s.Timestamp).First());

    private static RadarSignal? MostRecent(
        IReadOnlyList<RadarSignal> signals, string signalType, Func<RadarSignal, bool> predicate)
        => signals
            .Where(s => s.SignalType == signalType)
            .OrderByDescending(s => s.Timestamp)
            .FirstOrDefault(predicate);

    private static string FormatUsd(decimal usd)
    {
        var abs = Math.Abs(usd);
        return abs >= MillionThreshold ? $"${usd / MillionThreshold:0.#}m"
            : abs >= ThousandThreshold ? $"${usd / ThousandThreshold:0.#}k"
            : $"${usd:0}";
    }

    /// <summary>Payload values arrive as <see cref="JsonElement"/> from jsonb, or as CLR types in tests.</summary>
    private static decimal ReadDecimal(RadarSignal signal, string key)
    {
        if (!signal.Payload.TryGetValue(key, out var raw))
        {
            return 0m;
        }

        return raw switch
        {
            JsonElement el when el.ValueKind == JsonValueKind.Number => el.GetDecimal(),
            double d => (decimal)d,
            decimal d => d,
            int i => i,
            long l => l,
            _ => 0m,
        };
    }

    private static string ReadString(RadarSignal signal, string key)
    {
        if (!signal.Payload.TryGetValue(key, out var raw))
        {
            return string.Empty;
        }

        return raw switch
        {
            JsonElement el => el.GetString() ?? string.Empty,
            string s => s,
            _ => raw.ToString() ?? string.Empty,
        };
    }

    private static bool ReadBool(RadarSignal signal, string key)
        => signal.Payload.TryGetValue(key, out var raw) && raw switch
        {
            JsonElement el => el.ValueKind == JsonValueKind.True,
            bool b => b,
            _ => false,
        };

    private static string CapitalizeFirst(string? s)
        => s is null or "" ? string.Empty : char.ToUpperInvariant(s[0]) + s[1..];
}
