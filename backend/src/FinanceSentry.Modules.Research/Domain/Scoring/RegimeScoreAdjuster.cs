namespace FinanceSentry.Modules.Research.Domain.Scoring;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Research.Application.Services;
using FinanceSentry.Modules.Research.Domain.Opportunity;

/// <summary>
/// Pure, deterministic regime→structure-score adjustment (feature 021, US3). Regime is <b>context
/// only</b>: this haircuts the <i>presented/ranked</i> structure score of speculative candidates in
/// risk-off macro conditions, so a chase-machine can't rank a frothy momentum name top-of-list in a
/// crisis. It NEVER actions (no cash-raising, selling, or promotion block — FR-021), NEVER mutates
/// the raw score (which stays governed by <c>FormulaVersion</c>), and is fully explained + reversible
/// (set the config haircuts to 0 to disable).
///
/// Rules (magnitudes from <see cref="OpportunityOptions"/>): a volatility haircut in Stressed/Panic
/// scaled by crowding (Extended most penalized, Normal half, Early none), plus an additional
/// inversion haircut when the rates axis is Inverted. Result clamped to [0,100]. When no regime
/// reading is available, the adjusted score equals the raw score with a <c>no_regime_data</c>
/// rationale (FR-022).
/// </summary>
public static class RegimeScoreAdjuster
{
    public const string NoRegimeData = "no_regime_data";

    private const int MinScore = 0;
    private const int MaxScore = 100;

    private const string PanicRegime = "Panic";
    private const string StressedRegime = "Stressed";
    private const string InvertedRegime = "Inverted";

    // Non-Extended crowding takes a fraction of the Extended haircut (Normal = half; Early = none).
    private const int NormalCrowdingDivisor = 2;

    public static RegimeAdjustment Adjust(
        int? rawStructureScore,
        CrowdingClass crowding,
        MarketRegimeSnapshot? snapshot,
        OpportunityOptions options)
    {
        if (rawStructureScore is null || snapshot is null ||
            (!snapshot.VolatilityAvailable && !snapshot.RatesAvailable))
        {
            return new RegimeAdjustment(
                rawStructureScore, rawStructureScore, 0, [NoRegimeData],
                snapshot?.VolatilityRegime, snapshot?.RatesRegime,
                snapshot?.RecessionWarning ?? false, snapshot?.VixLevel, snapshot?.Spread,
                snapshot?.ComputedAt);
        }

        var reasons = new List<string>();
        var haircut = 0;

        if (snapshot.VolatilityAvailable)
        {
            var volHaircut = VolatilityHaircut(snapshot.VolatilityRegime, crowding, options);
            if (volHaircut > 0)
            {
                haircut += volHaircut;
                reasons.Add($"volatility:{snapshot.VolatilityRegime}");
                reasons.Add($"crowding:{crowding}");
                reasons.Add($"haircut:-{volHaircut}");
            }
        }

        if (snapshot.RatesAvailable && snapshot.RatesRegime == InvertedRegime)
        {
            var invHaircut = Scale(options.RegimeInvertedExtendedHaircut, crowding);
            if (invHaircut > 0)
            {
                haircut += invHaircut;
                reasons.Add("rates:Inverted");
                reasons.Add($"haircut:-{invHaircut}");
            }
        }

        if (reasons.Count == 0)
        {
            reasons.Add($"volatility:{snapshot.VolatilityRegime ?? "n/a"}");
            reasons.Add($"rates:{snapshot.RatesRegime ?? "n/a"}");
            reasons.Add("haircut:0");
        }

        var adjusted = Math.Clamp(rawStructureScore.Value - haircut, MinScore, MaxScore);
        return new RegimeAdjustment(
            rawStructureScore, adjusted, adjusted - rawStructureScore.Value, reasons,
            snapshot.VolatilityRegime, snapshot.RatesRegime, snapshot.RecessionWarning,
            snapshot.VixLevel, snapshot.Spread, snapshot.ComputedAt);
    }

    private static int VolatilityHaircut(string? regime, CrowdingClass crowding, OpportunityOptions options)
        => regime switch
        {
            PanicRegime => Scale(options.RegimePanicExtendedHaircut, crowding),
            StressedRegime => Scale(options.RegimeStressedExtendedHaircut, crowding),
            _ => 0,
        };

    // Extended = full haircut; Normal = half; Early = none (least speculative).
    private static int Scale(int extendedHaircut, CrowdingClass crowding) => crowding switch
    {
        CrowdingClass.Extended => extendedHaircut,
        CrowdingClass.Normal => extendedHaircut / NormalCrowdingDivisor,
        _ => 0,
    };
}

/// <summary>Outcome of the regime adjustment: raw + adjusted structure score, signed delta, rationale, and context.</summary>
public sealed record RegimeAdjustment(
    int? RawStructureScore,
    int? AdjustedStructureScore,
    int AdjustmentPoints,
    IReadOnlyList<string> Rationale,
    string? VolatilityRegime,
    string? RatesRegime,
    bool RecessionWarning,
    decimal? VixLevel,
    decimal? Spread,
    DateTimeOffset? AsOf);
