namespace FinanceSentry.Modules.Budgets.Domain.Exceptions;

using FinanceSentry.Core.Exceptions;

public class BudgetInvalidCategoryException()
    : ApiException(400, "BUDGET_INVALID_CATEGORY", "Invalid budget category.");
