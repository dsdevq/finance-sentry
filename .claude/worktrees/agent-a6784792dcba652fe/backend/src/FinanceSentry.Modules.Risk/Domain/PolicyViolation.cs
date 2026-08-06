namespace FinanceSentry.Modules.Risk.Domain;

public enum PolicyViolationStatus
{
    New,
    Acknowledged,
    Worsened,
}

/// <summary>A deterministic fact: a rule was breached by an observed value.</summary>
public sealed record PolicyViolation(
    string RuleKey,
    string Subject,
    decimal ObservedValue,
    decimal LimitValue,
    decimal ExcessUsd,
    decimal ExcessPct,
    PolicyViolationStatus Status,
    string? RemediationNote = null);

public static class RiskRuleKeys
{
    public const string MaxPositionWeight = "MaxPositionWeight";
    public const string MaxSleeveWeight = "MaxSleeveWeight";
    public const string MinCashBuffer = "MinCashBuffer";
    public const string MaxNewPosition = "MaxNewPosition";
    public const string Turnover = "Turnover";
    public const string AllocationDrift = "AllocationDrift";
    public const string AddToBrokenThesis = "AddToBrokenThesis";
}
