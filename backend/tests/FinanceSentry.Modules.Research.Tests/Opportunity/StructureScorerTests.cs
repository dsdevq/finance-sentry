namespace FinanceSentry.Modules.Research.Tests.Opportunity;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Research.Domain.Scoring;
using FluentAssertions;
using Xunit;

public sealed class StructureScorerTests
{
    private static MarketStructureSnapshot Snapshot(
        IReadOnlyDictionary<int, decimal?>? rsByWindow = null,
        decimal? extension = 0.05m,
        decimal? zScore = 0m,
        decimal? volumeRatio = 1m,
        bool stale = false)
        => new(
            "MU",
            rsByWindow ?? new Dictionary<int, decimal?> { [63] = 0.10m },
            new Dictionary<int, decimal?> { [63] = 0.15m },
            extension,
            zScore,
            volumeRatio,
            50m,
            48m,
            stale);

    [Fact]
    public void Score_ReturnsNull_WhenSnapshotIsNull()
    {
        var (score, reasons) = StructureScorer.Score(null);

        score.Should().BeNull();
        reasons.Should().Contain("no_structure_data");
    }

    [Fact]
    public void Score_ReturnsNull_WhenNoWindowsExtensionOrZScoreEvaluable()
    {
        var snapshot = Snapshot(
            rsByWindow: new Dictionary<int, decimal?> { [63] = null },
            extension: null,
            zScore: null);

        var (score, reasons) = StructureScorer.Score(snapshot);

        score.Should().BeNull();
        reasons.Should().Contain("no_rs_windows");
        reasons.Should().Contain("no_extension_data");
        reasons.Should().Contain("no_zscore_data");
    }

    [Fact]
    public void Score_IsDeterministic_ForIdenticalInputs()
    {
        var snapshot = Snapshot();

        var (score1, _) = StructureScorer.Score(snapshot);
        var (score2, _) = StructureScorer.Score(snapshot);

        score1.Should().Be(score2);
        score1.Should().BeInRange(0, 100);
    }

    [Fact]
    public void Score_StaysWithinBounds_ForExtremeInputs()
    {
        var snapshot = Snapshot(
            rsByWindow: new Dictionary<int, decimal?> { [63] = 5m },
            extension: 2m,
            zScore: 10m);

        var (score, reasons) = StructureScorer.Score(snapshot);

        score.Should().NotBeNull();
        score!.Value.Should().BeInRange(0, 100);
        reasons.Should().BeEmpty();
    }

    [Fact]
    public void Score_FlagsStaleData_ButStillEvaluatesWhenOtherInputsPresent()
    {
        var snapshot = Snapshot(stale: true);

        var (score, reasons) = StructureScorer.Score(snapshot);

        score.Should().NotBeNull();
        reasons.Should().Contain("stale_structure_data");
    }
}
