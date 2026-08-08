namespace FinanceSentry.Modules.Agent.Tests.Support;

using System.Reflection;
using FinanceSentry.Modules.Agent.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

/// <summary>Builds a conversation service wired to the fake tools + a scripted/keyless LLM client.</summary>
public static class AgentTestHarness
{
    private static readonly Assembly TestAssembly = typeof(FakeEchoTool).Assembly;

    public static (AgentConversationService Service, ServiceProvider Scope) Build(
        ILlmClient llmClient, Guid callerId, AgentOptions? options = null)
    {
        var opts = options ?? new AgentOptions();
        opts.PersonaRootPath = RepoPaths.RepoRoot();

        var bridge = new McpToolBridge(Options.Create(opts), NullLogger<McpToolBridge>.Instance, TestAssembly);
        var persona = new PersonaComposer(Options.Create(opts), NullLogger<PersonaComposer>.Instance);
        var service = new AgentConversationService(
            llmClient, bridge, persona, Options.Create(opts), NullLogger<AgentConversationService>.Instance);

        var scope = new ServiceCollection()
            .AddScoped<IFakeIdentity>(_ => new FakeIdentity(callerId))
            .AddScoped<FakeEchoTool>()
            .AddScoped<FakeThrowingTool>()
            .BuildServiceProvider();

        return (service, scope);
    }

    public static async Task<List<AgentStreamEvent>> DrainAsync(
        AgentConversationService service, ServiceProvider scope, string userMessage)
    {
        var events = new List<AgentStreamEvent>();
        var messages = new List<LlmMessage> { LlmMessage.UserText(userMessage) };
        await foreach (var evt in service.RunAsync(messages, scope, CancellationToken.None))
        {
            events.Add(evt);
        }

        return events;
    }
}
