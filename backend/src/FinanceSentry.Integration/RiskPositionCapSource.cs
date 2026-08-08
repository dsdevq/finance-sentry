namespace FinanceSentry.Integration;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.Domain.Ports;
using FinanceSentry.Modules.Risk.Application.Queries;

/// <summary>
/// 039: implements the Research module's <see cref="IPositionCapSource"/> by reading the Risk rule
/// set — the single home of the single-position cap — through Risk's <see cref="GetRiskRuleSetQuery"/>.
/// Lives in the host so neither module references the other.
/// </summary>
public sealed class RiskPositionCapSource(IQueryHandler<GetRiskRuleSetQuery, RiskRuleSetDto?> getRules)
    : IPositionCapSource
{
    public async Task<decimal?> GetMaxPositionWeightAsync(Guid userId, CancellationToken ct)
    {
        var rules = await getRules.Handle(new GetRiskRuleSetQuery(userId), ct);
        return rules?.MaxPositionWeightPct;
    }
}
