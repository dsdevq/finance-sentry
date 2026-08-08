namespace FinanceSentry.Modules.Agent.Application.Commands;

using System.Runtime.CompilerServices;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Agent.Application.Services;
using FinanceSentry.Modules.Agent.Domain;
using Microsoft.Extensions.Options;

/// <summary>
/// Sends one user message to Ledger and streams the reply. Persists the user message (creating the
/// conversation when <see cref="ConversationId"/> is null, deriving a title), runs the tool-use loop in
/// the caller's scope, and persists the assistant message on completion. User scope is explicit
/// (<see cref="UserId"/>); tools resolve the same id from the request context (FR-008).
/// </summary>
public sealed record SendAgentMessageCommand(Guid UserId, Guid? ConversationId, string Message)
    : ICommand<IAsyncEnumerable<AgentStreamEvent>>;

public sealed class SendAgentMessageCommandHandler(
    IConversationRepository repository,
    IAgentConversationService conversationService,
    IOptions<AgentOptions> options,
    IServiceProvider requestServices)
    : ICommandHandler<SendAgentMessageCommand, IAsyncEnumerable<AgentStreamEvent>>
{
    private const int MaxTitleLength = 60;

    private readonly IConversationRepository _repository = repository;
    private readonly IAgentConversationService _conversationService = conversationService;
    private readonly AgentOptions _options = options.Value;
    private readonly IServiceProvider _requestServices = requestServices;

    public Task<IAsyncEnumerable<AgentStreamEvent>> Handle(SendAgentMessageCommand command, CancellationToken cancellationToken)
        => Task.FromResult(Stream(command, cancellationToken));

    private async IAsyncEnumerable<AgentStreamEvent> Stream(
        SendAgentMessageCommand command, [EnumeratorCancellation] CancellationToken ct)
    {
        if (!_options.IsConfigured)
        {
            yield return new AgentErrorEvent("agent_not_configured", "The finance agent is not configured.");
            yield break;
        }

        Conversation conversation;
        if (command.ConversationId is { } existingId)
        {
            var existing = await _repository.GetWithMessagesAsync(command.UserId, existingId, ct);
            if (existing is null)
            {
                yield return new AgentErrorEvent("conversation_not_found", "Conversation not found.");
                yield break;
            }

            conversation = existing;
        }
        else
        {
            conversation = await _repository.CreateAsync(command.UserId, DeriveTitle(command.Message), _options.ModelId, ct);
        }

        yield return new AgentConversationEvent(conversation.Id);

        var userMessage = new Message { Role = MessageRole.User, Content = command.Message };
        await _repository.AppendMessageAsync(command.UserId, conversation.Id, userMessage, ct);

        var llmMessages = BuildHistory(conversation.Messages);
        llmMessages.Add(LlmMessage.UserText(command.Message));

        var finalText = string.Empty;
        string? toolCallsJson = null;
        var errored = false;

        await foreach (var evt in _conversationService.RunAsync(llmMessages, _requestServices, ct))
        {
            switch (evt)
            {
                case AgentCompletionEvent completion:
                    finalText = completion.FinalText;
                    toolCallsJson = completion.ToolCallsJson;
                    break;
                case AgentErrorEvent error:
                    errored = true;
                    yield return error;
                    break;
                default:
                    yield return evt;
                    break;
            }
        }

        if (errored)
        {
            yield break;
        }

        var assistantMessage = new Message
        {
            Role = MessageRole.Assistant,
            Content = finalText,
            ToolCallsJson = toolCallsJson,
        };
        await _repository.AppendMessageAsync(command.UserId, conversation.Id, assistantMessage, ct);

        yield return new AgentDoneEvent(assistantMessage.Id);
    }

    private List<LlmMessage> BuildHistory(IReadOnlyList<Message> messages)
    {
        var recent = messages
            .Where(m => m.Role is MessageRole.User or MessageRole.Assistant)
            .OrderBy(m => m.CreatedAt)
            .TakeLast(_options.HistoryTurnBudget)
            .Select(m => new LlmMessage(
                m.Role == MessageRole.User ? "user" : "assistant",
                [new LlmTextBlock(m.Content)]))
            .ToList();

        return recent;
    }

    private static string DeriveTitle(string message)
    {
        var trimmed = message.Trim().ReplaceLineEndings(" ");
        return trimmed.Length <= MaxTitleLength ? trimmed : trimmed[..MaxTitleLength].TrimEnd() + "…";
    }
}
