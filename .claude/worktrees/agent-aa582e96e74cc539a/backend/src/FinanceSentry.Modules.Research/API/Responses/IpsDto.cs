namespace FinanceSentry.Modules.Research.API.Responses;

using FinanceSentry.Modules.Research.Domain;

public record IpsDto(
    Guid Id,
    int Version,
    bool IsCurrent,
    IReadOnlyList<InvestmentGoal> Goals,
    int PrimaryHorizonYears,
    decimal? EmergencyCushionUsd,
    int RiskTolerance,
    int? RiskCapacity,
    decimal? MaxDrawdownTolerancePct,
    IReadOnlyList<AllocationTarget> AllocationTargets,
    RebalancingRule RebalancingRule,
    ContributionPlan? ContributionPlan,
    string? SellDiscipline,
    int CoolingOffDays,
    IReadOnlyList<string> Exclusions,
    decimal? MaxSinglePositionPct,
    string ReviewCadence,
    DateTimeOffset? LastReviewedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
