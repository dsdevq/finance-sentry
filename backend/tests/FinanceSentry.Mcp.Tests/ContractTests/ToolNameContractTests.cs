using System.Runtime.CompilerServices;
using FinanceSentry.Mcp.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FinanceSentry.Mcp.Tests.ContractTests;

public sealed class ToolNameContractTests
{
    private static readonly IReadOnlySet<string> AgreedToolSurface = new HashSet<string>
    {
        "get_account_summary",
        "list_transactions",
        "get_budget_status",
        "list_active_alerts",
        "get_portfolio_snapshot",
        "list_subscriptions",
        "get_sync_health",
        "get_crypto_pnl_detail",
        "get_tax_lots",
        "get_cashflow_report",
        "get_net_worth_history",
    };

    [Fact]
    public void ToolNames_MatchAgreedSurface()
    {
        var services = new ServiceCollection();
        var toolInterface = typeof(IReadOnlyMcpTool);
        var mcpAssembly = toolInterface.Assembly;

        // Mirror the scanning logic from Program.cs. Use GetUninitializedObject instead of
        // type-based AddScoped so the test harness doesn't need module infrastructure
        // (DbContexts, query handlers). ToolName is always a constant expression-body property
        // with no dependency on constructor-initialized state.
        foreach (var toolType in mcpAssembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && toolInterface.IsAssignableFrom(t)))
        {
            var instance = (IReadOnlyMcpTool)RuntimeHelpers.GetUninitializedObject(toolType);
            services.AddSingleton(toolInterface, instance);
        }

        var sp = services.BuildServiceProvider();
        var actual = sp.GetServices<IReadOnlyMcpTool>()
            .Select(t => t.ToolName)
            .ToHashSet();

        actual.Should().BeEquivalentTo(
            AgreedToolSurface,
            because: "the MCP tool surface must match the agreed 11-tool contract — no more, no fewer");
    }
}
