namespace FinanceSentry.Modules.Agent.Tests;

using FinanceSentry.Modules.Agent.Application.Services;
using FinanceSentry.Modules.Agent.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

public sealed class KeylessTests
{
    [Fact]
    public void AgentOptions_IsConfigured_ReflectsKeyPresence()
    {
        new AgentOptions().IsConfigured.Should().BeFalse();

        var withKey = new AgentOptions();
        withKey.Anthropic.ApiKey = "sk-ant-test";
        withKey.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public async Task Keyless_YieldsAgentNotConfigured_NoModelCall()
    {
        // KeylessLlmClient throws AgentNotConfiguredException on first move — i.e. no HTTP is attempted.
        var (service, scope) = AgentTestHarness.Build(new KeylessLlmClient(), Guid.NewGuid());
        using var _ = scope;

        var events = await AgentTestHarness.DrainAsync(service, scope, "hello");

        var error = events.OfType<AgentErrorEvent>().Should().ContainSingle().Subject;
        error.Code.Should().Be("agent_not_configured");
        events.OfType<AgentCompletionEvent>().Should().BeEmpty();
    }

    [Fact]
    public async Task AnthropicClient_Keyless_ThrowsBeforeAnyHttp()
    {
        // A real client with no key must throw AgentNotConfiguredException without touching the network.
        var options = Options.Create(new AgentOptions());
        var factory = new ThrowingHttpClientFactory();
        var client = new FinanceSentry.Modules.Agent.Infrastructure.AnthropicLlmClient(
            factory, options, NullLogger<FinanceSentry.Modules.Agent.Infrastructure.AnthropicLlmClient>.Instance);

        var act = async () =>
        {
            await foreach (var _ in client.StreamAsync("sys", [LlmMessage.UserText("hi")], [], CancellationToken.None))
            {
            }
        };

        await act.Should().ThrowAsync<AgentNotConfiguredException>();
        factory.CreateCount.Should().Be(0, "no HttpClient should be created when keyless");
    }

    private sealed class ThrowingHttpClientFactory : IHttpClientFactory
    {
        public int CreateCount { get; private set; }

        public HttpClient CreateClient(string name)
        {
            CreateCount++;
            throw new InvalidOperationException("HTTP must not be attempted when keyless.");
        }
    }
}
