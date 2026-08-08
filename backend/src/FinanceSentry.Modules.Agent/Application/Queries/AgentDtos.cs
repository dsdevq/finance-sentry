namespace FinanceSentry.Modules.Agent.Application.Queries;

/// <summary>Header row for the conversation sidebar.</summary>
public sealed record ConversationSummaryDto(Guid Id, string? Title, DateTimeOffset UpdatedAt, string ModelId);

/// <summary>A persisted message with its tool metadata.</summary>
public sealed record AgentMessageDto(
    Guid Id,
    string Role,
    string Content,
    string? ToolCallsJson,
    string? ToolResultsJson,
    DateTimeOffset CreatedAt);

/// <summary>Full conversation with ordered messages.</summary>
public sealed record ConversationDetailDto(
    Guid Id,
    string? Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string ModelId,
    IReadOnlyList<AgentMessageDto> Messages);
