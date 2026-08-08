namespace FinanceSentry.Modules.Agent.Domain;

/// <summary>
/// Persistence for agent conversations and their messages. Every method is user-scoped (FR-008):
/// callers pass the authenticated user's id and can never reach another user's thread.
/// </summary>
public interface IConversationRepository
{
    Task<Conversation> CreateAsync(Guid userId, string? title, string modelId, CancellationToken ct);

    /// <summary>Returns the conversation with its ordered messages, or null when not owned by the user.</summary>
    Task<Conversation?> GetWithMessagesAsync(Guid userId, Guid conversationId, CancellationToken ct);

    /// <summary>Header-only list for the sidebar, newest first.</summary>
    Task<IReadOnlyList<Conversation>> ListAsync(Guid userId, CancellationToken ct);

    Task AppendMessageAsync(Guid userId, Guid conversationId, Message message, CancellationToken ct);

    /// <summary>Deletes the conversation (cascade messages). Returns false when not owned by the user.</summary>
    Task<bool> DeleteAsync(Guid userId, Guid conversationId, CancellationToken ct);
}
