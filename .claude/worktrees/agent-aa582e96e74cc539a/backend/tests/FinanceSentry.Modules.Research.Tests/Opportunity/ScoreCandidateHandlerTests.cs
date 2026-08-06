namespace FinanceSentry.Modules.Research.Tests.Opportunity;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Research.Application.Commands;
using FinanceSentry.Modules.Research.Application.Services;
using FinanceSentry.Modules.Research.Domain;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

public sealed class ScoreCandidateHandlerTests
{
    private static ScoreCandidateCommandHandler BuildHandler(
        FakeCandidateRepository candidates,
        FakeCandidateScoreRepository scores,
        InvestmentPolicyStatement? ips = null,
        IReadOnlyList<BrokerageHoldingSummary>? holdings = null,
        IReadOnlyList<FundamentalFact>? facts = null,
        MarketStructureSnapshot? structure = null)
        => new(
            candidates,
            scores,
            new FakeMarketStructureReader(structure),
            new FakeSecEdgarService(facts),
            new FakeIpsRepository(ips),
            new FakeBrokerageHoldingsReader(holdings),
            new RecordingRadarSignalWriter(),
            new RecordingThesisEventRecorder(),
            new FakeOpportunityAlertGenerator(),
            Options.Create(new OpportunityOptions()));

    [Fact]
    public async Task Handle_ReScore_AppendsCandidateScore_WithoutDuplicatingCandidate()
    {
        var candidates = new FakeCandidateRepository();
        var scores = new FakeCandidateScoreRepository();
        var handler = BuildHandler(candidates, scores);
        var userId = Guid.NewGuid();

        var first = await handler.Handle(new ScoreCandidateCommand(userId, "MSFT"), CancellationToken.None);
        var second = await handler.Handle(new ScoreCandidateCommand(userId, "MSFT"), CancellationToken.None);

        first.IsNewCandidate.Should().BeTrue();
        second.IsNewCandidate.Should().BeFalse();
        second.CandidateId.Should().Be(first.CandidateId);

        candidates.Candidates.Should().ContainSingle();
        scores.Scores.Count(s => s.CandidateId == first.CandidateId).Should().Be(2);
    }

    [Fact]
    public async Task Handle_FlagsIpsConcentration_WhenHeldTickerExceedsMaxSinglePosition()
    {
        var userId = Guid.NewGuid();
        var ips = new InvestmentPolicyStatement { UserId = userId, MaxSinglePositionPct = 0.10m };
        var holdings = new List<BrokerageHoldingSummary>
        {
            new("MSFT", "STK", 100m, 8_000m, DateTime.UtcNow, "ibkr"),
            new("AAPL", "STK", 100m, 2_000m, DateTime.UtcNow, "ibkr"),
        };

        var handler = BuildHandler(new FakeCandidateRepository(), new FakeCandidateScoreRepository(), ips, holdings);
        var result = await handler.Handle(new ScoreCandidateCommand(userId, "MSFT"), CancellationToken.None);

        // MSFT is 80% of the $10k book vs a 10% cap → concentration flag trips.
        result.Scorecard.IpsFit.CurrentWeight.Should().Be(0.80m);
        result.Scorecard.IpsFit.MaxSinglePositionPct.Should().Be(0.10m);
        result.Scorecard.IpsFit.WithinConcentration.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_LeavesSubScoresNull_WhenNoStructureOrFundamentalsData()
    {
        var handler = BuildHandler(new FakeCandidateRepository(), new FakeCandidateScoreRepository());

        var result = await handler.Handle(
            new ScoreCandidateCommand(Guid.NewGuid(), "MSFT"), CancellationToken.None);

        // Not-evaluable is labeled null, never faked to a neutral number (FR-002/FR-006).
        result.Scorecard.StructureScore.Should().BeNull();
        result.Scorecard.FundamentalsScore.Should().BeNull();
    }
}
