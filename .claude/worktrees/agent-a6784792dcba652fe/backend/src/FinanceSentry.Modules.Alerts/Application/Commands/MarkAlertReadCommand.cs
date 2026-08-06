namespace FinanceSentry.Modules.Alerts.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Alerts.Domain.Repositories;

public record MarkAlertReadCommand(Guid UserId, Guid AlertId) : ICommand<bool>;

public class MarkAlertReadCommandHandler(IAlertRepository alerts) : ICommandHandler<MarkAlertReadCommand, bool>
{
    private readonly IAlertRepository _alerts = alerts;

    public Task<bool> Handle(MarkAlertReadCommand command, CancellationToken cancellationToken)
        => _alerts.MarkReadAsync(command.UserId, command.AlertId, cancellationToken);
}
