namespace FinanceSentry.Modules.Risk.Domain;

public enum RiskDecision
{
    Allowed,
    Refused,
}

/// <summary>Result of a promotion-time proposal check (FR-004/FR-007).</summary>
public sealed record RiskVerdict(
    RiskDecision Decision,
    string? RuleKey,
    decimal? ObservedValue,
    decimal? LimitValue,
    decimal? MaxCompliantSizeUsd,
    decimal? HeadroomUsd,
    string? Note = null);

/// <summary>The `check_risk_rules()` no-arg response — current compliance state of the book.</summary>
public sealed record ComplianceReport(
    DateTimeOffset GeneratedAt,
    bool IsStale,
    IReadOnlyList<string> StaleSources,
    IReadOnlyList<PolicyViolation> Violations,
    bool HasRuleSet);
