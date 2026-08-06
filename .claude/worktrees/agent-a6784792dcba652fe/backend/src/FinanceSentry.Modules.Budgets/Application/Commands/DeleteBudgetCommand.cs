namespace FinanceSentry.Modules.Budgets.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Budgets.Domain.Exceptions;
using FinanceSentry.Modules.Budgets.Domain.Repositories;

public record DeleteBudgetCommand(Guid UserId, Guid BudgetId) : ICommand<Unit>;

public class DeleteBudgetCommandHandler(IBudgetRepository budgets)
    : ICommandHandler<DeleteBudgetCommand, Unit>
{
    private readonly IBudgetRepository _budgets = budgets;

    public async Task<Unit> Handle(DeleteBudgetCommand command, CancellationToken cancellationToken)
    {
        var budget = await _budgets.GetByIdAsync(command.BudgetId, cancellationToken);
        if (budget is null || budget.UserId != command.UserId)
            throw new BudgetNotFoundException();

        await _budgets.DeleteAsync(budget, cancellationToken);
        return Unit.Value;
    }
}
