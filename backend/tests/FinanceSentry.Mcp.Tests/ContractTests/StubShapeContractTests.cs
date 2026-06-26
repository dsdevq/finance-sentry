using System.Runtime.CompilerServices;
using System.Text.Json;
using FinanceSentry.Mcp.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FinanceSentry.Mcp.Tests.ContractTests;

public sealed class StubShapeContractTests
{
    private static IReadOnlyMcpTool ResolveStubByName(string toolName)
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
            .Single(t => t.ToolName == toolName);
    }

    [Theory]
    [InlineData("get_crypto_pnl_detail")]
    [InlineData("get_tax_lots")]
    [InlineData("get_cashflow_report")]
    [InlineData("get_net_worth_history")]
    public void StubTool_ReturnsNotYetAvailableShape(string toolName)
    {
        var tool = ResolveStubByName(toolName);

        var executeMethod = tool.GetType().GetMethod("Execute");
        executeMethod.Should().NotBeNull(because: $"{toolName} must expose an Execute() method");

        var result = executeMethod!.Invoke(tool, null);
        result.Should().NotBeNull();

        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("status").GetString()
            .Should().Be("not_yet_available",
                because: $"{toolName} must return status=\"not_yet_available\"");

        root.GetProperty("reason").GetString()
            .Should().NotBeNullOrWhiteSpace(
                because: $"{toolName} must include a non-empty reason explaining why it is unavailable");
    }
}
