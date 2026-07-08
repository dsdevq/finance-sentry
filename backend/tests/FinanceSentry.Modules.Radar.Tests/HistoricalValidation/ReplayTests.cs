using FinanceSentry.Modules.Radar.Domain;
using FinanceSentry.Modules.Radar.Domain.MarketStructure;
using FluentAssertions;
using Xunit;

namespace FinanceSentry.Modules.Radar.Tests.HistoricalValidation;

public sealed class ReplayTests
{
    private static List<DailyBar> BuildSeries(int calmDays, decimal crashDropPct)
    {
        var bars = new List<DailyBar>();
        var date = new DateOnly(2020, 1, 1);
        decimal price = 100m;

        // Calm period with small alternating moves so σ is small but non-zero.
        for (var i = 0; i < calmDays; i++)
        {
            price = i % 2 == 0 ? 100m : 100.5m;
            bars.Add(Bar(date.AddDays(i), price));
        }

        // The loud day: a large drop vs the calm volatility.
        var crashPrice = price * (1 - crashDropPct);
        bars.Add(Bar(date.AddDays(calmDays), crashPrice));

        return bars;
    }

    private static DailyBar Bar(DateOnly d, decimal price) => new()
    {
        Ticker = "MU", Date = d, Open = price, High = price + 1, Low = price - 1,
        Close = price, AdjClose = price, Volume = 1_000_000,
    };

    [Fact]
    public void Replay_DetectsUnusualMove_OnTheLoudDay()
    {
        // 2026-07-07 memory-rotation style ~16% gap-down after a calm stretch.
        var series = BuildSeries(calmDays: 80, crashDropPct: 0.16m);
        var thresholds = new HistoricalReplay.Thresholds(
            UnusualMoveZScore: 3m, ExtensionThreshold: 0.15m, VolWindow: 63);

        var detections = HistoricalReplay.Replay(series, thresholds);
        var loudDay = series[^1].Date;

        detections.Should().Contain(d =>
            d.SignalType == RadarReplaySignalTypes.UnusualMove &&
            d.Date == loudDay);
    }

    [Fact]
    public void Replay_DoesNotSpam_OnCalmSeries()
    {
        var calm = BuildSeries(calmDays: 120, crashDropPct: 0m); // last "crash" is a no-op drop
        var thresholds = new HistoricalReplay.Thresholds(3m, 0.15m, 63);

        var detections = HistoricalReplay.Replay(calm, thresholds);

        detections.Count(d => d.SignalType == RadarReplaySignalTypes.UnusualMove)
            .Should().Be(0, "a calm series must not produce unusual-move spam");
    }

    [Fact]
    public void Replay_IsEmpty_WhenHistoryShorterThanVolWindow()
    {
        var shortSeries = BuildSeries(calmDays: 10, crashDropPct: 0.16m);
        HistoricalReplay.Replay(shortSeries, new HistoricalReplay.Thresholds(3m, 0.15m, 63))
            .Should().BeEmpty();
    }
}
