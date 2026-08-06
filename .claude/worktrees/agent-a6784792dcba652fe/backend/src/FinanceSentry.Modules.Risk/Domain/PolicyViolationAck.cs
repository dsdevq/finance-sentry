namespace FinanceSentry.Modules.Risk.Domain;

/// <summary>Acknowledged pre-existing violation with a remediation note (FR-003).</summary>
public class PolicyViolationAck
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public string RuleKey { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public DateTimeOffset AcknowledgedAt { get; set; } = DateTimeOffset.UtcNow;

    public string RemediationNote { get; set; } = string.Empty;

    public decimal WorseningStepPct { get; set; }

    public decimal ObservedAtAck { get; set; }

    public bool IsActive { get; set; } = true;
}
