namespace FinanceSentry.Modules.Risk.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Risk.Domain;
using FinanceSentry.Modules.Risk.Domain.Repositories;

/// <summary>FR-003: acknowledge a pre-existing violation with a remediation note.</summary>
public record AcknowledgeViolationCommand(
    Guid UserId,
    string RuleKey,
    string Subject,
    string RemediationNote,
    decimal ObservedAtAck,
    decimal WorseningStepPct) : ICommand<Unit>;

public sealed class AcknowledgeViolationCommandHandler(IPolicyViolationAckRepository ackRepo)
    : ICommandHandler<AcknowledgeViolationCommand, Unit>
{
    public async Task<Unit> Handle(AcknowledgeViolationCommand command, CancellationToken ct)
    {
        var existing = await ackRepo.FindActiveAsync(command.UserId, command.RuleKey, command.Subject, ct);
        if (existing is not null)
        {
            await ackRepo.DeactivateAsync(existing.Id, ct);
        }

        await ackRepo.AddAsync(new PolicyViolationAck
        {
            UserId = command.UserId,
            RuleKey = command.RuleKey,
            Subject = command.Subject,
            RemediationNote = command.RemediationNote,
            ObservedAtAck = command.ObservedAtAck,
            WorseningStepPct = command.WorseningStepPct,
        }, ct);

        return Unit.Value;
    }
}
