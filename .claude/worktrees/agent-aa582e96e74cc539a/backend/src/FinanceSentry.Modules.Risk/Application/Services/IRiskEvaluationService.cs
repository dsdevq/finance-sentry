namespace FinanceSentry.Modules.Risk.Application.Services;

using FinanceSentry.Modules.Risk.Domain;

/// <summary>Pure function: (BookSnapshot, RiskRuleSet, ack state) -&gt; ComplianceReport / RiskVerdict.</summary>
public interface IRiskEvaluationService
{
    ComplianceReport Evaluate(
        BookSnapshot book,
        RiskRuleSet? ruleSet,
        IReadOnlyList<PolicyViolationAck> acks,
        DateTimeOffset? now = null);

    RiskVerdict EvaluateProposal(
        BookSnapshot book,
        RiskRuleSet? ruleSet,
        string ticker,
        decimal proposedUsd,
        int turnoverCountThisQuarter);
}
