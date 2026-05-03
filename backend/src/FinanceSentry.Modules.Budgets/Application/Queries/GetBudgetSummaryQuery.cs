namespace FinanceSentry.Modules.Budgets.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.BankSync.Application.Queries;
using FinanceSentry.Modules.Budgets.API.Responses;
using FinanceSentry.Modules.Budgets.Application.Services;
using FinanceSentry.Modules.Budgets.Domain.Exceptions;
using FinanceSentry.Modules.Budgets.Domain.Repositories;

public record GetBudgetSummaryQuery(Guid UserId, int? Year = null, int? Month = null)
    : IQuery<BudgetSummaryResponse>;

public class GetBudgetSummaryQueryHandler(
    IBudgetRepository budgets,
    ICategoryNormalizationService normalization,
    IQueryHandler<GetMerchantSpendingQuery, IReadOnlyDictionary<string, decimal>> merchantSpending)
    : IQueryHandler<GetBudgetSummaryQuery, BudgetSummaryResponse>
{
    private readonly IBudgetRepository _budgets = budgets;
    private readonly ICategoryNormalizationService _normalization = normalization;
    private readonly IQueryHandler<GetMerchantSpendingQuery, IReadOnlyDictionary<string, decimal>> _merchantSpending = merchantSpending;

    public async Task<BudgetSummaryResponse> Handle(GetBudgetSummaryQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var year = request.Year ?? now.Year;
        var month = request.Month ?? now.Month;

        if (month < 1 || month > 12 || year < 2020)
            throw new BudgetInvalidPeriodException();

        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1).AddDays(-1);

        var userBudgets = await _budgets.GetByUserIdAsync(request.UserId, cancellationToken);

        var rawSpending = await _merchantSpending.Handle(
            new GetMerchantSpendingQuery(request.UserId, from, to),
            cancellationToken);

        var spentByCategory = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var (rawCategory, amount) in rawSpending)
        {
            var normalized = _normalization.Normalize(rawCategory);
            spentByCategory[normalized] = spentByCategory.GetValueOrDefault(normalized) + amount;
        }

        var items = userBudgets.Select(b =>
        {
            var spent = spentByCategory.GetValueOrDefault(b.Category, 0m);
            var remaining = b.MonthlyLimit - spent;
            return new BudgetSummaryItemDto(
                b.Id,
                b.Category,
                _normalization.GetLabel(b.Category),
                b.MonthlyLimit,
                spent,
                remaining,
                spent > b.MonthlyLimit,
                b.Currency);
        }).ToList();

        return new BudgetSummaryResponse(
            year,
            month,
            items,
            items.Sum(i => i.MonthlyLimit),
            items.Sum(i => i.Spent));
    }
}
