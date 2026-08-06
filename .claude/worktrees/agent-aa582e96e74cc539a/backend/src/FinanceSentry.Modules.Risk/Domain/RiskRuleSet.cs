namespace FinanceSentry.Modules.Risk.Domain;

/// <summary>Versioned per-user risk rule set (FR-001). A new save appends a row and flips IsCurrent.</summary>
public class RiskRuleSet
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public int Version { get; set; }

    public bool IsCurrent { get; set; }

    public decimal? MaxPositionWeightPct { get; set; }

    public decimal? MaxSleeveWeightPct { get; set; }

    public decimal? MinCashBufferPct { get; set; }

    public decimal? MaxLossPerThesisPct { get; set; }

    public decimal? MaxNewPositionPct { get; set; }

    public int? TurnoverBudgetPerQuarter { get; set; }

    public List<AllocationTargetEntry> AllocationTargets { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed record AllocationTargetEntry(string AssetClass, decimal TargetPct, decimal DriftBandPct);
