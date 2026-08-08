namespace FinanceSentry.Modules.Agent.Application.Services;

/// <summary>
/// The server-side tool-use loop: compose persona → call the LLM with the bridged tools → dispatch each
/// <c>tool_use</c> in the caller's scope → iterate to a final answer, bounded by
/// <see cref="AgentOptions.MaxToolIterations"/>. Yields typed stream events (text, tool start/end, a
/// terminal completion, or error).
/// </summary>
public interface IAgentConversationService
{
    IAsyncEnumerable<AgentStreamEvent> RunAsync(
        IReadOnlyList<LlmMessage> messages,
        IServiceProvider toolScope,
        CancellationToken ct);
}
