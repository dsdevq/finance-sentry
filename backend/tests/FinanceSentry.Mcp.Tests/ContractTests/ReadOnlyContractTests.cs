using System.Runtime.CompilerServices;
using FinanceSentry.Mcp.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FinanceSentry.Mcp.Tests.ContractTests;

public sealed class ReadOnlyContractTests
{
    private static IReadOnlyList<IReadOnlyMcpTool> ResolveAllTools()
    {
        var services = new ServiceCollection();
        var toolInterface = typeof(IReadOnlyMcpTool);
        var mcpAssembly = toolInterface.Assembly;

        foreach (var toolType in mcpAssembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && toolInterface.IsAssignableFrom(t)))
        {
            var instance = (IReadOnlyMcpTool)RuntimeHelpers.GetUninitializedObject(toolType);
            services.AddSingleton(toolInterface, instance);
        }

        return services.BuildServiceProvider()
            .GetServices<IReadOnlyMcpTool>()
            .ToList();
    }

    [Fact]
    public void AllTools_AreReadOnly()
    {
        var tools = ResolveAllTools();

        tools.Should().NotBeEmpty();

        var nonReadOnly = tools
            .Where(t => !t.IsReadOnly)
            .Select(t => t.ToolName)
            .ToList();

        nonReadOnly.Should().BeEmpty(
            because: "every IReadOnlyMcpTool implementation must return IsReadOnly = true");
    }

    [Fact]
    public void NoMutationToolNames()
    {
        var tools = ResolveAllTools();
        string[] mutationPrefixes = ["create_", "update_", "delete_", "trigger_", "dismiss_"];

        tools.Should().NotBeEmpty();

        var mutatingNames = tools
            .Select(t => t.ToolName)
            .Where(name => mutationPrefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal)))
            .ToList();

        mutatingNames.Should().BeEmpty(
            because: "read-only MCP tools must not expose mutation operations " +
                     "(names must not start with create_, update_, delete_, trigger_, or dismiss_)");
    }
}
