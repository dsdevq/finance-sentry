using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Mcp.Tools;
using FluentAssertions;
using Xunit;

namespace FinanceSentry.Mcp.Tests;

public sealed class ToolRegistryTests
{
    [Fact]
    public void GetAccountSummaryTool_Implements_IReadOnlyMcpTool()
    {
        typeof(GetAccountSummaryTool).Should().Implement<IReadOnlyMcpTool>();
    }

    [Fact]
    public void ListTransactionsTool_Implements_IReadOnlyMcpTool()
    {
        typeof(ListTransactionsTool).Should().Implement<IReadOnlyMcpTool>();
    }

    [Fact]
    public void IReadOnlyMcpTool_IsReadOnly_DefaultsToTrue()
    {
        IReadOnlyMcpTool tool = new TestTool();
        tool.IsReadOnly.Should().BeTrue();
    }

    private sealed class TestTool : IReadOnlyMcpTool
    {
        public string ToolName => "test";
    }
}
