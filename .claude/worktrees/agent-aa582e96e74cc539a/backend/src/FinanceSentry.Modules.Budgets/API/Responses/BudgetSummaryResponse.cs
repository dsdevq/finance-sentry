namespace FinanceSentry.Modules.Budgets.API.Responses;

public record BudgetSummaryResponse(
    int Year,
    int Month,
    IReadOnlyList<BudgetSummaryItemDto> Items,
    decimal TotalLimit,
    decimal TotalSpent);
