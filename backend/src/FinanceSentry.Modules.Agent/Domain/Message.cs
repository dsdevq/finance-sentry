namespace FinanceSentry.Modules.Agent.Domain;

/// <summary>
/// One turn in a <see cref="Conversation"/> (user, assistant, or tool exchange). Belongs to exactly
/// one conversation (and thus one user); cascade-deleted with its conversation.
/// </summary>
public sealed class Message
{
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    public MessageRole Role { get; set; }

    /// <summary>User/assistant natural-language content.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Assistant turn's requested tool_use blocks (name + input), serialized JSON — for replay/audit.
    /// Null on non-assistant rows or when no tools were called.
    /// </summary>
    public string? ToolCallsJson { get; set; }

    /// <summary>
    /// Tool turn's results returned to the model (name + short result summary), serialized JSON.
    /// Null on non-tool rows.
    /// </summary>
    public string? ToolResultsJson { get; set; }

    /// <summary>Ordering key within a conversation.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
