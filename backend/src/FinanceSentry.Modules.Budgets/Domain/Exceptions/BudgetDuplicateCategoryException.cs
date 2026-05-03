namespace FinanceSentry.Modules.Budgets.Domain.Exceptions;

using FinanceSentry.Core.Exceptions;

public class BudgetDuplicateCategoryException()
    : ApiException(409, "BUDGET_DUPLICATE_CATEGORY", "A budget for this category already exists.");
