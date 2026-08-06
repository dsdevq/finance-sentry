namespace FinanceSentry.Modules.Companion.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Companion.Domain.Repositories;

/// <summary>
/// Marks the given events Delivered after the agent has delivered them to the user (feature 031, US2),
/// so they don't resurface in a pull, scan, or digest. Foreign/unknown ids are ignored.
/// </summary>
public record AcknowledgeCompanionEventsCommand(Guid UserId, IReadOnlyList<Guid> EventIds) : ICommand<int>;

public class AcknowledgeCompanionEventsCommandHandler(ICompanionEventRepository events)
    : ICommandHandler<AcknowledgeCompanionEventsCommand, int>
{
    public async Task<int> Handle(AcknowledgeCompanionEventsCommand cmd, CancellationToken ct)
        => await events.MarkDeliveredAsync(cmd.UserId, cmd.EventIds ?? [], ct);
}
