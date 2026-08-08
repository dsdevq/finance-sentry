namespace FinanceSentry.Modules.Agent.Tests;

using FinanceSentry.Modules.Agent.Application.Services;
using FinanceSentry.Modules.Agent.Tests.Support;
using FluentAssertions;
using Xunit;

public sealed class AgentConversationServiceTests
{
    [Fact]
    public async Task Loop_Terminates_WithPlainTextAnswer()
    {
        var llm = new FakeLlmClient().EnqueueText("Your net worth is $1.8M.");
        var (service, scope) = AgentTestHarness.Build(llm, Guid.NewGuid());
        using var _ = scope;

        var events = await AgentTestHarness.DrainAsync(service, scope, "what's my net worth?");

        events.OfType<AgentTextEvent>().Select(e => e.Delta).Should().Contain("Your net worth is $1.8M.");
        var completion = events.OfType<AgentCompletionEvent>().Should().ContainSingle().Subject;
        completion.FinalText.Should().Be("Your net worth is $1.8M.");
        llm.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Loop_DispatchesTool_ThreadsResult_ThenFinalAnswer()
    {
        var llm = new FakeLlmClient()
            .EnqueueToolCall("tu_1", "fake_echo", new { message = "ping" })
            .EnqueueText("All set.");
        var (service, scope) = AgentTestHarness.Build(llm, Guid.NewGuid());
        using var _ = scope;

        var events = await AgentTestHarness.DrainAsync(service, scope, "run the tool");

        events.OfType<AgentToolEvent>().Select(e => (e.Name, e.Phase))
            .Should().ContainInOrder(("fake_echo", "start"), ("fake_echo", "end"));
        events.OfType<AgentCompletionEvent>().Single().FinalText.Should().Be("All set.");

        llm.CallCount.Should().Be(2);
        // The second model call must carry the tool_result back to the model.
        var secondCall = llm.Received[1];
        secondCall.Last().Role.Should().Be("user");
        secondCall.Last().Content.OfType<LlmToolResultBlock>().Should().ContainSingle();
    }

    [Fact]
    public async Task Loop_RespectsMaxIterationCap()
    {
        var llm = new FakeLlmClient()
            .EnqueueToolCall("tu_1", "fake_echo", new { message = "a" })
            .EnqueueToolCall("tu_2", "fake_echo", new { message = "b" })
            .EnqueueToolCall("tu_3", "fake_echo", new { message = "c" });
        var (service, scope) = AgentTestHarness.Build(llm, Guid.NewGuid(), new AgentOptions { MaxToolIterations = 2 });
        using var _ = scope;

        var events = await AgentTestHarness.DrainAsync(service, scope, "loop forever");

        llm.CallCount.Should().Be(2, "the loop must stop at the iteration cap");
        var completion = events.OfType<AgentCompletionEvent>().Should().ContainSingle().Subject;
        completion.FinalText.Should().Contain("budget");
    }

    [Fact]
    public async Task Loop_RecordsToolCalls_ForAudit()
    {
        var llm = new FakeLlmClient()
            .EnqueueToolCall("tu_1", "fake_echo", new { message = "ping" })
            .EnqueueText("done");
        var (service, scope) = AgentTestHarness.Build(llm, Guid.NewGuid());
        using var _ = scope;

        var events = await AgentTestHarness.DrainAsync(service, scope, "go");

        var completion = events.OfType<AgentCompletionEvent>().Single();
        completion.ToolCallsJson.Should().NotBeNull();
        completion.ToolCallsJson!.Should().Contain("fake_echo");
    }
}
