namespace FinanceSentry.Modules.Risk.Domain.Repositories;

public interface IRiskRuleSetRepository
{
    Task<RiskRuleSet?> GetCurrentAsync(Guid userId, CancellationToken ct = default);

    Task<RiskRuleSet> SaveNewVersionAsync(RiskRuleSet ruleSet, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> GetUserIdsWithRuleSetsAsync(CancellationToken ct = default);
}
