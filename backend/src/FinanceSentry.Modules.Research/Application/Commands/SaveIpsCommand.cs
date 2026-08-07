namespace FinanceSentry.Modules.Research.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Repositories;

public record SaveIpsCommand(
    Guid UserId,
    IReadOnlyList<InvestmentGoal> Goals,
    int PrimaryHorizonYears,
    decimal? EmergencyCushionUsd,
    int RiskTolerance,
    int? RiskCapacity,
    decimal? MaxDrawdownTolerancePct,
    IReadOnlyList<AllocationTarget> AllocationTargets,
    RebalancingRule? RebalancingRule,
    ContributionPlan? ContributionPlan,
    string? SellDiscipline,
    int? CoolingOffDays,
    IReadOnlyList<string> Exclusions,
    string? ReviewCadence) : ICommand<IpsDto>;

public class SaveIpsCommandHandler(IIpsRepository repo) : ICommandHandler<SaveIpsCommand, IpsDto>
{
    private const string DefaultReviewCadence = "annual";

    public async Task<IpsDto> Handle(SaveIpsCommand cmd, CancellationToken ct)
    {
        var nextVersion = await repo.GetMaxVersionAsync(cmd.UserId, ct) + 1;

        var ips = new InvestmentPolicyStatement
        {
            UserId = cmd.UserId,
            Version = nextVersion,
            IsCurrent = true,
            Goals = cmd.Goals.ToList(),
            PrimaryHorizonYears = cmd.PrimaryHorizonYears,
            EmergencyCushionUsd = cmd.EmergencyCushionUsd,
            RiskTolerance = cmd.RiskTolerance,
            RiskCapacity = cmd.RiskCapacity,
            MaxDrawdownTolerancePct = cmd.MaxDrawdownTolerancePct,
            AllocationTargets = cmd.AllocationTargets.ToList(),
            RebalancingRule = cmd.RebalancingRule ?? Domain.RebalancingRule.Default,
            ContributionPlan = cmd.ContributionPlan,
            SellDiscipline = cmd.SellDiscipline?.Trim(),
            Exclusions = cmd.Exclusions.ToList(),
            ReviewCadence = string.IsNullOrWhiteSpace(cmd.ReviewCadence)
                ? DefaultReviewCadence
                : cmd.ReviewCadence.Trim(),
        };

        if (cmd.CoolingOffDays is { } days)
        {
            ips.CoolingOffDays = days;
        }

        await repo.AddVersionAsync(ips, ct);

        return new IpsDto(
            ips.Id, ips.Version, ips.IsCurrent, ips.Goals, ips.PrimaryHorizonYears,
            ips.EmergencyCushionUsd, ips.RiskTolerance, ips.RiskCapacity, ips.MaxDrawdownTolerancePct,
            ips.AllocationTargets, ips.RebalancingRule, ips.ContributionPlan, ips.SellDiscipline,
            ips.CoolingOffDays, ips.Exclusions, ips.ReviewCadence,
            ips.LastReviewedAt, ips.CreatedAt, ips.UpdatedAt);
    }
}
