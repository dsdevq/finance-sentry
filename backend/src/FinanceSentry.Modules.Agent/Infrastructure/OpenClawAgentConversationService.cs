namespace FinanceSentry.Modules.Agent.Infrastructure;

using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FinanceSentry.Modules.Agent.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// The OpenClaw-backed brain (feature 040): instead of running FS's own persona + tool loop, it
/// delegates the whole turn to the existing OpenClaw Ledger (<c>finance</c>) agent over the gateway's
/// OpenAI-compatible API (<c>POST /v1/chat/completions</c>, <c>model: openclaw/finance</c>, SSE). That
/// agent owns its persona, its MCP tool surface (finance-sentry, etc.), and its own credentials — so the
/// browser chat is the real Ledger with no Anthropic key in FS. Conversations are independent: FS is the
/// source of truth for browser threads and replays full history each turn, so the gateway is driven
/// statelessly (no OpenAI <c>user</c> session key) and browser/Telegram memories never cross.
/// </summary>
public sealed class OpenClawAgentConversationService(
    IHttpClientFactory httpClientFactory,
    IOptions<AgentOptions> options,
    ILogger<OpenClawAgentConversationService> logger) : IAgentConversationService
{
    public const string HttpClientName = "agent-openclaw";

    private const int MaxLoggedBody = 500;

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly AgentOptions.OpenClawOptions _openClaw = options.Value.OpenClaw;
    private readonly ILogger<OpenClawAgentConversationService> _logger = logger;

    public async IAsyncEnumerable<AgentStreamEvent> RunAsync(
        IReadOnlyList<LlmMessage> messages,
        IServiceProvider toolScope,
        [EnumeratorCancellation] CancellationToken ct)
    {
        // toolScope is unused: the OpenClaw finance agent dispatches its own tools server-side.
        var payload = BuildRequestBody(messages);
        var client = _httpClientFactory.CreateClient(HttpClientName);

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
        {
            Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        if (!string.IsNullOrWhiteSpace(_openClaw.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openClaw.Token);
        }

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        HttpResponseMessage? response = null;
        try
        {
            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenClaw gateway request failed.");
        }

        if (response is null)
        {
            yield return new AgentErrorEvent("llm_unavailable", "The finance agent is temporarily unavailable.");
            yield break;
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "OpenClaw API returned {Status}: {Body}", (int)response.StatusCode, Truncate(body));
                yield return new AgentErrorEvent("llm_unavailable", "The finance agent is temporarily unavailable.");
                yield break;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var builder = new StringBuilder();

            while (true)
            {
                string? line;
                AgentErrorEvent? failure = null;
                try
                {
                    ct.ThrowIfCancellationRequested();
                    line = await reader.ReadLineAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "OpenClaw stream read failed.");
                    failure = new AgentErrorEvent("llm_unavailable", "The finance agent stream was interrupted.");
                    line = null;
                }

                if (failure is not null)
                {
                    yield return failure;
                    yield break;
                }

                if (line is null)
                {
                    break;
                }

                if (!line.StartsWith("data:", StringComparison.Ordinal))
                {
                    continue;
                }

                var data = line["data:".Length..].Trim();
                if (data.Length == 0)
                {
                    continue;
                }

                if (string.Equals(data, "[DONE]", StringComparison.Ordinal))
                {
                    break;
                }

                var delta = ExtractDelta(data);
                if (!string.IsNullOrEmpty(delta))
                {
                    builder.Append(delta);
                    yield return new AgentTextEvent(delta);
                }
            }

            yield return new AgentCompletionEvent(builder.ToString(), null);
        }
    }

    private JsonObject BuildRequestBody(IReadOnlyList<LlmMessage> messages) => new()
    {
        ["model"] = _openClaw.Model,
        ["stream"] = true,
        ["messages"] = SerializeMessages(messages),
    };

    /// <summary>Flattens the FS message history into OpenAI chat messages (role + plain-text content).</summary>
    private static JsonArray SerializeMessages(IReadOnlyList<LlmMessage> messages)
    {
        var array = new JsonArray();
        foreach (var message in messages)
        {
            var text = new StringBuilder();
            foreach (var block in message.Content)
            {
                if (block is LlmTextBlock textBlock)
                {
                    text.Append(textBlock.Text);
                }
            }

            array.Add(new JsonObject { ["role"] = message.Role, ["content"] = text.ToString() });
        }

        return array;
    }

    /// <summary>Pulls the incremental assistant text from one OpenAI streaming chunk, if any.</summary>
    private string? ExtractDelta(string data)
    {
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(data);
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Skipping unparsable OpenClaw SSE data line.");
            return null;
        }

        var choice = node?["choices"]?.AsArray() is { Count: > 0 } choices ? choices[0] : null;
        var content = choice?["delta"]?["content"];
        return content?.GetValueKind() == JsonValueKind.String ? content.GetValue<string>() : null;
    }

    private static string Truncate(string value) => value.Length <= MaxLoggedBody ? value : value[..MaxLoggedBody];
}
