namespace FinanceSentry.Modules.Budgets.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Budgets.API.Responses;
using FinanceSentry.Modules.Budgets.Application.Services;
using FinanceSentry.Modules.Budgets.Domain.Exceptions;
using FinanceSentry.Modules.Budgets.Domain.Repositories;

public record UpdateBudgetCommand(Guid UserId, Guid BudgetId, decimal MonthlyLimit) : ICommand<BudgetDto>;

public class UpdateBudgetCommandHandler(
    IBudgetRepository budgets,
    ICategoryNormalizationService normalization)
    : ICommandHandler<UpdateBudgetCommand, BudgetDto>
{
    private readonly IBudgetRepository _budgets = budgets;
    private readonly ICategoryNormalizationService _normalization = normalization;

    public async Task<BudgetDto> Handle(UpdateBudgetCommand command, CancellationToken cancellationToken)
    {
        if (command.MonthlyLimit <= 0)
            throw new BudgetInvalidLimitException();

        var budget = await _budgets.GetByIdAsync(command.BudgetId, cancellationToken);
        if (budget is null || budget.UserId != command.UserId)
            throw new BudgetNotFoundException();

        budget.UpdateLimit(command.MonthlyLimit);
        await _budgets.UpdateAsync(budget, cancellationToken);

        return new BudgetDto(
            budget.Id,
            budget.Category,
            _normalization.GetLabel(budget.Category),
            budget.MonthlyLimit,
            budget.Currency,
            budget.CreatedAt);
    }
}
