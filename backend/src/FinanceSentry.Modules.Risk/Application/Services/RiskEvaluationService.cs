namespace FinanceSentry.Modules.Risk.Application.Services;

using FinanceSentry.Modules.Risk.Domain;

/// <summary>
/// Pure evaluation logic (SC-001): no I/O, no clock reads beyond the injected `now`. Deterministic
/// facts only — no LLM, no composite/blended score (FR-008).
/// </summary>
public sealed class RiskEvaluationService : IRiskEvaluationService
{
    public ComplianceReport Evaluate(
        BookSnapshot book,
        RiskRuleSet? ruleSet,
        IReadOnlyList<PolicyViolationAck> acks,
        DateTimeOffset? now = null)
    {
        var generatedAt = now ?? DateTimeOffset.UtcNow;

        if (ruleSet is null)
        {
            return new ComplianceReport(generatedAt, book.IsStale, book.StaleSources, [], HasRuleSet: false);
        }

        var raw = ComputeRawViolations(book, ruleSet);
        var acked = ApplyAcks(raw, acks);

        return new ComplianceReport(generatedAt, book.IsStale, book.StaleSources, acked, HasRuleSet: true);
    }

    public RiskVerdict EvaluateProposal(
        BookSnapshot book,
        RiskRuleSet? ruleSet,
        string ticker,
        decimal proposedUsd,
        int turnoverCountThisQuarter)
    {
        if (ruleSet is null)
        {
            return new RiskVerdict(RiskDecision.Allowed, null, null, null, null, null, "No rules on file — nothing to check.");
        }

        if (ruleSet.TurnoverBudgetPerQuarter is { } turnoverBudget && turnoverCountThisQuarter >= turnoverBudget)
        {
            return new RiskVerdict(
                RiskDecision.Refused,
                RiskRuleKeys.Turnover,
                turnoverCountThisQuarter,
                turnoverBudget,
                0m,
                0m,
                "Discretionary turnover budget for this quarter is already reached.");
        }

        var existingUsd = book.Positions
            .Where(p => string.Equals(p.Symbol, ticker, StringComparison.OrdinalIgnoreCase))
            .Sum(p => p.UsdValue);
        var isNewPosition = existingUsd <= 0m;
        var projectedTotal = book.TotalUsd + proposedUsd;

        if (ruleSet.MaxPositionWeightPct is { } maxWeight && maxWeight is > 0 and <= 1)
        {
            var projectedWeight = projectedTotal > 0 ? (existingUsd + proposedUsd) / projectedTotal : 0m;
            if (projectedWeight > maxWeight)
            {
                var maxCompliantSize = MaxCompliantSize(book.TotalUsd, existingUsd, maxWeight);
                return new RiskVerdict(
                    RiskDecision.Refused,
                    RiskRuleKeys.MaxPositionWeight,
                    projectedWeight,
                    maxWeight,
                    maxCompliantSize,
                    Math.Max(0m, maxCompliantSize - proposedUsd));
            }
        }

        if (isNewPosition && ruleSet.MaxNewPositionPct is { } maxNew && maxNew is > 0 and <= 1)
        {
            var projectedWeight = projectedTotal > 0 ? proposedUsd / projectedTotal : 0m;
            if (projectedWeight > maxNew)
            {
                var maxCompliantSize = MaxCompliantSize(book.TotalUsd, 0m, maxNew);
                return new RiskVerdict(
                    RiskDecision.Refused,
                    RiskRuleKeys.MaxNewPosition,
                    projectedWeight,
                    maxNew,
                    maxCompliantSize,
                    Math.Max(0m, maxCompliantSize - proposedUsd));
            }
        }

        if (ruleSet.MinCashBufferPct is { } minCash and > 0)
        {
            var projectedCash = book.CashUsd - proposedUsd;
            var projectedCashPct = book.TotalUsd > 0 ? projectedCash / book.TotalUsd : 0m;
            if (projectedCashPct < minCash)
            {
                var headroom = Math.Max(0m, book.CashUsd - (minCash * book.TotalUsd));
                return new RiskVerdict(
                    RiskDecision.Refused,
                    RiskRuleKeys.MinCashBuffer,
                    projectedCashPct,
                    minCash,
                    headroom,
                    headroom);
            }
        }

        var headroomUsd = ruleSet.MaxPositionWeightPct is { } cap
            ? Math.Max(0m, MaxCompliantSize(book.TotalUsd, existingUsd, cap) - proposedUsd)
            : (decimal?)null;

        return new RiskVerdict(RiskDecision.Allowed, null, null, null, null, headroomUsd);
    }

