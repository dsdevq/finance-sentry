namespace FinanceSentry.Mcp.Tests;

using System.Reflection;
using FinanceSentry.Mcp.Tools;
using FluentAssertions;
using ModelContextProtocol.Server;
using Xunit;

public sealed class McpToolContractTests
{
    [Fact]
    public void ExposedTools_ShouldUseExplicitReadOnlySnakeCaseNames()
    {
        var tools = GetToolAttributes();

        tools.Select(tool => tool.Name).Should().BeEquivalentTo(
            [
                "get_account_balances",
                "get_account_positions",
                "get_account_summary",
                "get_account_trades",
                "get_alerts",
                "get_all_accounts",
                "get_bank_transactions",
                "get_cashflow_summary",
                "get_crypto_positions",
                "get_fx_exposure",
                "get_investment_thesis",
                "get_net_worth",
                "get_net_worth_history",
                "get_pa_allocation",
                "get_pa_performance_all_periods",
                "get_price_history",
                "get_report_calendar",
                "get_spending_by_category",
                "get_subscriptions",
                "get_total_exposure",
                "get_watchlist",
                "search_contracts",
            ]);

        tools.Should().OnlyContain(tool =>
            !string.IsNullOrWhiteSpace(tool.Name)
            && tool.Name == tool.Name.ToLowerInvariant()
            && tool.ReadOnly == true
            && tool.Destructive == false);
    }

    [Fact]
    public void ExposedTools_ShouldNotIncludeMutationOperations()
    {
        var forbiddenFragments = new[]
        {
            "create_",
            "delete_",
            "update_",
            "write_",
            "post_",
            "place_order",
            "cancel_order",
            "modify_order",
            "transfer_money",
            "convert_currency",
        };

        var toolNames = GetToolAttributes()
            .Select(tool => tool.Name)
            .Where(name => name is not null)
            .Cast<string>();

        toolNames.Should().OnlyContain(name =>
            !forbiddenFragments.Any(fragment => name.Contains(fragment, StringComparison.Ordinal)));
    }

    private static IReadOnlyCollection<McpServerToolAttribute> GetToolAttributes()
    {
        return typeof(AlertTools).Assembly
            .GetTypes()
            .Where(type => type.GetCustomAttribute<McpServerToolTypeAttribute>() is not null)
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static))
            .Select(method => method.GetCustomAttribute<McpServerToolAttribute>())
            .Where(attribute => attribute is not null)
            .Cast<McpServerToolAttribute>()
            .ToArray();
    }
}
