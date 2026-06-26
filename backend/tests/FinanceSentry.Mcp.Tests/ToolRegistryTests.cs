using FluentAssertions;
using Xunit;

namespace FinanceSentry.Mcp.Tests;

public sealed class ToolRegistryTests
{
    [Fact]
    public void ToolRegistry_InstantiatesWithEmptyToolList()
    {
        var registry = new ToolRegistry();

        registry.ToolNames.Should().BeEmpty();
    }

    [Fact]
    public void Register_AddsToolName()
    {
        var registry = new ToolRegistry();

        registry.Register("get-accounts");

        registry.ToolNames.Should().ContainSingle().Which.Should().Be("get-accounts");
    }
}
