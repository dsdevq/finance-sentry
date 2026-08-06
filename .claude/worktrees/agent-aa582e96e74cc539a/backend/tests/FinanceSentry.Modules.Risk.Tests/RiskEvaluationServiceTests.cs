using FinanceSentry.Modules.Risk.Application.Services;
using FinanceSentry.Modules.Risk.Domain;
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

        var report = _service.Evaluate(book, null, []);

        report.HasRuleSet.Should().BeFalse();
        report.Violations.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_SeededDram46PercentVs25PercentCap_ProducesExactlyOneViolation()
    {
        // The real seeded case: ~$15k book, one position (DRAM) at ~46%, cap at 25%.
        var book = new BookSnapshot(15000m, 1000m, [new BookPosition("DRAM", RiskSleeve.Brokerage, 100m, 6900m, 0.46m)], false, []);
        var ruleSet = new RiskRuleSet { UserId = UserId, MaxPositionWeightPct = 0.25m };

        var report = _service.Evaluate(book, ruleSet, []);

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

        var report = _service.Evaluate(book, ruleSet, []);

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

        var report = _service.Evaluate(book, ruleSet, []);

        report.Violations.Should().ContainSingle(v => v.RuleKey == RiskRuleKeys.MaxSleeveWeight && v.Subject == RiskSleeve.Brokerage);
    }

    [Fact]
    public void Evaluate_MinCashBufferBreach_IsFlagged()
    {
        var book = new BookSnapshot(10000m, 200m, [new BookPosition("AAPL", RiskSleeve.Brokerage, 1m, 9800m, 0.98m)], false, []);
        var ruleSet = new RiskRuleSet { UserId = UserId, MinCashBufferPct = 0.1m };

        var report = _service.Evaluate(book, ruleSet, []);

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

        var report = _service.Evaluate(book, ruleSet, [ack]);

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

        var report = _service.Evaluate(book, ruleSet, [ack]);

        report.Violations.Should().ContainSingle().Which.Status.Should().Be(PolicyViolationStatus.Worsened);
    }

    [Fact]
    public void Evaluate_StaleBook_FlagsReportStale_ButDoesNotAutoClearViolations()
    {
        var book = new BookSnapshot(15000m, 1000m, [new BookPosition("DRAM", RiskSleeve.Brokerage, 100m, 6900m, 0.46m)], true, ["brokerage"]);
        var ruleSet = new RiskRuleSet { UserId = UserId, MaxPositionWeightPct = 0.25m };

        var report = _service.Evaluate(book, ruleSet, []);

        report.IsStale.Should().BeTrue();
        report.Violations.Should().ContainSingle();
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
}
