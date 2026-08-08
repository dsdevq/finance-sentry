namespace FinanceSentry.Modules.Agent.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Agent.Domain;

/// <summary>Deletes a conversation (cascade messages). Owner-only; returns false when not owned.</summary>
public sealed record DeleteConversationCommand(Guid UserId, Guid ConversationId) : ICommand<bool>;

public sealed class DeleteConversationCommandHandler(IConversationRepository repository)
    : ICommandHandler<DeleteConversationCommand, bool>
{
    private readonly IConversationRepository _repository = repository;

    public Task<bool> Handle(DeleteConversationCommand command, CancellationToken cancellationToken)
        => _repository.DeleteAsync(command.UserId, command.ConversationId, cancellationToken);
}
