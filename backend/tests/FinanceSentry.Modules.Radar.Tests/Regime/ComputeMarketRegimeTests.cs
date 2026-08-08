using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Radar.Application.Commands;
using FinanceSentry.Modules.Radar.Application.Queries;
using FinanceSentry.Modules.Radar.Application.Services;
using FinanceSentry.Modules.Radar.Domain;
using FinanceSentry.Modules.Radar.Domain.Regime;
using FinanceSentry.Modules.Radar.Domain.Repositories;
using FinanceSentry.Modules.Radar.Infrastructure.MarketData;
using FinanceSentry.Modules.Radar.Infrastructure.Persistence;
using FinanceSentry.Modules.Radar.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FinanceSentry.Modules.Radar.Tests.Regime;

public sealed class ComputeMarketRegimeTests
{
    private sealed class FakeYieldSource(YieldCurveReading? reading, bool configured = true) : IYieldCurveSource
    {
        public bool IsConfigured => configured;

        public Task<YieldCurveReading?> GetLatestAsync(CancellationToken ct = default)
            => Task.FromResult(reading);
    }

    private static IReadOnlyList<DailyBarData> VixBars(params decimal[] closes)
    {
        var start = new DateOnly(2026, 7, 1);
        return closes
            .Select((c, i) => new DailyBarData(start.AddDays(i), c, c, c, c, c, 0L))
            .ToList();
    }

    private static ComputeMarketRegimeCommandHandler Handler(
        RadarDbContext db,
        IReadOnlyList<DailyBarData> vixBars,
        YieldCurveReading? yield,
        bool yieldConfigured = true)
    {
        var history = new FakeHistorySource(new Dictionary<string, IReadOnlyList<DailyBarData>> { ["^VIX"] = vixBars });
        var signalWriter = new RadarSignalWriter(new RadarSignalRepository(db), TestSupport.Options());
        return new ComputeMarketRegimeCommandHandler(
            history,
            new FakeYieldSource(yield, yieldConfigured),
            new RegimeReadingRepository(db),
            signalWriter,
            Options.Create(new RegimeOptions()),
            NullLogger<ComputeMarketRegimeCommandHandler>.Instance);
    }

    private static Task<int> CountAsync(RadarDbContext db, string signalType)
        => new RadarSignalRepository(db)
            .ListAsync(new SignalFilter(Scanner: RadarScanners.MarketRegime, SignalType: signalType))
            .ContinueWith(t => t.Result.Count);

    [Fact]
    public async Task Persists_BothAxes_WhenAvailable()
    {
        await using var db = TestSupport.NewContext();
        var handler = Handler(db, VixBars(12m, 13m, 14m), new YieldCurveReading(3.71m, 4.08m, new DateOnly(2026, 8, 7)));

        var summary = await handler.Handle(new ComputeMarketRegimeCommand(), default);

        summary.VolatilityAvailable.Should().BeTrue();
        summary.RatesAvailable.Should().BeTrue();
        var latest = await new RegimeReadingRepository(db).LatestAsync();
        latest.Should().NotBeNull();
        latest!.VolatilityRegime.Should().Be(VolatilityRegime.Calm);
        latest.RatesRegime.Should().Be(RatesRegime.Inverted);
        latest.RecessionWarning.Should().BeTrue();
    }

    [Fact]
    public async Task NoChangeRunTwice_EmitsInfoEachRun_NoChangeSignals()
    {
        await using var db = TestSupport.NewContext();
        var yield = new YieldCurveReading(4.6m, 4.0m, new DateOnly(2026, 8, 7)); // spread 0.6 -> Normal

        var h1 = Handler(db, VixBars(18m, 18m, 18m), yield); // Normal vol
        await h1.Handle(new ComputeMarketRegimeCommand(), default);
        var h2 = Handler(db, VixBars(18m, 18m, 18m), yield); // still Normal
        await h2.Handle(new ComputeMarketRegimeCommand(), default);

        (await CountAsync(db, RadarSignalTypes.RegimeVolatility)).Should().Be(2);
        (await CountAsync(db, RadarSignalTypes.RegimeRates)).Should().Be(2);
        (await CountAsync(db, RadarSignalTypes.RegimeChange)).Should().Be(0);
    }

