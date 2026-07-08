namespace FinanceSentry.Modules.Risk.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Risk.Application.Services;
using FinanceSentry.Modules.Risk.Domain;
using FinanceSentry.Modules.Risk.Domain.Repositories;
using Microsoft.Extensions.Options;

public sealed record RiskProposal(string Ticker, decimal ProposedUsd, bool Override = false);

public sealed record CheckRiskRulesResult(ComplianceReport? Report, RiskVerdict? Verdict, bool HasRuleSet);

public record CheckRiskRulesQuery(Guid UserId, RiskProposal? Proposal = null) : IQuery<CheckRiskRulesResult>;

public sealed class CheckRiskRulesQueryHandler(
    IBookSnapshotReader bookReader,
    IRiskRuleSetRepository ruleSetRepo,
    IPolicyViolationAckRepository ackRepo,
    IHoldingSnapshotRepository snapshotRepo,
    IRiskEvaluationService evaluationService,
    ITurnoverTracker turnoverTracker,
    IOptions<RiskOptions> options)
    : IQueryHandler<CheckRiskRulesQuery, CheckRiskRulesResult>
{
    private readonly int _rollingQuarterDays = options.Value.RollingQuarterDays;

    public async Task<CheckRiskRulesResult> Handle(CheckRiskRulesQuery query, CancellationToken ct)
    {
        var book = await bookReader.ReadAsync(query.UserId, ct);
        var ruleSet = await ruleSetRepo.GetCurrentAsync(query.UserId, ct);

        if (query.Proposal is null)
        {
            var acks = await ackRepo.ListActiveAsync(query.UserId, ct);
            var report = evaluationService.Evaluate(book, ruleSet, acks);
            return new CheckRiskRulesResult(report, null, ruleSet is not null);
        }

        var now = DateTimeOffset.UtcNow;
        var history = await snapshotRepo.ListSinceAsync(query.UserId, now - TimeSpan.FromDays(_rollingQuarterDays), ct);
        var turnoverCount = turnoverTracker.CountDiscretionaryTradesInRollingQuarter(history, now);

        var verdict = evaluationService.EvaluateProposal(
            book, ruleSet, query.Proposal.Ticker, query.Proposal.ProposedUsd, turnoverCount);
        return new CheckRiskRulesResult(null, verdict, ruleSet is not null);
    }
}
