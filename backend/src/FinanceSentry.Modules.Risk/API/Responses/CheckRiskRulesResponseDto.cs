namespace FinanceSentry.Modules.Risk.API.Responses;

using FinanceSentry.Modules.Risk.Domain;

/// <summary>MCP-facing shape for `check_risk_rules` — either a compliance report or a proposal verdict.</summary>
public sealed record CheckRiskRulesResponseDto(
    DateTimeOffset GeneratedAt,
    bool HasRuleSet,
    bool IsStale,
    IReadOnlyList<string> StaleSources,
    IReadOnlyList<PolicyViolation>? Violations,
    RiskDecision? Decision,
    string? RuleKey,
    decimal? ObservedValue,
    decimal? LimitValue,
    decimal? MaxCompliantSizeUsd,
    decimal? HeadroomUsd,
    string? Note)
{
    public static CheckRiskRulesResponseDto FromReport(ComplianceReport report) => new(
        report.GeneratedAt, report.HasRuleSet, report.IsStale, report.StaleSources, report.Violations,
        null, null, null, null, null, null, null);

    public static CheckRiskRulesResponseDto FromVerdict(RiskVerdict verdict, bool hasRuleSet) => new(
        DateTimeOffset.UtcNow, hasRuleSet, false, [], null,
        verdict.Decision, verdict.RuleKey, verdict.ObservedValue, verdict.LimitValue,
        verdict.MaxCompliantSizeUsd, verdict.HeadroomUsd, verdict.Note);
}