    [Fact]
    public async Task BandCrossOnOneAxis_EmitsSingleChange_OtherAxisInfoOnly()
    {
        await using var db = TestSupport.NewContext();
        var yield = new YieldCurveReading(4.6m, 4.0m, new DateOnly(2026, 8, 7)); // Normal both runs

        // Run 1: Calm volatility.
        await Handler(db, VixBars(12m, 12m, 12m), yield).Handle(new ComputeMarketRegimeCommand(), default);
        // Run 2: Panic volatility (band cross); rates unchanged.
        var summary = await Handler(db, VixBars(34m, 34m, 34m), yield).Handle(new ComputeMarketRegimeCommand(), default);

        summary.VolatilityChanged.Should().BeTrue();
        summary.RatesChanged.Should().BeFalse();

        var changes = await new RadarSignalRepository(db).ListAsync(
            new SignalFilter(Scanner: RadarScanners.MarketRegime, SignalType: RadarSignalTypes.RegimeChange));
        changes.Should().ContainSingle();
        changes[0].Subject.Should().Be("volatility");
        changes[0].Severity.Should().Be(SignalSeverity.Notable);
    }

    [Fact]
    public async Task FirstEverRun_EmitsNoChange()
    {
        await using var db = TestSupport.NewContext();
        var summary = await Handler(db, VixBars(34m), new YieldCurveReading(3.7m, 4.1m, new DateOnly(2026, 8, 7)))
            .Handle(new ComputeMarketRegimeCommand(), default);

        summary.VolatilityChanged.Should().BeFalse();
        summary.RatesChanged.Should().BeFalse();
        (await CountAsync(db, RadarSignalTypes.RegimeChange)).Should().Be(0);
    }

    [Fact]
    public async Task ReRunningAfterChange_DoesNotDuplicateChangeSignal()
    {
        await using var db = TestSupport.NewContext();
        var yield = new YieldCurveReading(4.6m, 4.0m, new DateOnly(2026, 8, 7));

        await Handler(db, VixBars(12m), yield).Handle(new ComputeMarketRegimeCommand(), default);   // Calm
        await Handler(db, VixBars(34m), yield).Handle(new ComputeMarketRegimeCommand(), default);   // -> Panic (change)
        await Handler(db, VixBars(34m), yield).Handle(new ComputeMarketRegimeCommand(), default);   // still Panic (prior=Panic)

        (await CountAsync(db, RadarSignalTypes.RegimeChange)).Should().Be(1);
    }

    [Fact]
    public async Task VixOutage_SkipsVolatilityAxis_RatesStillComputes()
    {
        await using var db = TestSupport.NewContext();
        var handler = Handler(db, VixBars(), new YieldCurveReading(3.7m, 4.1m, new DateOnly(2026, 8, 7)));

        var summary = await handler.Handle(new ComputeMarketRegimeCommand(), default);

        summary.VolatilityAvailable.Should().BeFalse();
        summary.RatesAvailable.Should().BeTrue();
        (await CountAsync(db, RadarSignalTypes.RegimeVolatility)).Should().Be(0);
        (await CountAsync(db, RadarSignalTypes.RegimeRates)).Should().Be(1);
        var latest = await new RegimeReadingRepository(db).LatestAsync();
        latest!.VolatilityAvailable.Should().BeFalse();
        latest.RatesAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task KeylessFredAndVixOutage_PersistsNothing()
    {
        await using var db = TestSupport.NewContext();
        var handler = Handler(db, VixBars(), yield: null, yieldConfigured: false);

        var summary = await handler.Handle(new ComputeMarketRegimeCommand(), default);

        summary.VolatilityAvailable.Should().BeFalse();
        summary.RatesAvailable.Should().BeFalse();
        (await new RegimeReadingRepository(db).LatestAsync()).Should().BeNull();
    }

    [Fact]
    public async Task GetMarketRegimeQuery_ReturnsEmpty_WhenNoReadings()
    {
        await using var db = TestSupport.NewContext();
        var result = await new GetMarketRegimeQueryHandler(new RegimeReadingRepository(db))
            .Handle(new GetMarketRegimeQuery(), default);

        result.AsOf.Should().BeNull();
        result.Volatility.Available.Should().BeFalse();
        result.Rates.Available.Should().BeFalse();
    }

    [Fact]
    public async Task GetMarketRegimeQuery_ReturnsBothAxes_AfterCompute()
    {
        await using var db = TestSupport.NewContext();
        await Handler(db, VixBars(24m, 24m), new YieldCurveReading(3.71m, 4.08m, new DateOnly(2026, 8, 7)))
            .Handle(new ComputeMarketRegimeCommand(), default);

        var result = await new GetMarketRegimeQueryHandler(new RegimeReadingRepository(db))
            .Handle(new GetMarketRegimeQuery(), default);

        result.AsOf.Should().NotBeNull();
        result.Volatility.Available.Should().BeTrue();
        result.Volatility.Regime.Should().Be(nameof(VolatilityRegime.Stressed));
        result.Rates.Available.Should().BeTrue();
        result.Rates.Regime.Should().Be(nameof(RatesRegime.Inverted));
        result.Rates.RecessionWarning.Should().BeTrue();
    }
}
