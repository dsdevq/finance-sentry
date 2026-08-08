using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Research.Application.Services;
using FinanceSentry.Modules.Research.Domain.Opportunity;
using FinanceSentry.Modules.Research.Domain.Scoring;
using FluentAssertions;
using Xunit;

namespace FinanceSentry.Modules.Research.Tests.Regime;

public sealed class RegimeScoreAdjusterTests
{
    private static readonly OpportunityOptions Options = new();

    private static MarketRegimeSnapshot Snapshot(
        string? volatility = "Normal",
        string? rates = "Normal",
        bool volAvailable = true,
        bool ratesAvailable = true)
        => new(
            DateTimeOffset.UtcNow,
            volAvailable, volatility, 20m, "Flat",
            ratesAvailable, rates, 0.6m, rates == "Inverted", "mid-cycle, balanced",
            null, null);

    [Fact]
    public void NoSnapshot_PassesThrough_WithNoRegimeData()
    {
        var result = RegimeScoreAdjuster.Adjust(80, CrowdingClass.Extended, null, Options);
        result.RawStructureScore.Should().Be(80);
        result.AdjustedStructureScore.Should().Be(80);
        result.AdjustmentPoints.Should().Be(0);
        result.Rationale.Should().ContainSingle().Which.Should().Be(RegimeScoreAdjuster.NoRegimeData);
    }

    [Fact]
    public void NullRawScore_PassesThrough_WithNoRegimeData()
    {
        var result = RegimeScoreAdjuster.Adjust(null, CrowdingClass.Extended, Snapshot("Panic"), Options);
        result.AdjustedStructureScore.Should().BeNull();
        result.Rationale.Should().Contain(RegimeScoreAdjuster.NoRegimeData);
    }

    [Fact]
    public void BothAxesUnavailable_PassesThrough_WithNoRegimeData()
    {
        var snap = Snapshot(volAvailable: false, ratesAvailable: false);
        var result = RegimeScoreAdjuster.Adjust(80, CrowdingClass.Extended, snap, Options);
        result.AdjustedStructureScore.Should().Be(80);
        result.Rationale.Should().Contain(RegimeScoreAdjuster.NoRegimeData);
    }

    [Fact]
    public void Panic_Extended_AppliesFullVolatilityHaircut()
    {
        var result = RegimeScoreAdjuster.Adjust(80, CrowdingClass.Extended, Snapshot("Panic"), Options);
        result.AdjustedStructureScore.Should().Be(80 - Options.RegimePanicExtendedHaircut);
        result.RawStructureScore.Should().Be(80); // raw never mutated
        result.Rationale.Should().Contain("volatility:Panic");
        result.Rationale.Should().Contain("crowding:Extended");
    }

    [Fact]
    public void Panic_Early_AppliesNoHaircut()
    {
        var result = RegimeScoreAdjuster.Adjust(80, CrowdingClass.Early, Snapshot("Panic"), Options);
        result.AdjustedStructureScore.Should().Be(80);
        result.AdjustmentPoints.Should().Be(0);
    }

    [Fact]
    public void Panic_Normal_AppliesHalfHaircut()
    {
        var result = RegimeScoreAdjuster.Adjust(80, CrowdingClass.Normal, Snapshot("Panic"), Options);
        result.AdjustedStructureScore.Should().Be(80 - (Options.RegimePanicExtendedHaircut / 2));
    }

    [Fact]
    public void Stressed_Extended_AppliesStressedHaircut()
    {
        var result = RegimeScoreAdjuster.Adjust(70, CrowdingClass.Extended, Snapshot("Stressed"), Options);
        result.AdjustedStructureScore.Should().Be(70 - Options.RegimeStressedExtendedHaircut);
    }

    [Fact]
    public void Calm_Steep_LeavesScoreUnchanged()
    {
        var result = RegimeScoreAdjuster.Adjust(90, CrowdingClass.Extended, Snapshot("Calm", "Steep"), Options);
        result.AdjustedStructureScore.Should().Be(90);
        result.AdjustmentPoints.Should().Be(0);
    }

    [Fact]
    public void Inverted_StacksAdditionalHaircut_OnTopOfVolatility()
    {
        var result = RegimeScoreAdjuster.Adjust(90, CrowdingClass.Extended, Snapshot("Panic", "Inverted"), Options);
        var expected = 90 - Options.RegimePanicExtendedHaircut - Options.RegimeInvertedExtendedHaircut;
        result.AdjustedStructureScore.Should().Be(expected);
        result.Rationale.Should().Contain("rates:Inverted");
        result.RecessionWarning.Should().BeTrue();
    }

    [Fact]
    public void Inverted_Only_AppliesInversionHaircut_WhenVolatilityCalm()
    {
        var result = RegimeScoreAdjuster.Adjust(90, CrowdingClass.Extended, Snapshot("Calm", "Inverted"), Options);
        result.AdjustedStructureScore.Should().Be(90 - Options.RegimeInvertedExtendedHaircut);
    }

    [Fact]
    public void Result_IsClampedToZero()
    {
        var result = RegimeScoreAdjuster.Adjust(5, CrowdingClass.Extended, Snapshot("Panic", "Inverted"), Options);
        result.AdjustedStructureScore.Should().Be(0);
    }

    [Fact]
    public void SwitchingRegime_LowersExtendedButNotEarly_RawScoreUnchanged()
    {
        const int raw = 85;
        var calm = RegimeScoreAdjuster.Adjust(raw, CrowdingClass.Extended, Snapshot("Calm", "Steep"), Options);
        var panic = RegimeScoreAdjuster.Adjust(raw, CrowdingClass.Extended, Snapshot("Panic", "Inverted"), Options);
        var panicEarly = RegimeScoreAdjuster.Adjust(raw, CrowdingClass.Early, Snapshot("Panic", "Inverted"), Options);

        panic.AdjustedStructureScore.Should().BeLessThan(calm.AdjustedStructureScore!.Value);
        panicEarly.AdjustedStructureScore.Should().Be(raw); // non-speculative name unaffected
        calm.RawStructureScore.Should().Be(raw);
        panic.RawStructureScore.Should().Be(raw);
    }
}
