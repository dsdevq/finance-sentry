namespace FinanceSentry.Modules.Research.Application.Services;

/// <summary>
/// All Opportunity Scanner thresholds bound from configuration (section <c>Opportunity</c>).
/// No magic numbers in the scorers or lifecycle handlers (FR-008 parity with Radar).
/// </summary>
public sealed class OpportunityOptions
{
    public const string SectionName = "Opportunity";

    /// <summary>Extension-from-MA50 (fraction) at/above this classifies crowding as Extended.</summary>
    public decimal ExtendedExtensionThreshold { get; set; } = 0.20m;

    /// <summary>Extension-from-MA50 (fraction) at/below this classifies crowding as Early.</summary>
    public decimal EarlyExtensionThreshold { get; set; } = 0.05m;

    /// <summary>Volume ratio at/above this, combined with Extended-range extension, confirms Extended crowding.</summary>
    public decimal ExtendedVolumeRatioThreshold { get; set; } = 1.5m;

    /// <summary>Structure/Fundamentals score at/above this bar triggers a top-tier ("notable") signal + Alert.</summary>
    public int TopTierScoreBar { get; set; } = 80;

    /// <summary>Days after creation an Active candidate auto-expires if never promoted/rejected.</summary>
    public int CandidateTtlDays { get; set; } = 30;

    /// <summary>Default drawdown fraction used to prefill the price_drawdown invalidation trigger.</summary>
    public decimal DefaultDrawdownPrefill { get; set; } = 0.30m;

    /// <summary>Consecutive trading days the default drawdown trigger requires.</summary>
    public int DefaultDrawdownConsecutiveDays { get; set; } = 3;

    /// <summary>Buffer subtracted from the latest gross margin when prefilling a gross_margin trigger.</summary>
    public decimal GrossMarginPrefillBuffer { get; set; } = 0.10m;

    /// <summary>Bumped whenever the scoring normalization rules change, so old scorecards stay honest (FR-002).</summary>
    public int FormulaVersion { get; set; } = 1;
}
