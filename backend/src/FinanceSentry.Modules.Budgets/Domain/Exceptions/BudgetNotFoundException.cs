namespace FinanceSentry.Modules.Budgets.Domain.Exceptions;

using FinanceSentry.Core.Exceptions;

public class BudgetNotFoundException() : ApiException(404, "BUDGET_NOT_FOUND", "Budget not found.");
