namespace FinanceSentry.Modules.Agent.Tests.Support;

using System.Text.Json;
using FinanceSentry.Modules.Agent.Application.Services;

/// <summary>
/// A scripted <see cref="ILlmClient"/> for the conversation-loop tests — never calls the real API. Each
/// scripted turn is a list of chunks the client replays; the loop consumes them like a real stream.
/// </summary>
public sealed class FakeLlmClient : ILlmClient
{
    private readonly Queue<IReadOnlyList<LlmStreamChunk>> _turns = new();

    public int CallCount { get; private set; }

    public List<IReadOnlyList<LlmMessage>> Received { get; } = [];

    public FakeLlmClient Enqueue(params LlmStreamChunk[] chunks)
    {
        _turns.Enqueue(chunks);
        return this;
    }

    /// <summary>Convenience: a plain final text answer (no tools).</summary>
    public FakeLlmClient EnqueueText(string text) =>
        Enqueue(new LlmTextDelta(text), new LlmMessageCompleted([new LlmTextBlock(text)], "end_turn"));

    /// <summary>Convenience: a turn that asks for one tool call.</summary>
    public FakeLlmClient EnqueueToolCall(string id, string name, object input)
    {
        var element = JsonSerializer.SerializeToElement(input);
        return Enqueue(new LlmMessageCompleted([new LlmToolUseBlock(id, name, element)], "tool_use"));
    }

    public async IAsyncEnumerable<LlmStreamChunk> StreamAsync(
        string system,
        IReadOnlyList<LlmMessage> messages,
        IReadOnlyList<LlmTool> tools,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        CallCount++;
        Received.Add([.. messages]); // snapshot — the loop mutates the working list in place

        var chunks = _turns.Count > 0
            ? _turns.Dequeue()
            : [new LlmMessageCompleted([new LlmTextBlock("(no more scripted turns)")], "end_turn")];

        foreach (var chunk in chunks)
        {
            await Task.Yield();
            yield return chunk;
        }
    }
}

/// <summary>An <see cref="ILlmClient"/> that always signals keyless — proves no HTTP call is attempted.</summary>
public sealed class KeylessLlmClient : ILlmClient
{
#pragma warning disable CS1998, CS0162 // immediate throw before the (required) yield is intentional
    public async IAsyncEnumerable<LlmStreamChunk> StreamAsync(
        string system,
        IReadOnlyList<LlmMessage> messages,
        IReadOnlyList<LlmTool> tools,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        throw new AgentNotConfiguredException();
        yield break;
    }
#pragma warning restore CS1998, CS0162
}
