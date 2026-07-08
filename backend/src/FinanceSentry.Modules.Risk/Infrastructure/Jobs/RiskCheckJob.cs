namespace FinanceSentry.Modules.Risk.Infrastructure.Jobs;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Risk.Application.Services;
using FinanceSentry.Modules.Risk.Domain;
using FinanceSentry.Modules.Risk.Domain.Repositories;
using Hangfire;
using Microsoft.Extensions.Options;

/// <summary>
/// Daily (after sync) check of the live book against the active RiskRuleSet (FR-002). Writes a
/// HoldingSnapshot row per position, raises Alerts + radar_signals for violations, and flags
/// adds-to-broken-thesis (FR-006). Deterministic only — no LLM, no composite score (FR-008).
/// </summary>
public sealed class RiskCheckJob(
    IBankingTotalsReader bankingTotals,
    IBookSnapshotReader bookReader,
    IRiskRuleSetRepository ruleSetRepo,
    IPolicyViolationAckRepository ackRepo,
    IHoldingSnapshotRepository snapshotRepo,
    IRiskEvaluationService evaluationService,
    IAddToBrokenThesisDetector brokenThesisDetector,
    IBrokenThesisReader brokenThesisReader,
    IAlertGeneratorService alertGenerator,
    IRadarSignalWriter signalWriter,
    IOptions<RiskOptions> options)
{
    private readonly int _rollingQuarterDays = options.Value.RollingQuarterDays;

    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var userIds = await bankingTotals.GetActiveUserIdsAsync(ct);
        foreach (var userId in userIds)
        {
            await CheckForUserAsync(userId, ct);
        }
    }

    public async Task CheckForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var book = await bookReader.ReadAsync(userId, ct);
        var now = DateTimeOffset.UtcNow;

        await snapshotRepo.AddRangeAsync(
            book.Positions
                .Select(p => new HoldingSnapshot
                {
                    UserId = userId,
                    Symbol = p.Symbol,
                    Sleeve = p.Sleeve,
                    Quantity = p.Quantity,
                    UsdValue = p.UsdValue,
                    CapturedAt = now,
                })
                .ToList(),
            ct);

        var ruleSet = await ruleSetRepo.GetCurrentAsync(userId, ct);
        var acks = await ackRepo.ListActiveAsync(userId, ct);
        var report = evaluationService.Evaluate(book, ruleSet, acks, now);

        await EmitComplianceSignalsAsync(userId, report, ct);
        await DetectAddToBrokenThesisAsync(userId, now, ct);
    }

    private async Task EmitComplianceSignalsAsync(Guid userId, ComplianceReport report, CancellationToken ct)
    {
        if (!report.HasRuleSet)
        {
            await signalWriter.AppendSignalAsync(new RadarSignalRequest(
                Scanner: "risk_rules",
                SignalType: "no_rules_configured",
                Severity: SignalSeverity.Info,
                SubjectType: "User",
                Subject: userId.ToString(),
                UserId: userId,
                DedupKey: $"risk_rules:no_rules:{userId}:{report.GeneratedAt:yyyy-MM-dd}",
                Payload: new { }), ct);
            return;
        }

        var alertWorthy = report.Violations
            .Where(v => v.Status is PolicyViolationStatus.New or PolicyViolationStatus.Worsened)
            .ToList();

        foreach (var violation in alertWorthy)
        {
            await alertGenerator.GeneratePolicyViolationAlertAsync(
                userId, violation.RuleKey, violation.Subject, violation.ObservedValue, violation.LimitValue,
                isOverride: false, ct);

            await signalWriter.AppendSignalAsync(new RadarSignalRequest(
                Scanner: "risk_rules",
                SignalType: "policy_violation",
                Severity: SignalSeverity.Alerted,
                SubjectType: "Ticker",
                Subject: violation.Subject,
                UserId: userId,
                DedupKey: $"risk_rules:policy_violation:{userId}:{violation.RuleKey}:{violation.Subject}:{report.GeneratedAt:yyyy-MM-dd}",
                Payload: new
                {
                    violation.RuleKey,
                    violation.ObservedValue,
                    violation.LimitValue,
                    violation.ExcessUsd,
                    violation.Status,
                }), ct);
        }

        if (report.Violations.Count == 0)
        {
            await signalWriter.AppendSignalAsync(new RadarSignalRequest(
                Scanner: "risk_rules",
                SignalType: "compliant",
                Severity: SignalSeverity.Info,
                SubjectType: "User",
                Subject: userId.ToString(),
                UserId: userId,
                DedupKey: $"risk_rules:compliant:{userId}:{report.GeneratedAt:yyyy-MM-dd}",
                Payload: new { }), ct);
        }
    }

    private async Task DetectAddToBrokenThesisAsync(Guid userId, DateTimeOffset now, CancellationToken ct)
    {
        var brokenTheses = await brokenThesisReader.ListBrokenAsync(userId, ct);
        if (brokenTheses.Count == 0)
        {
            return;
        }

        var history = await snapshotRepo.ListSinceAsync(userId, now - TimeSpan.FromDays(_rollingQuarterDays), ct);
        var flags = brokenThesisDetector.Detect(history, brokenTheses);

        foreach (var flag in flags)
        {
            await alertGenerator.GeneratePolicyViolationAlertAsync(
                userId, RiskRuleKeys.AddToBrokenThesis, flag.Ticker, flag.ToQuantity, flag.FromQuantity,
                isOverride: false, ct);

            await signalWriter.AppendSignalAsync(new RadarSignalRequest(
                Scanner: "risk_rules",
                SignalType: "add_to_broken_thesis",
                Severity: SignalSeverity.Notable,
                SubjectType: "Ticker",
                Subject: flag.Ticker,
                UserId: userId,
                DedupKey: $"risk_rules:add_to_broken_thesis:{userId}:{flag.Ticker}:{flag.IncreasedAt:yyyy-MM-dd}",
                Payload: new
                {
                    flag.FromQuantity,
                    flag.ToQuantity,
                    flag.IncreasedAt,
                }), ct);
        }
    }
}
