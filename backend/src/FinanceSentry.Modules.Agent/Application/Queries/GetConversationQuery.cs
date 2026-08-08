namespace FinanceSentry.Modules.Agent.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Agent.Domain;

/// <summary>Full history for one conversation. Returns null when not owned by the caller (never leaks).</summary>
public sealed record GetConversationQuery(Guid UserId, Guid ConversationId) : IQuery<ConversationDetailDto?>;

public sealed class GetConversationQueryHandler(IConversationRepository repository)
    : IQueryHandler<GetConversationQuery, ConversationDetailDto?>
{
    private readonly IConversationRepository _repository = repository;

    public async Task<ConversationDetailDto?> Handle(GetConversationQuery query, CancellationToken cancellationToken)
    {
        var conversation = await _repository.GetWithMessagesAsync(query.UserId, query.ConversationId, cancellationToken);
        if (conversation is null)
        {
            return null;
        }

        var messages = conversation.Messages
            .Select(m => new AgentMessageDto(
                m.Id,
                m.Role.ToString().ToLowerInvariant(),
                m.Content,
                m.ToolCallsJson,
                m.ToolResultsJson,
                m.CreatedAt))
            .ToList();

        return new ConversationDetailDto(
            conversation.Id,
            conversation.Title,
            conversation.CreatedAt,
            conversation.UpdatedAt,
            conversation.ModelId,
            messages);
    }
}
