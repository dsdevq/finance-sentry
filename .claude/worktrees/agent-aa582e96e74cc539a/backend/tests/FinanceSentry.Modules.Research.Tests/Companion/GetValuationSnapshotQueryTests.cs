namespace FinanceSentry.Modules.Research.Tests.Companion;

using FinanceSentry.Modules.Research.Application.Queries;
using FinanceSentry.Modules.Research.Application.Services;
using FluentAssertions;
using Xunit;

/// <summary>
/// Valuation-snapshot compose logic (feature 030, US2): trailing-P/E history attaches, other metrics
/// flag historyUnavailable, implied upside computes from consensus target, every call persists a
/// snapshot, and non-equity tickers return an explicit not-applicable result (never fabricated).
/// </summary>
public sealed class GetValuationSnapshotQueryTests
{
    private static ValuationCurrentMetrics Equity(
        string ticker, decimal price, decimal? target, decimal? trailingPe = 24m, decimal? forwardPe = 21m,
        decimal? evToEbitda = 16m, decimal? divYield = 0.02m, string? sector = "Consumer Cyclical") => new(
        ticker, price, trailingPe, forwardPe, evToEbitda, divYield, target,
        IsStale: false, NotApplicable: false, sector, "Restaurants");

    [Fact]
    public async Task Composes_metrics_history_and_implied_upside_and_persists()
    {
        var valuation = new FakeValuationDataService();
        valuation.Metrics["MCD"] = Equity("MCD", price: 265m, target: 336m);
        var snapshots = new FakeValuationSnapshotRepository();

        var handler = new GetValuationSnapshotQueryHandler(
            valuation, new FakeValuationHistoryService(new TrailingPeHistory(26m, 5)), snapshots);

        var result = await handler.Handle(new GetValuationSnapshotQuery("mcd", null), default);

        result.NotApplicable.Should().BeFalse();
        result.Metrics.TrailingPe.Value.Should().Be(24m);
        result.Metrics.TrailingPe.FiveYearAvg.Should().Be(26m);
        result.Metrics.TrailingPe.HistoryWindowYears.Should().Be(5);
        result.Metrics.ForwardPe.HistoryUnavailable.Should().BeTrue();
        result.Metrics.EvToEbitda.HistoryUnavailable.Should().BeTrue();
        result.ImpliedUpsidePct.Should().Be(26.8m, "(336 / 265 - 1) * 100 rounds to 26.8");
        result.Sources.Should().Contain("yahoo:quoteSummary").And.Contain("sec-edgar:xbrl");
        snapshots.Added.Should().ContainSingle().Which.Ticker.Should().Be("MCD");
    }

    [Fact]
    public async Task Crypto_returns_not_applicable_and_persists_nothing()
    {
        var valuation = new FakeValuationDataService();
        valuation.Metrics["SOL-USD"] = new ValuationCurrentMetrics(
            "SOL-USD", null, null, null, null, null, null,
            IsStale: false, NotApplicable: true, null, null);
        var snapshots = new FakeValuationSnapshotRepository();

        var result = await new GetValuationSnapshotQueryHandler(
                valuation, new FakeValuationHistoryService(), snapshots)
            .Handle(new GetValuationSnapshotQuery("SOL-USD", null), default);

        result.NotApplicable.Should().BeTrue();
        result.Metrics.TrailingPe.Value.Should().BeNull();
        snapshots.Added.Should().BeEmpty();
    }

    [Fact]
    public async Task No_consensus_target_yields_null_implied_upside()
    {
        var valuation = new FakeValuationDataService();
        valuation.Metrics["ABC"] = Equity("ABC", price: 100m, target: null);

        var result = await new GetValuationSnapshotQueryHandler(
                valuation, new FakeValuationHistoryService(), new FakeValuationSnapshotRepository())
            .Handle(new GetValuationSnapshotQuery("ABC", null), default);

        result.ImpliedUpsidePct.Should().BeNull();
    }

    [Fact]
    public async Task Default_peer_set_is_named_from_sector_and_populated()
    {
        var valuation = new FakeValuationDataService();
        valuation.Metrics["MCD"] = Equity("MCD", price: 265m, target: 336m);
        valuation.Metrics["YUM"] = Equity("YUM", price: 140m, target: 150m, forwardPe: 24m, evToEbitda: 19m);
        valuation.DefaultPeers.Add("YUM");

        var result = await new GetValuationSnapshotQueryHandler(
                valuation, new FakeValuationHistoryService(), new FakeValuationSnapshotRepository())
            .Handle(new GetValuationSnapshotQuery("MCD", null), default);

        result.PeerSet.Should().NotBeNull();
        result.PeerSet!.Name.Should().Be("sector:Consumer Cyclical (default)");
        result.PeerSet.Peers.Should().ContainSingle();
        result.PeerSet.Peers[0].Ticker.Should().Be("YUM");
        result.PeerSet.Peers[0].ForwardPe.Should().Be(24m);
    }

    [Fact]
    public async Task Explicit_peers_override_the_default_set_and_name_is_custom()
    {
        var valuation = new FakeValuationDataService();
        valuation.Metrics["MCD"] = Equity("MCD", price: 265m, target: 336m);
        valuation.Metrics["SBUX"] = Equity("SBUX", price: 95m, target: 110m);
        valuation.DefaultPeers.Add("YUM"); // must be ignored when explicit peers are supplied

        var result = await new GetValuationSnapshotQueryHandler(
                valuation, new FakeValuationHistoryService(), new FakeValuationSnapshotRepository())
            .Handle(new GetValuationSnapshotQuery("MCD", ["SBUX"]), default);

        result.PeerSet!.Name.Should().Be("custom");
        result.PeerSet.Peers.Should().ContainSingle().Which.Ticker.Should().Be("SBUX");
    }
}