    /// <summary>Max additional USD that can be added to a position without breaching `cap` weight.</summary>
    private static decimal MaxCompliantSize(decimal totalUsd, decimal existingUsd, decimal cap)
    {
        if (cap >= 1m)
        {
            return decimal.MaxValue;
        }

        var maxAdd = ((cap * totalUsd) - existingUsd) / (1m - cap);
        return Math.Max(0m, maxAdd);
    }

    private static List<PolicyViolation> ComputeRawViolations(BookSnapshot book, RiskRuleSet ruleSet)
    {
        var violations = new List<PolicyViolation>();

        if (ruleSet.MaxPositionWeightPct is { } maxWeight)
        {
            foreach (var p in book.Positions)
            {
                if (p.WeightPct > maxWeight)
                {
                    var limitUsd = maxWeight * book.TotalUsd;
                    violations.Add(new PolicyViolation(
                        RiskRuleKeys.MaxPositionWeight,
                        p.Symbol,
                        p.WeightPct,
                        maxWeight,
                        Math.Max(0m, p.UsdValue - limitUsd),
                        p.WeightPct - maxWeight,
                        PolicyViolationStatus.New));
                }
            }
        }

        if (ruleSet.MaxSleeveWeightPct is { } maxSleeve)
        {
            foreach (var group in book.Positions.GroupBy(p => p.Sleeve))
            {
                var sleeveWeight = group.Sum(p => p.WeightPct);
                if (sleeveWeight > maxSleeve)
                {
                    var sleeveUsd = group.Sum(p => p.UsdValue);
                    var limitUsd = maxSleeve * book.TotalUsd;
                    violations.Add(new PolicyViolation(
                        RiskRuleKeys.MaxSleeveWeight,
                        group.Key,
                        sleeveWeight,
                        maxSleeve,
                        Math.Max(0m, sleeveUsd - limitUsd),
                        sleeveWeight - maxSleeve,
                        PolicyViolationStatus.New));
                }
            }
        }

        if (ruleSet.MinCashBufferPct is { } minCash && book.TotalUsd > 0)
        {
            var cashPct = book.CashUsd / book.TotalUsd;
            if (cashPct < minCash)
            {
                var shortfallUsd = (minCash * book.TotalUsd) - book.CashUsd;
                violations.Add(new PolicyViolation(
                    RiskRuleKeys.MinCashBuffer,
                    "CASH",
                    cashPct,
                    minCash,
                    Math.Max(0m, shortfallUsd),
                    minCash - cashPct,
                    PolicyViolationStatus.New));
            }
        }

        return violations;
    }

    private static List<PolicyViolation> ApplyAcks(
        IReadOnlyList<PolicyViolation> raw, IReadOnlyList<PolicyViolationAck> acks)
    {
        var result = new List<PolicyViolation>(raw.Count);

        foreach (var violation in raw)
        {
            var ack = acks.SingleOrDefault(a =>
                a.RuleKey == violation.RuleKey &&
                string.Equals(a.Subject, violation.Subject, StringComparison.OrdinalIgnoreCase));

            if (ack is null)
            {
                result.Add(violation);
                continue;
            }

            var worsenedPastStep = violation.ObservedValue - ack.ObservedAtAck > ack.WorseningStepPct;
            result.Add(violation with
            {
                Status = worsenedPastStep ? PolicyViolationStatus.Worsened : PolicyViolationStatus.Acknowledged,
                RemediationNote = ack.RemediationNote,
            });
        }

        return result;
    }
}
