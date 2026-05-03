namespace FinanceSentry.Modules.Budgets.API.Responses;

public record BudgetsListResponse(IReadOnlyList<BudgetDto> Items, int TotalCount);
