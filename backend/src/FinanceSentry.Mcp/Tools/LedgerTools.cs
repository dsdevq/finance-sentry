using System.ComponentModel;
using ModelContextProtocol.Server;

namespace FinanceSentry.Mcp.Tools;

public sealed record StubResult(string Status, string Tool, string Message);

public sealed record MetadataResult(string Version, IReadOnlyList<string> Tools);

[McpServerToolType]
public sealed class LedgerTools
{
    public static readonly IReadOnlyList<string> AllToolNames =
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

    private static StubResult Stub(string toolName) =>
        new("not_yet_available", toolName, "This capability is not yet wired to a live data source.");

    [McpServerTool(Name = "get_identity"), Description("Returns current user identity info.")]
    public StubResult GetIdentity() => Stub("get_identity");

    [McpServerTool(Name = "get_snapshot"), Description("Returns the latest net-worth snapshot.")]
    public StubResult GetSnapshot() => Stub("get_snapshot");

    [McpServerTool(Name = "get_accounts"), Description("Lists all linked financial accounts.")]
    public StubResult GetAccounts() => Stub("get_accounts");

    [McpServerTool(Name = "get_transactions"), Description("Lists recent transactions across all accounts.")]
    public StubResult GetTransactions() => Stub("get_transactions");

    [McpServerTool(Name = "get_bank_sync_status"), Description("Reports BankSync module sync health.")]
    public StubResult GetBankSyncStatus() => Stub("get_bank_sync_status");

    [McpServerTool(Name = "get_cash_positions"), Description("Lists cash account balances.")]
    public StubResult GetCashPositions() => Stub("get_cash_positions");

    [McpServerTool(Name = "get_crypto_positions"), Description("Lists CryptoSync portfolio holdings.")]
    public StubResult GetCryptoPositions() => Stub("get_crypto_positions");

    [McpServerTool(Name = "get_brokerage_positions"), Description("Lists BrokerageSync equity holdings.")]
    public StubResult GetBrokeragePositions() => Stub("get_brokerage_positions");

    [McpServerTool(Name = "get_budget_status"), Description("Summarises budget consumption for the current period.")]
    public StubResult GetBudgetStatus() => Stub("get_budget_status");

    [McpServerTool(Name = "get_alerts"), Description("Lists active financial alerts.")]
    public StubResult GetAlerts() => Stub("get_alerts");

    [McpServerTool(Name = "get_subscriptions"), Description("Lists detected active subscriptions.")]
    public StubResult GetSubscriptions() => Stub("get_subscriptions");

    [McpServerTool(Name = "get_dashboard_summary"), Description("Returns dashboard KPI summary.")]
    public StubResult GetDashboardSummary() => Stub("get_dashboard_summary");

    [McpServerTool(Name = "get_spending_report"), Description("Returns a spending report for the current period.")]
    public StubResult GetSpendingReport() => Stub("get_spending_report");

    [McpServerTool(Name = "get_data_quality"), Description("Reports data quality flags across all data sources.")]
    public StubResult GetDataQuality() => Stub("get_data_quality");

    [McpServerTool(Name = "search_transactions"), Description("Full-text search across transactions.")]
    public StubResult SearchTransactions(
        [Description("Search query string")] string query = "") => Stub("search_transactions");

    [McpServerTool(Name = "get_metadata"), Description("Returns server version and the complete tool catalog.")]
    public MetadataResult GetMetadata() => new("1.0.0", AllToolNames);
}
