namespace FinanceSentry.Modules.Budgets.Domain.Exceptions;

using FinanceSentry.Core.Exceptions;

public class BudgetInvalidLimitException()
    : ApiException(400, "BUDGET_INVALID_LIMIT", "Budget limit must be greater than zero.");
