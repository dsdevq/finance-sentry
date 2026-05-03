namespace FinanceSentry.Modules.Alerts.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Alerts.Domain.Repositories;

public record MarkAllAlertsReadCommand(Guid UserId) : ICommand<Unit>;

public class MarkAllAlertsReadCommandHandler(IAlertRepository alerts) : ICommandHandler<MarkAllAlertsReadCommand, Unit>
{
    private readonly IAlertRepository _alerts = alerts;

    public async Task<Unit> Handle(MarkAllAlertsReadCommand command, CancellationToken cancellationToken)
    {
        await _alerts.MarkAllReadAsync(command.UserId, cancellationToken);
        return Unit.Value;
    }
}
