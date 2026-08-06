namespace FinanceSentry.Modules.Budgets.API.Responses;

public record BudgetDto(
    Guid Id,
    string Category,
    string CategoryLabel,
    decimal MonthlyLimit,
    string Currency,
    DateTimeOffset CreatedAt);
