using FinanceSentry.Modules.Risk.Application.Services;
using FinanceSentry.Modules.Risk.Domain;
using FinanceSentry.Modules.Risk.Domain.Ports;
using FluentAssertions;
using Xunit;

namespace FinanceSentry.Modules.Risk.Tests;

public sealed class RiskEvaluationServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private readonly RiskEvaluationService _service = new();

    [Fact]
    public void Evaluate_NoRuleSet_ReturnsNoRulesOnFile_NoInferredViolations()
    {
        var book = new BookSnapshot(15000m, 0m, [new BookPosition("DRAM", RiskSleeve.Brokerage, 100m, 6900m, 0.46m)], false, []);

        var report = _service.Evaluate(book, null, [], []);

        report.HasRuleSet.Should().BeFalse();
        report.Violations.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_SeededDram46PercentVs25PercentCap_ProducesExactlyOneViolation()
    {
        // The real seeded case: ~$15k book, one position (DRAM) at ~46%, cap at 25%.
        var book = new BookSnapshot(15000m, 1000m, [new BookPosition("DRAM", RiskSleeve.Brokerage, 100m, 6900m, 0.46m)], false, []);
        var ruleSet = new RiskRuleSet { UserId = UserId, MaxPositionWeightPct = 0.25m };

        var report = _service.Evaluate(book, ruleSet, [], []);

        report.Violations.Should().ContainSingle();
        var violation = report.Violations.Single();
        violation.RuleKey.Should().Be(RiskRuleKeys.MaxPositionWeight);
        violation.Subject.Should().Be("DRAM");
        violation.ObservedValue.Should().Be(0.46m);
        violation.LimitValue.Should().Be(0.25m);
        violation.ExcessUsd.Should().BeGreaterThan(0m);
        violation.Status.Should().Be(PolicyViolationStatus.New);
    }

    [Fact]
    public void Evaluate_CompliantBook_ReturnsEmptyViolations()
    {
        var book = new BookSnapshot(10000m, 3000m, [new BookPosition("AAPL", RiskSleeve.Brokerage, 10m, 2000m, 0.2m)], false, []);
        var ruleSet = new RiskRuleSet { UserId = UserId, MaxPositionWeightPct = 0.25m };

        var report = _service.Evaluate(book, ruleSet, [], []);

        report.Violations.Should().BeEmpty();
        report.HasRuleSet.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_MaxSleeveWeightBreach_IsFlagged()
    {
        var book = new BookSnapshot(10000m,
            0m,
            [
                new BookPosition("NVDA", RiskSleeve.Brokerage, 1m, 4000m, 0.4m),
                new BookPosition("AAPL", RiskSleeve.Brokerage, 1m, 4000m, 0.4m),
            ],
            false, []);
        var ruleSet = new RiskRuleSet { UserId = UserId, MaxSleeveWeightPct = 0.5m };

        var report = _service.Evaluate(book, ruleSet, [], []);

        report.Violations.Should().ContainSingle(v => v.RuleKey == RiskRuleKeys.MaxSleeveWeight && v.Subject == RiskSleeve.Brokerage);
    }

    [Fact]
    public void Evaluate_MinCashBufferBreach_IsFlagged()
    {
        var book = new BookSnapshot(10000m, 200m, [new BookPosition("AAPL", RiskSleeve.Brokerage, 1m, 9800m, 0.98m)], false, []);
        var ruleSet = new RiskRuleSet { UserId = UserId, MinCashBufferPct = 0.1m };

        var report = _service.Evaluate(book, ruleSet, [], []);

        report.Violations.Should().ContainSingle(v => v.RuleKey == RiskRuleKeys.MinCashBuffer && v.Subject == "CASH");
    }

    [Fact]
    public void Evaluate_AcknowledgedViolation_ReportsAcknowledged_NotNew()
    {
        var book = new BookSnapshot(15000m, 1000m, [new BookPosition("DRAM", RiskSleeve.Brokerage, 100m, 6900m, 0.46m)], false, []);
        var ruleSet = new RiskRuleSet { UserId = UserId, MaxPositionWeightPct = 0.25m };
        var ack = new PolicyViolationAck
        {
            UserId = UserId,
            RuleKey = RiskRuleKeys.MaxPositionWeight,
            Subject = "DRAM",
            RemediationNote = "trim DRAM on strength to <=30% by Q4",
            ObservedAtAck = 0.46m,
            WorseningStepPct = 0.05m,
        };

        var report = _service.Evaluate(book, ruleSet, [], [ack]);

        var violation = report.Violations.Should().ContainSingle().Subject;
        violation.Status.Should().Be(PolicyViolationStatus.Acknowledged);
        violation.RemediationNote.Should().Be(ack.RemediationNote);
    }

    [Fact]
    public void Evaluate_AcknowledgedViolation_WorsensPastStep_ReopensAsWorsened()
    {
        var book = new BookSnapshot(15000m, 1000m, [new BookPosition("DRAM", RiskSleeve.Brokerage, 100m, 8000m, 0.55m)], false, []);
        var ruleSet = new RiskRuleSet { UserId = UserId, MaxPositionWeightPct = 0.25m };
        var ack = new PolicyViolationAck
        {
            UserId = UserId,
            RuleKey = RiskRuleKeys.MaxPositionWeight,
            Subject = "DRAM",
            RemediationNote = "trim DRAM on strength to <=30% by Q4",
            ObservedAtAck = 0.46m,
            WorseningStepPct = 0.05m,
        };

        var report = _service.Evaluate(book, ruleSet, [], [ack]);

        report.Violations.Should().ContainSingle().Which.Status.Should().Be(PolicyViolationStatus.Worsened);
    }

    [Fact]
    public void Evaluate_StaleBook_FlagsReportStale_ButDoesNotAutoClearViolations()
    {
        var book = new BookSnapshot(15000m, 1000m, [new BookPosition("DRAM", RiskSleeve.Brokerage, 100m, 6900m, 0.46m)], true, ["brokerage"]);
        var ruleSet = new RiskRuleSet { UserId = UserId, MaxPositionWeightPct = 0.25m };

        var report = _service.Evaluate(book, ruleSet, [], []);

        report.IsStale.Should().BeTrue();
        report.Violations.Should().ContainSingle();
    }

    // 039 (US2/SC-002): allocation-drift verdicts are now produced from the target allocation passed
    // in from its single home (the IPS), not a copy on the rule set. The comparison and emitted
    // violation fields are unchanged — a sleeve past its drift band flags exactly as before.
    [Fact]
    public void Evaluate_SleeveDriftPastBand_FlagsAllocationDrift_FromInjectedTargets()
    {
        var book = new BookSnapshot(
            10000m, 0m,
            [new BookPosition("DRAM", RiskSleeve.Brokerage, 100m, 4600m, 0.46m)],
            false, []);
        var ruleSet = new RiskRuleSet { UserId = UserId, MaxPositionWeightPct = 0.50m };
        AllocationDriftTarget[] targets = [new(RiskSleeve.Brokerage, 0.30m, 0.05m)];

        var report = _service.Evaluate(book, ruleSet, targets, []);

        var drift = report.Violations.Should()
            .ContainSingle(v => v.RuleKey == RiskRuleKeys.AllocationDrift).Subject;
        drift.Subject.Should().Be(RiskSleeve.Brokerage);
        drift.ObservedValue.Should().Be(0.46m);
        drift.LimitValue.Should().Be(0.30m);
        drift.ExcessUsd.Should().BeApproximately(0.16m * 10000m, 0.01m);
    }

    [Fact]
    public void Evaluate_SleeveWithinBand_NoAllocationDrift_FromInjectedTargets()
    {
        var book = new BookSnapshot(
            10000m, 0m,
            [new BookPosition("DRAM", RiskSleeve.Brokerage, 100m, 3200m, 0.32m)],
            false, []);
        var ruleSet = new RiskRuleSet { UserId = UserId, MaxPositionWeightPct = 0.50m };
        AllocationDriftTarget[] targets = [new(RiskSleeve.Brokerage, 0.30m, 0.05m)];

        var report = _service.Evaluate(book, ruleSet, targets, []);

        report.Violations.Should().NotContain(v => v.RuleKey == RiskRuleKeys.AllocationDrift);
    }

    [Fact]
    public void Evaluate_NoInjectedTargets_EmitsNoAllocationDrift()
    {
        var book = new BookSnapshot(
            10000m, 0m,
            [new BookPosition("DRAM", RiskSleeve.Brokerage, 100m, 4600m, 0.46m)],
            false, []);
        var ruleSet = new RiskRuleSet { UserId = UserId, MaxPositionWeightPct = 0.50m };

        var report = _service.Evaluate(book, ruleSet, [], []);

        report.Violations.Should().NotContain(v => v.RuleKey == RiskRuleKeys.AllocationDrift);
    }

    [Fact]
    public void EvaluateProposal_NoRuleSet_ReturnsAllowed()
    {
        var book = new BookSnapshot(10000m, 5000m, [], false, []);

        var verdict = _service.EvaluateProposal(book, null, "NVDA", 1000m, 0);

        verdict.Decision.Should().Be(RiskDecision.Allowed);
    }

    [Fact]
    public void EvaluateProposal_WithinLimits_ReturnsAllowedWithHeadroom()
    {
        var book = new BookSnapshot(10000m, 5000m, [], false, []);
        var ruleSet = new RiskRuleSet { UserId = UserId, MaxPositionWeightPct = 0.25m };

        var verdict = _service.EvaluateProposal(book, ruleSet, "NVDA", 500m, 0);

        verdict.Decision.Should().Be(RiskDecision.Allowed);
        verdict.HeadroomUsd.Should().NotBeNull();
    }

    [Fact]
    public void EvaluateProposal_BreachesMaxPositionWeight_ReturnsRefusedWithMaxCompliantSize()
    {
        var book = new BookSnapshot(10000m, 5000m, [], false, []);
        var ruleSet = new RiskRuleSet { UserId = UserId, MaxPositionWeightPct = 0.25m };

        var verdict = _service.EvaluateProposal(book, ruleSet, "NVDA", 5000m, 0);

        verdict.Decision.Should().Be(RiskDecision.Refused);
        verdict.RuleKey.Should().Be(RiskRuleKeys.MaxPositionWeight);
        verdict.MaxCompliantSizeUsd.Should().NotBeNull();
        verdict.MaxCompliantSizeUsd!.Value.Should().BeLessThan(5000m);
    }

    [Fact]
    public void EvaluateProposal_BreachesMinCashBuffer_ReturnsRefused()
    {
        var book = new BookSnapshot(10000m, 1500m, [], false, []);
        var ruleSet = new RiskRuleSet { UserId = UserId, MinCashBufferPct = 0.1m };

        var verdict = _service.EvaluateProposal(book, ruleSet, "NVDA", 1000m, 0);

        verdict.Decision.Should().Be(RiskDecision.Refused);
        verdict.RuleKey.Should().Be(RiskRuleKeys.MinCashBuffer);
    }

    [Fact]
    public void EvaluateProposal_BreachesMaxNewPosition_ReturnsRefused()
    {
        var book = new BookSnapshot(10000m, 5000m, [], false, []);
        var ruleSet = new RiskRuleSet { UserId = UserId, MaxNewPositionPct = 0.1m };

        var verdict = _service.EvaluateProposal(book, ruleSet, "NVDA", 5000m, 0);

        verdict.Decision.Should().Be(RiskDecision.Refused);
        verdict.RuleKey.Should().Be(RiskRuleKeys.MaxNewPosition);
    }

    [Fact]
    public void EvaluateProposal_TurnoverBudgetAtCap_ReturnsRefusedTurnover()
    {
        var book = new BookSnapshot(10000m, 5000m, [], false, []);
        var ruleSet = new RiskRuleSet { UserId = UserId, TurnoverBudgetPerQuarter = 5 };

        var verdict = _service.EvaluateProposal(book, ruleSet, "NVDA", 100m, turnoverCountThisQuarter: 5);

        verdict.Decision.Should().Be(RiskDecision.Refused);
        verdict.RuleKey.Should().Be(RiskRuleKeys.Turnover);
    }

    // Regression: MinCashBuffer worsening direction was inverted — cash dropping further below the
    // minimum was silently treated as Acknowledged instead of Worsened (direction bug #417).

    [Fact]
    public void Evaluate_MinCashBuffer_CashDropsFurtherBelowMinimum_ReopensAsWorsened()
    {
        // Acked at 4% cash; cash has since dropped to 2% — clearly worse, should reopen.
        var book = new BookSnapshot(10000m, 200m,
            [new BookPosition("AAPL", RiskSleeve.Brokerage, 1m, 9800m, 0.98m)], false, []);
        var ruleSet = new RiskRuleSet { UserId = UserId, MinCashBufferPct = 0.05m };
        var ack = new PolicyViolationAck
        {
            UserId = UserId,
            RuleKey = RiskRuleKeys.MinCashBuffer,
            Subject = "CASH",
            ObservedAtAck = 0.04m,     // acked when cash was 4% (bad, but less bad)
            WorseningStepPct = 0.01m,  // reopen if cash drops ≥1 pp further
        };

        var report = _service.Evaluate(book, ruleSet, [], [ack]);

        // cashPct now = 200/10000 = 2%; delta = 4% − 2% = 2% > 1% step → Worsened
        report.Violations.Should().ContainSingle().Which.Status.Should().Be(PolicyViolationStatus.Worsened);
    }

    [Fact]
    public void Evaluate_MinCashBuffer_CashImprovesButStillViolating_RemainsAcknowledged()
    {
        // Acked at 3% cash; cash has improved to 4% — still below the 5% minimum but not worse.
        var book = new BookSnapshot(10000m, 400m,
            [new BookPosition("AAPL", RiskSleeve.Brokerage, 1m, 9600m, 0.96m)], false, []);
        var ruleSet = new RiskRuleSet { UserId = UserId, MinCashBufferPct = 0.05m };
        var ack = new PolicyViolationAck
        {
            UserId = UserId,
            RuleKey = RiskRuleKeys.MinCashBuffer,
            Subject = "CASH",
            ObservedAtAck = 0.03m,     // acked when cash was 3%
            WorseningStepPct = 0.01m,
        };

        var report = _service.Evaluate(book, ruleSet, [], [ack]);

        // cashPct now = 400/10000 = 4%; delta = 3% − 4% = -1% (improving) → Acknowledged
        report.Violations.Should().ContainSingle().Which.Status.Should().Be(PolicyViolationStatus.Acknowledged);
    }

    [Fact]
    public void Evaluate_MinCashBuffer_SustainedViolationWithinStep_RemainsAcknowledged()
    {
        // Acked at 3% cash; still at 3% (no change) — violation is sustained but not worsened.
        var book = new BookSnapshot(10000m, 300m,
            [new BookPosition("AAPL", RiskSleeve.Brokerage, 1m, 9700m, 0.97m)], false, []);
        var ruleSet = new RiskRuleSet { UserId = UserId, MinCashBufferPct = 0.05m };
        var ack = new PolicyViolationAck
        {
            UserId = UserId,
            RuleKey = RiskRuleKeys.MinCashBuffer,
            Subject = "CASH",
            ObservedAtAck = 0.03m,
            WorseningStepPct = 0.01m,
        };

        var report = _service.Evaluate(book, ruleSet, [], [ack]);

        // cashPct now = 300/10000 = 3%; delta = 3% − 3% = 0% ≤ 1% step → Acknowledged (sustained)
        report.Violations.Should().ContainSingle().Which.Status.Should().Be(PolicyViolationStatus.Acknowledged);
    }
}
