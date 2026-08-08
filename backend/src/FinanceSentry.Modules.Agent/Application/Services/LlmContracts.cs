namespace FinanceSentry.Modules.Agent.Application.Services;

using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>
/// The domain interface over the LLM provider (Principle I — external integration behind an interface).
/// Implemented by <c>AnthropicLlmClient</c> against the Anthropic Messages API; mocked in tests so the
/// conversation loop is verified without ever calling the real API.
/// </summary>
public interface ILlmClient
{
    /// <summary>
    /// Streams one model turn: the assistant's text arrives as <see cref="LlmTextDelta"/> chunks, and a
    /// final <see cref="LlmMessageCompleted"/> carries the fully-assembled turn (text + any tool_use
    /// blocks) plus the stop reason. Throws <see cref="AgentNotConfiguredException"/> when keyless — no
    /// HTTP call is made.
    /// </summary>
    IAsyncEnumerable<LlmStreamChunk> StreamAsync(
        string system,
        IReadOnlyList<LlmMessage> messages,
        IReadOnlyList<LlmTool> tools,
        CancellationToken ct);
}

/// <summary>A tool advertised to the model — Anthropic tool-use schema (name/description/input_schema).</summary>
public sealed record LlmTool(string Name, string Description, JsonNode InputSchema);

/// <summary>One message in the running exchange (role = <c>user</c> or <c>assistant</c>).</summary>
public sealed record LlmMessage(string Role, IReadOnlyList<LlmContentBlock> Content)
{
    public static LlmMessage UserText(string text) =>
        new("user", [new LlmTextBlock(text)]);
}

public abstract record LlmContentBlock;

public sealed record LlmTextBlock(string Text) : LlmContentBlock;

public sealed record LlmToolUseBlock(string Id, string Name, JsonElement Input) : LlmContentBlock;

public sealed record LlmToolResultBlock(string ToolUseId, string Content, bool IsError) : LlmContentBlock;

public abstract record LlmStreamChunk;

/// <summary>An incremental slice of assistant text — forward straight to the UI.</summary>
public sealed record LlmTextDelta(string Text) : LlmStreamChunk;

/// <summary>
/// The turn is complete: <see cref="Content"/> is the assembled assistant message (text + tool_use
/// blocks), <see cref="StopReason"/> is Anthropic's stop reason (<c>tool_use</c> means dispatch + loop).
/// </summary>
public sealed record LlmMessageCompleted(IReadOnlyList<LlmContentBlock> Content, string? StopReason) : LlmStreamChunk;

/// <summary>Thrown when the Anthropic API key is unset. Surfaces as <c>agent_not_configured</c>; no HTTP call.</summary>
public sealed class AgentNotConfiguredException() : Exception("The finance agent is not configured (missing Anthropic API key).");
