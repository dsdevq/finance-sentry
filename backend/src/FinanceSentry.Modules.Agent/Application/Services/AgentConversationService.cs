namespace FinanceSentry.Modules.Agent.Application.Services;

using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <inheritdoc />
public sealed class AgentConversationService(
    ILlmClient llmClient,
    McpToolBridge toolBridge,
    PersonaComposer personaComposer,
    IOptions<AgentOptions> options,
    ILogger<AgentConversationService> logger) : IAgentConversationService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ILlmClient _llmClient = llmClient;
    private readonly McpToolBridge _toolBridge = toolBridge;
    private readonly PersonaComposer _personaComposer = personaComposer;
    private readonly AgentOptions _options = options.Value;
    private readonly ILogger<AgentConversationService> _logger = logger;

    public async IAsyncEnumerable<AgentStreamEvent> RunAsync(
        IReadOnlyList<LlmMessage> messages,
        IServiceProvider toolScope,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var system = _personaComposer.Compose();
        var tools = _toolBridge.GetTools();
        var working = new List<LlmMessage>(messages);
        var toolCalls = new JsonArray();
        var lastText = string.Empty;

        for (var iteration = 0; iteration < _options.MaxToolIterations; iteration++)
        {
            LlmMessageCompleted? completed = null;
            var enumerator = _llmClient.StreamAsync(system, working, tools, ct).GetAsyncEnumerator(ct);

            await using (enumerator.ConfigureAwait(false))
            {
                while (true)
                {
                    bool moved;
                    AgentErrorEvent? failure = null;
                    try
                    {
                        moved = await enumerator.MoveNextAsync();
                    }
                    catch (AgentNotConfiguredException)
                    {
                        failure = new AgentErrorEvent("agent_not_configured", "The finance agent is not configured.");
                        moved = false;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "LLM stream failed.");
                        failure = new AgentErrorEvent("llm_unavailable", "The finance agent is temporarily unavailable.");
                        moved = false;
                    }

                    if (failure is not null)
                    {
                        yield return failure;
                        yield break;
                    }

                    if (!moved)
                    {
                        break;
                    }

                    var chunk = enumerator.Current;
                    if (chunk is LlmTextDelta delta)
                    {
                        yield return new AgentTextEvent(delta.Text);
                    }
                    else if (chunk is LlmMessageCompleted done)
                    {
                        completed = done;
                    }
                }
            }

            if (completed is null)
            {
                yield return new AgentErrorEvent("llm_unavailable", "The finance agent returned no response.");
                yield break;
            }

            working.Add(new LlmMessage("assistant", completed.Content));
            lastText = ConcatText(completed.Content);

            var toolUses = completed.Content.OfType<LlmToolUseBlock>().ToList();
            var wantsTools = string.Equals(completed.StopReason, "tool_use", StringComparison.Ordinal) && toolUses.Count > 0;

            if (!wantsTools)
            {
                yield return new AgentCompletionEvent(lastText, SerializeToolCalls(toolCalls));
                yield break;
            }

            var results = new List<LlmContentBlock>(toolUses.Count);
            foreach (var toolUse in toolUses)
            {
                RecordToolCall(toolCalls, toolUse);
                yield return new AgentToolEvent(toolUse.Name, "start");
                var result = await _toolBridge.DispatchAsync(toolUse.Id, toolUse.Name, toolUse.Input, toolScope, ct);
                yield return new AgentToolEvent(toolUse.Name, "end");
                results.Add(result);
            }

            working.Add(new LlmMessage("user", results));
        }

        // Iteration cap reached — return the best answer so far with a note rather than looping forever.
        _logger.LogInformation("Agent hit the max tool-iteration cap ({Cap}).", _options.MaxToolIterations);
        var capped = string.IsNullOrWhiteSpace(lastText)
            ? "I gathered as much as I could but couldn't finish within the tool-call budget. Ask me to narrow the question."
            : lastText + "\n\n(Note: I stopped after reaching my tool-call budget for this turn.)";
        yield return new AgentCompletionEvent(capped, SerializeToolCalls(toolCalls));
    }

    private static string ConcatText(IReadOnlyList<LlmContentBlock> content)
    {
        var builder = new StringBuilder();
        foreach (var block in content)
        {
            if (block is LlmTextBlock text)
            {
                builder.Append(text.Text);
            }
        }

        return builder.ToString();
    }

    private static void RecordToolCall(JsonArray toolCalls, LlmToolUseBlock toolUse)
    {
        toolCalls.Add(new JsonObject
        {
            ["name"] = toolUse.Name,
            ["input"] = JsonNode.Parse(toolUse.Input.GetRawText()),
        });
    }

    private static string? SerializeToolCalls(JsonArray toolCalls) =>
        toolCalls.Count == 0 ? null : toolCalls.ToJsonString(SerializerOptions);
}
