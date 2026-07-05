using FinanceSentry.Mcp.Tools;
using FluentAssertions;
using ModelContextProtocol.Server;
using Xunit;

namespace FinanceSentry.Mcp.Tests;

public sealed class ToolAttributeContractTests
{
    [Fact]
    public void RepresentativeTools_Have_McpServerToolTypeAttribute()
    {
        typeof(GetAccountSummaryTool).Should().BeDecoratedWith<McpServerToolTypeAttribute>();
        typeof(ListTransactionsTool).Should().BeDecoratedWith<McpServerToolTypeAttribute>();
        typeof(ListActiveAlertsTool).Should().BeDecoratedWith<McpServerToolTypeAttribute>();
    }

    [Fact]
    public void EveryToolType_Exposes_AtLeastOneAnnotatedMethod()
    {
        var invalidToolTypes = McpToolReflection.GetToolTypes()
            .Where(toolType => McpToolReflection.GetToolNames(toolType).Count == 0)
            .Select(toolType => toolType.Name)
            .ToList();

        invalidToolTypes.Should().BeEmpty(
            because: "every MCP tool type should expose at least one method annotated with [McpServerTool]");
    }
}
