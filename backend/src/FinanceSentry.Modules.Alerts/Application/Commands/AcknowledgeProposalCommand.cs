namespace FinanceSentry.Modules.Alerts.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Alerts.Domain.Repositories;

public record AcknowledgeProposalCommand(Guid UserId, Guid AlertId, string Decision) : ICommand<bool>;

public class AcknowledgeProposalCommandHandler(IAlertRepository alerts) : ICommandHandler<AcknowledgeProposalCommand, bool>
{
    private readonly IAlertRepository _alerts = alerts;

    public Task<bool> Handle(AcknowledgeProposalCommand command, CancellationToken cancellationToken)
        => _alerts.AcknowledgeAsync(command.UserId, command.AlertId, command.Decision, cancellationToken);
}

public record AcknowledgeProposalByReferenceCommand(Guid UserId, string AlertType, Guid ReferenceId, string Decision) : ICommand<bool>;

public class AcknowledgeProposalByReferenceCommandHandler(IAlertRepository alerts)
    : ICommandHandler<AcknowledgeProposalByReferenceCommand, bool>
{
    private readonly IAlertRepository _alerts = alerts;

    public Task<bool> Handle(AcknowledgeProposalByReferenceCommand command, CancellationToken cancellationToken)
        => _alerts.AcknowledgeByReferenceAsync(
            command.UserId, command.AlertType, command.ReferenceId, command.Decision, cancellationToken);
}
