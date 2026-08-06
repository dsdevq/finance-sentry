namespace FinanceSentry.Modules.Research.Application.Services;

/// <summary>
/// Net-of-friction return parameters (FR-007b) — bound from <c>appsettings.json</c> section
/// <c>ThesisTrackRecord:Friction</c>. Defaults are placeholders, not tax/investment advice
/// (spec Assumptions) — Denys configures jurisdiction-specific values.
/// </summary>
public class FrictionConfig
{
    public const string SectionName = "ThesisTrackRecord:Friction";

    private const decimal DefaultPerTradeCostBps = 10m;
    private const decimal DefaultShortTermTaxRate = 0.37m;
    private const decimal DefaultLongTermTaxRate = 0.20m;
    private const int DefaultShortLongBoundaryDays = 365;

    /// <summary>Round-trip cost estimate in basis points, applied to gross return.</summary>
    public decimal PerTradeCostBps { get; set; } = DefaultPerTradeCostBps;

    /// <summary>Tax rate applied to gains held for less than <see cref="ShortLongBoundaryDays"/>.</summary>
    public decimal ShortTermTaxRate { get; set; } = DefaultShortTermTaxRate;

    /// <summary>Tax rate applied to gains held for at least <see cref="ShortLongBoundaryDays"/>.</summary>
    public decimal LongTermTaxRate { get; set; } = DefaultLongTermTaxRate;

    /// <summary>Holding-period boundary (days) between short-term and long-term tax treatment.</summary>
    public int ShortLongBoundaryDays { get; set; } = DefaultShortLongBoundaryDays;
}
