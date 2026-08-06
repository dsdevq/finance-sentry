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

    /// <summary>Sector rotation ranks 1..N qualify as "top rotating" for scan rule (a) (FR-008a).</summary>
    public int ScanTopRotatingSectors { get; set; } = 2;

    /// <summary>Universe RS percentile at/above this is "top-quartile" for scan rule (a).</summary>
    public int ScanTopQuartileRsPercentile { get; set; } = 75;

    /// <summary>Universe RS percentile at/above this is "top-decile" for scan rule (b) (FR-008b).</summary>
    public int ScanTopDecileRsPercentile { get; set; } = 90;

    /// <summary>Volume ratio at/above this counts as above-average for the breakout rule (c) (FR-008c).</summary>
    public decimal ScanBreakoutVolumeRatioMin { get; set; } = 1.2m;

    /// <summary>Most nominations a single scan run may score; excess is logged and dropped (alert-flood guard).</summary>
    public int ScanMaxNominationsPerRun { get; set; } = 5;

    /// <summary>Hour (UTC) the daily opportunity scan runs — after Radar's 23:00 UTC compute job has refreshed structure.</summary>
    public int ScanHourUtc { get; set; } = 0;
}
