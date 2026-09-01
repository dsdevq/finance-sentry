namespace FinanceSentry.Tests.Unit.Radar;

using FluentAssertions;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Radar.Domain;
using FinanceSentry.Modules.Radar.Infrastructure.Jobs;
using Xunit;

/// <summary>
/// Unit tests for BookPerformanceBriefJob message composition (feature 414).
/// Exercises BuildMessage directly — no Hangfire, no DB.
/// </summary>
public sealed class BookPerformanceBriefJobTests
{
    private static BookPerformanceResult OneWeekResult(
        decimal bookTwr = 0.03m, decimal spyTwr = 0.01m) =>
        new(
            [new PeriodTwr(
                BookPerformancePeriod.OneWeek,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
                bookTwr, spyTwr,
                bookTwr - spyTwr,
                bookTwr > spyTwr + 0.001m ? "outperform"
                    : bookTwr < spyTwr - 0.001m ? "underperform" : "inline")],
            DateOnly.FromDateTime(DateTime.UtcNow));

    private static RadarSignal DriftSignal(
        string subject, string status, double driftPct,
        DateTimeOffset? timestamp = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Scanner = RadarScanners.Portfolio,
            SignalType = RadarSignalTypes.AllocationDrift,
            Severity = SignalSeverity.Notable,
            SubjectType = RadarSubjectTypes.AssetClass,
            Subject = subject,
            UserId = Guid.NewGuid(),
            DedupKey = $"test:{subject}",
            Timestamp = timestamp ?? DateTimeOffset.UtcNow,
            Payload = new Dictionary<string, object>
            {
                ["status"] = status,
                ["driftPct"] = driftPct,
                ["targetPct"] = 0.4,
                ["actualPct"] = 0.4 + driftPct,
            },
        };

    [Fact]
    public void BuildMessage_Headline_IncludesVerdictAndDelta()
    {
        var result = OneWeekResult(bookTwr: 0.03m, spyTwr: 0.01m);
        var (headline, _) = BookPerformanceBriefJob.BuildMessage(result, []);

        headline.Should().StartWith("Weekly brief:");
        headline.Should().Contain("Outperform");
        headline.Should().Contain("vs SPY");
    }

