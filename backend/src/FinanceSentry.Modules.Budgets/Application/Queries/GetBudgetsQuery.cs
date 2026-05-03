namespace FinanceSentry.Modules.Budgets.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Budgets.API.Responses;
using FinanceSentry.Modules.Budgets.Application.Services;
using FinanceSentry.Modules.Budgets.Domain.Repositories;

public record GetBudgetsQuery(Guid UserId) : IQuery<BudgetsListResponse>;

public class GetBudgetsQueryHandler(
    IBudgetRepository budgets,
    ICategoryNormalizationService normalization)
    : IQueryHandler<GetBudgetsQuery, BudgetsListResponse>
{
    private readonly IBudgetRepository _budgets = budgets;
    private readonly ICategoryNormalizationService _normalization = normalization;

    public async Task<BudgetsListResponse> Handle(GetBudgetsQuery request, CancellationToken cancellationToken)
    {
        var list = await _budgets.GetByUserIdAsync(request.UserId, cancellationToken);
        var dtos = list.Select(b => new BudgetDto(
            b.Id,
            b.Category,
            _normalization.GetLabel(b.Category),
            b.MonthlyLimit,
            b.Currency,
            b.CreatedAt)).ToList();

        return new BudgetsListResponse(dtos, dtos.Count);
    }
}
