namespace FinanceSentry.Modules.Budgets.Domain.Exceptions;

using FinanceSentry.Core.Exceptions;

public class BudgetInvalidPeriodException()
    : ApiException(400, "BUDGET_INVALID_PERIOD", "Invalid budget period.");
