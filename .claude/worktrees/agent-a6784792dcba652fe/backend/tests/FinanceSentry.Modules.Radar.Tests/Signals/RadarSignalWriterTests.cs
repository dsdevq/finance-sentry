using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Radar.Application.Services;
using FinanceSentry.Modules.Radar.Domain.Repositories;
using FinanceSentry.Modules.Radar.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Xunit;

namespace FinanceSentry.Modules.Radar.Tests.Signals;

public sealed class RadarSignalWriterTests
{
    private static RadarSignalRequest Request(string type, SignalSeverity severity, string dedupKey) => new(
        "market_structure", type, severity, "Ticker", "NVDA", null, dedupKey,
        new Dictionary<string, object> { ["z"] = 3.5m });

    [Fact]
    public async Task Notable_IsDedupedByDedupKey_WithinSilenceWindow()
    {
        await using var db = TestSupport.NewContext();
        var repo = new RadarSignalRepository(db);
        var writer = new RadarSignalWriter(repo, TestSupport.Options());

        var req = Request("unusual_move", SignalSeverity.Notable, "market_structure:unusual_move:NVDA:2026-07-07");
        await writer.AppendSignalAsync(req);
        await writer.AppendSignalAsync(req); // same DedupKey within silence window → suppressed

        var stored = await repo.ListAsync(new SignalFilter());
        stored.Should().ContainSingle();
    }

    [Fact]
    public async Task Info_IsRecordedEveryRun()
    {
        await using var db = TestSupport.NewContext();
        var repo = new RadarSignalRepository(db);
        var writer = new RadarSignalWriter(repo, TestSupport.Options());

        var req = Request("breadth", SignalSeverity.Info, "market_structure:breadth:universe:2026-07-07");
        await writer.AppendSignalAsync(req);
        await writer.AppendSignalAsync(req); // info repeats

        var stored = await repo.ListAsync(new SignalFilter());
        stored.Should().HaveCount(2);
    }

    [Fact]
    public async Task Append_IsAdditive_AndRoundTripsPayload()
    {
        await using var db = TestSupport.NewContext();
        var repo = new RadarSignalRepository(db);
        var writer = new RadarSignalWriter(repo, TestSupport.Options());

        await writer.AppendSignalAsync(Request("unusual_move", SignalSeverity.Notable, "k1"));
        await writer.AppendSignalAsync(Request("rotation_shift", SignalSeverity.Notable, "k2"));

        var stored = await repo.ListAsync(new SignalFilter());
        stored.Should().HaveCount(2);
        stored.Should().OnlyContain(s => s.Payload.ContainsKey("z"));
    }
}