    [Fact]
    public void BuildMessage_Body_ContainsFourPeriodLines_WhenAllPeriodsPresent()
    {
        var result = new BookPerformanceResult(
            [
                new PeriodTwr(BookPerformancePeriod.OneWeek, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
                    0.03m, 0.01m, 0.02m, "outperform"),
                new PeriodTwr(BookPerformancePeriod.OneMonth, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
                    0.05m, 0.04m, 0.01m, "outperform"),
                new PeriodTwr(BookPerformancePeriod.ThreeMonths, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-3)),
                    0.10m, 0.08m, 0.02m, "outperform"),
                new PeriodTwr(BookPerformancePeriod.OneYear, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
                    0.25m, 0.20m, 0.05m, "outperform"),
            ],
            DateOnly.FromDateTime(DateTime.UtcNow));

        var (_, body) = BookPerformanceBriefJob.BuildMessage(result, []);

        var lines = body.Split('\n');
        lines.Should().Contain(l => l.StartsWith("1W:"));
        lines.Should().Contain(l => l.StartsWith("1M:"));
        lines.Should().Contain(l => l.StartsWith("3M:"));
        lines.Should().Contain(l => l.StartsWith("1Y:"));
    }

    [Fact]
    public void BuildMessage_AppendsDriftTrend_WhenNotableSignalPresent()
    {
        var result = OneWeekResult();
        var signal = DriftSignal("Equity", "OverBand", 0.083);

        var (_, body) = BookPerformanceBriefJob.BuildMessage(result, [signal]);

        body.Should().Contain("Drift: Equity OverBand");
        body.Should().Contain("vs target");
    }

    [Fact]
    public void BuildMessage_NoDriftLines_WhenNoSignals()
    {
        var result = OneWeekResult();

        var (_, body) = BookPerformanceBriefJob.BuildMessage(result, []);

        body.Should().NotContain("Drift:");
    }

    [Fact]
    public void BuildMessage_DeduplicatesSubject_TakingMostRecent()
    {
        var result = OneWeekResult();

        var older = DriftSignal("Equity", "OverBand", 0.05, DateTimeOffset.UtcNow.AddDays(-10));
        var newer = DriftSignal("Equity", "UnderBand", 0.12, DateTimeOffset.UtcNow.AddDays(-1));

        var (_, body) = BookPerformanceBriefJob.BuildMessage(result, [older, newer]);

        // Only the most recent Equity signal appears.
        var driftLines = body.Split('\n').Where(l => l.StartsWith("Drift:")).ToList();
        driftLines.Should().HaveCount(1);
        driftLines[0].Should().Contain("UnderBand");
    }

    [Fact]
    public void BuildMessage_SortsSignals_ByAbsoluteDriftDescending()
    {
        var result = OneWeekResult();

        var smallDrift = DriftSignal("Bonds", "UnderBand", -0.03);
        var largeDrift = DriftSignal("Equity", "OverBand", 0.15);

        var (_, body) = BookPerformanceBriefJob.BuildMessage(result, [smallDrift, largeDrift]);

        var lines = body.Split('\n').Where(l => l.StartsWith("Drift:")).ToList();
        lines.Should().HaveCount(2);
        lines[0].Should().Contain("Equity"); // larger |drift| first
        lines[1].Should().Contain("Bonds");
    }

    [Fact]
    public void BuildMessage_TotalLinesDoNotExceedTwelve()
    {
        var result = new BookPerformanceResult(
            [
                new PeriodTwr(BookPerformancePeriod.OneWeek, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
                    0.03m, 0.01m, 0.02m, "outperform"),
                new PeriodTwr(BookPerformancePeriod.OneMonth, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
                    0.05m, 0.04m, 0.01m, "outperform"),
                new PeriodTwr(BookPerformancePeriod.ThreeMonths, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-3)),
                    0.10m, 0.08m, 0.02m, "outperform"),
                new PeriodTwr(BookPerformancePeriod.OneYear, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
                    0.25m, 0.20m, 0.05m, "outperform"),
            ],
            DateOnly.FromDateTime(DateTime.UtcNow));

        // 10 drift signals — cap should kick in.
        var manySignals = Enumerable.Range(1, 10)
            .Select(i => DriftSignal($"Asset{i}", "OverBand", 0.05 + i * 0.01))
            .ToList();

        var (_, body) = BookPerformanceBriefJob.BuildMessage(result, manySignals);

        var lines = body.Split('\n');
        lines.Should().HaveCountLessOrEqualTo(12);
    }

    [Fact]
    public void BuildMessage_FormatsUnderperformVerdict()
    {
        var result = OneWeekResult(bookTwr: 0.01m, spyTwr: 0.05m);

        var (headline, _) = BookPerformanceBriefJob.BuildMessage(result, []);

        headline.Should().Contain("Underperform");
    }

    [Fact]
    public void BuildMessage_HeadlineOmitsDelta_WhenWeeklyDataMissing()
    {
        var result = new BookPerformanceResult(
            [new PeriodTwr(
                BookPerformancePeriod.OneMonth,
                DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
                0.05m, 0.04m, 0.01m, "outperform")],
            DateOnly.FromDateTime(DateTime.UtcNow));

        var (headline, _) = BookPerformanceBriefJob.BuildMessage(result, []);

        // No delta shown when weekly period is absent.
        headline.Should().Be("Weekly brief: Outperform");
    }

    [Fact]
    public void BuildMessage_DriftPctNegative_ShowsNegativeSign()
    {
        var result = OneWeekResult();
        var signal = DriftSignal("Bonds", "UnderBand", -0.07);

        var (_, body) = BookPerformanceBriefJob.BuildMessage(result, [signal]);

        body.Should().Contain("Drift: Bonds UnderBand (-7");
    }
}
