namespace FinanceSentry.Tests.Unit.Mcp;

using System.Reflection;
using FinanceSentry.Mcp.Tools;
using FluentAssertions;
using ModelContextProtocol.Server;
using Xunit;

public class McpContractTests
{
    private static readonly string[] MutationPrefixes =
        ["create_", "update_", "delete_", "set_", "sync_", "trigger_", "patch_"];

    private static readonly string[] ExpectedToolNames =
    [
        "get_identity",
        "get_snapshot",
        "get_accounts",
        "get_transactions",
        "get_bank_sync_status",
        "get_cash_positions",
        "get_crypto_positions",
        "get_brokerage_positions",
        "get_budget_status",
        "get_alerts",
        "get_subscriptions",
        "get_dashboard_summary",
        "get_spending_report",
        "get_data_quality",
        "search_transactions",
        "get_metadata",
    ];

    private static IEnumerable<MethodInfo> ToolMethods() =>
        typeof(LedgerTools)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() is not null);

    private static string ToolName(MethodInfo method) =>
        method.GetCustomAttribute<McpServerToolAttribute>()!.Name ?? method.Name;

    private static object?[] DefaultParams(MethodInfo method) =>
        method.GetParameters()
            .Select(p => p.HasDefaultValue
                ? p.DefaultValue
                : p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null)
            .ToArray();

    [Fact]
    public void AllExpectedToolNamesAreRegistered()
    {
        var registered = ToolMethods()
            .Select(ToolName)
            .ToHashSet(StringComparer.Ordinal);

        registered.Should().BeEquivalentTo(ExpectedToolNames,
            "all 16 tools must be present on LedgerTools");
    }

    [Fact]
    public void NoToolHasMutationVerbs()
    {
        foreach (var method in ToolMethods())
        {
            var name = ToolName(method);
            MutationPrefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal))
                .Should().BeFalse($"tool '{name}' starts with a mutation verb prefix");
        }
    }

    [Fact]
    public void StubToolsReturnNotYetAvailable()
    {
        var tools = new LedgerTools();
        var stubMethods = ToolMethods()
            .Where(m => ToolName(m) != "get_metadata");

        foreach (var method in stubMethods)
        {
            var result = method.Invoke(tools, DefaultParams(method)) as StubResult;
            result.Should().NotBeNull(because: $"tool '{ToolName(method)}' should return a StubResult");
            result!.Status.Should().Be("not_yet_available",
                because: $"tool '{ToolName(method)}' is not yet wired to live data");
        }
    }

    [Fact]
    public void GetMetadataReturnsCatalog()
    {
        var tools = new LedgerTools();
        var result = tools.GetMetadata();

        result.Should().NotBeNull();
        result.Tools.Should().NotBeEmpty();
        result.Tools.Should().Contain("get_accounts");
        result.Tools.Should().Contain("get_transactions");
    }
}
