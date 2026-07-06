using FluentAssertions;
using Xunit;

namespace FinanceSentry.Mcp.Tests.ContractTests;

public sealed class ToolNameContractTests
{
    private static readonly IReadOnlySet<string> AgreedToolSurface = new HashSet<string>
    {
        "add_to_watchlist",
        "delete_thesis",
        "get_account_summary",
        "get_allocation_vs_target",
        "get_budget_status",
        "get_cashflow_report",
        "get_crypto_pnl_detail",
        "get_earnings_calendar",
        "get_fundamentals",
        "get_ips",
        "get_macro_calendar",
        "get_net_worth_history",
        "get_news_for_ticker",
        "get_portfolio_snapshot",
        "get_quotes",
        "get_recent_filings",
        "get_sync_health",
        "get_tax_lots",
        "list_active_alerts",
        "list_subscriptions",
        "list_theses",
        "list_transactions",
        "list_watchlist",
        "remove_from_watchlist",
        "save_ips",
        "save_thesis",
        "search_market_news",
    };

    [Fact]
    public void ToolNames_MatchAgreedSurface()
    {
        var actual = McpToolReflection.GetToolNames().ToHashSet(StringComparer.Ordinal);

        actual.Should().BeEquivalentTo(
            AgreedToolSurface,
            because: "the MCP tool surface must match the agreed 27-tool contract — no more, no fewer");
    }
}
