namespace FinanceSentry.Modules.Alerts.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Alerts.Domain.Repositories;

public record DismissAlertCommand(Guid UserId, Guid AlertId) : ICommand<bool>;

public class DismissAlertCommandHandler(IAlertRepository alerts) : ICommandHandler<DismissAlertCommand, bool>
{
    private readonly IAlertRepository _alerts = alerts;

    public Task<bool> Handle(DismissAlertCommand command, CancellationToken cancellationToken)
        => _alerts.DismissAsync(command.UserId, command.AlertId, cancellationToken);
}
