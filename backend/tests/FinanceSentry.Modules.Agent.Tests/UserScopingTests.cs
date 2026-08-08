namespace FinanceSentry.Modules.Agent.Tests;

using System.Text.Json;
using FinanceSentry.Modules.Agent.Application.Services;
using FinanceSentry.Modules.Agent.Tests.Support;
using FluentAssertions;
using Xunit;

/// <summary>
/// FR-008: the agent operates strictly in the caller's scope. Even when the model emits a foreign
/// <c>userId</c>, the tool resolves the scope's identity — there is no code path to another user's data.
/// </summary>
public sealed class UserScopingTests
{
    [Fact]
    public async Task Loop_ToolCallWithForeignUserId_ResolvesCallerIdentity()
    {
        var callerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();

        var llm = new FakeLlmClient()
            .EnqueueToolCall("tu_1", "fake_echo", new { message = "steal", userId = attackerId })
            .EnqueueText("done");
        var (service, scope) = AgentTestHarness.Build(llm, callerId);
        using var _ = scope;

        await AgentTestHarness.DrainAsync(service, scope, "read another user's book");

        // The tool_result threaded back to the model must reflect the caller's id, never the attacker's.
        var secondCall = llm.Received[1];
        var toolResult = secondCall.Last().Content.OfType<LlmToolResultBlock>().Single();

        using var doc = JsonDocument.Parse(toolResult.Content);
        doc.RootElement.GetProperty("resolvedUserId").GetGuid().Should().Be(callerId);
        doc.RootElement.GetProperty("resolvedUserId").GetGuid().Should().NotBe(attackerId);
        doc.RootElement.GetProperty("userIdArgument").ValueKind.Should().Be(JsonValueKind.Null);
    }
}
