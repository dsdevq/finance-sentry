namespace FinanceSentry.Modules.Agent.Tests;

using System.Reflection;
using System.Text.Json;
using FinanceSentry.Mcp;
using FinanceSentry.Modules.Agent.Application.Services;
using FinanceSentry.Modules.Agent.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using Xunit;

public sealed class McpToolBridgeTests
{
    private static readonly Assembly TestAssembly = typeof(FakeEchoTool).Assembly;

    private static McpToolBridge FakeBridge(AgentOptions? options = null) =>
        new(Options.Create(options ?? new AgentOptions()), NullLogger<McpToolBridge>.Instance, TestAssembly);

    [Fact]
    public void GetTools_ConvertsMetadataVerbatim_AndStripsUserId()
    {
        var tools = FakeBridge().GetTools();

        var echo = tools.Single(t => t.Name == "fake_echo");
        echo.Description.Should().Be("Echoes the caller's resolved user id and a message.");

        var schema = echo.InputSchema.AsObject();
        schema["type"]!.GetValue<string>().Should().Be("object");

        var properties = schema["properties"]!.AsObject();
        properties.ContainsKey("message").Should().BeTrue();
        properties.ContainsKey("userId").Should().BeFalse("the caller's id is resolved server-side, never from model output (FR-008)");

        var required = schema["required"]!.AsArray().Select(n => n!.GetValue<string>()).ToList();
        required.Should().Contain("message");
        required.Should().NotContain("userId");
    }

    [Fact]
    public async Task Dispatch_ResolvesCallerId_IgnoringModelSuppliedUserId()
    {
        var callerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        using var provider = BuildScope(callerId);
        var bridge = FakeBridge();

        var input = JsonSerializer.SerializeToElement(new { message = "hi", userId = attackerId });
        var result = await bridge.DispatchAsync("tu_1", "fake_echo", input, provider, CancellationToken.None);

        result.IsError.Should().BeFalse();
        using var doc = JsonDocument.Parse(result.Content);
        doc.RootElement.GetProperty("resolvedUserId").GetGuid().Should().Be(callerId);
        doc.RootElement.GetProperty("userIdArgument").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Dispatch_UnknownTool_ReturnsIsError()
    {
        using var provider = BuildScope(Guid.NewGuid());
        var result = await FakeBridge().DispatchAsync(
            "tu_2", "does_not_exist", JsonSerializer.SerializeToElement(new { }), provider, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("Unknown tool");
    }

    [Fact]
    public async Task Dispatch_ToolThrows_DegradesToIsError()
    {
        using var provider = BuildScope(Guid.NewGuid());
        var result = await FakeBridge().DispatchAsync(
            "tu_3", "fake_throw", JsonSerializer.SerializeToElement(new { }), provider, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("failed");
    }

    [Fact]
    public void AllowList_RestrictsExposedTools()
    {
        var options = new AgentOptions { ToolAllowList = ["fake_echo"] };
        var tools = FakeBridge(options).GetTools();

        tools.Should().ContainSingle(t => t.Name == "fake_echo");
        tools.Should().NotContain(t => t.Name == "fake_throw");
    }

    [Fact]
    public void RealBridge_CoversTheWholeMcpSurface_WithUserIdStripped()
    {
        var bridge = new McpToolBridge(Options.Create(new AgentOptions()), NullLogger<McpToolBridge>.Instance);
        var tools = bridge.GetTools();

        tools.Count.Should().Be(ExpectedMcpToolNames().Count, "every [McpServerTool] should be bridged (no hand-picked subset)");
        tools.Select(t => t.Name).Should().Contain(["get_portfolio_snapshot", "get_ips", "check_risk_rules"]);

        foreach (var tool in tools)
        {
            var properties = tool.InputSchema.AsObject()["properties"]?.AsObject();
            properties?.ContainsKey("userId").Should().NotBe(true, $"{tool.Name} must not expose userId to the model");
        }
    }

    private static ServiceProvider BuildScope(Guid callerId) =>
        new ServiceCollection()
            .AddScoped<IFakeIdentity>(_ => new FakeIdentity(callerId))
            .AddScoped<FakeEchoTool>()
            .AddScoped<FakeThrowingTool>()
            .BuildServiceProvider();

    private static IReadOnlyCollection<string> ExpectedMcpToolNames() =>
        McpServiceRegistration.McpAssembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                && t.GetCustomAttribute<McpServerToolTypeAttribute>() is not null)
            .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            .Select(m => m.GetCustomAttribute<McpServerToolAttribute>()?.Name)
            .Where(n => n is not null)
            .Select(n => n!)
            .ToHashSet();
}
