namespace FinanceSentry.Modules.Agent.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Agent.Domain;

/// <summary>Lists the caller's conversations, newest first (FR-008: owner-scoped).</summary>
public sealed record ListConversationsQuery(Guid UserId) : IQuery<IReadOnlyList<ConversationSummaryDto>>;

public sealed class ListConversationsQueryHandler(IConversationRepository repository)
    : IQueryHandler<ListConversationsQuery, IReadOnlyList<ConversationSummaryDto>>
{
    private readonly IConversationRepository _repository = repository;

    public async Task<IReadOnlyList<ConversationSummaryDto>> Handle(ListConversationsQuery query, CancellationToken cancellationToken)
    {
        var conversations = await _repository.ListAsync(query.UserId, cancellationToken);
        return conversations
            .Select(c => new ConversationSummaryDto(c.Id, c.Title, c.UpdatedAt, c.ModelId))
            .ToList();
    }
}
