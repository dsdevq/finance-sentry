namespace FinanceSentry.Mcp.Tools;

using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

[McpServerToolType]
public sealed class BrokerageTools(FinanceSentryApiClient apiClient)
{
    private readonly FinanceSentryApiClient _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));

    [McpServerTool(Name = "get_account_summary", ReadOnly = true, Destructive = false)]
    [Description("IBKR-compatible: get brokerage account summary from Finance Sentry holdings.")]
    public Task<JsonElement> GetAccountSummary(CancellationToken cancellationToken)
    {
        return _apiClient.GetJsonAsync("brokerage/holdings", cancellationToken);
    }

    [McpServerTool(Name = "get_account_balances", ReadOnly = true, Destructive = false)]
    [Description("IBKR-compatible: get brokerage account balances from Finance Sentry holdings.")]
    public Task<JsonElement> GetAccountBalances(CancellationToken cancellationToken)
    {
        return _apiClient.GetJsonAsync("brokerage/holdings", cancellationToken);
    }

    [McpServerTool(Name = "get_account_positions", ReadOnly = true, Destructive = false)]
    [Description("IBKR-compatible: get brokerage account positions from Finance Sentry holdings.")]
    public Task<JsonElement> GetAccountPositions(CancellationToken cancellationToken)
    {
        return _apiClient.GetJsonAsync("brokerage/holdings", cancellationToken);
    }

    [McpServerTool(Name = "get_account_trades", ReadOnly = true, Destructive = false)]
    [Description("IBKR-compatible: get brokerage account trades. Stubbed until Finance Sentry stores brokerage trades.")]
    public static JsonElement GetAccountTrades()
    {
        return FinanceSentryApiClient.NotYetAvailable("No brokerage trades endpoint exists yet.");
    }

    [McpServerTool(Name = "get_pa_allocation", ReadOnly = true, Destructive = false)]
    [Description("IBKR-compatible: get portfolio allocation. Stubbed until Finance Sentry exposes allocation analytics.")]
    public static JsonElement GetPaAllocation()
    {
        return FinanceSentryApiClient.NotYetAvailable("No portfolio allocation endpoint exists yet.");
    }

    [McpServerTool(Name = "get_pa_performance_all_periods", ReadOnly = true, Destructive = false)]
    [Description("IBKR-compatible: get portfolio performance for all periods. Stubbed until Finance Sentry exposes performance analytics.")]
    public static JsonElement GetPaPerformanceAllPeriods()
    {
        return FinanceSentryApiClient.NotYetAvailable("No portfolio performance endpoint exists yet.");
    }

    [McpServerTool(Name = "search_contracts", ReadOnly = true, Destructive = false)]
    [Description("IBKR-compatible: search contracts. Stubbed until Finance Sentry stores contract metadata.")]
    public static JsonElement SearchContracts([Description("Search query.")] string query)
    {
        _ = query;
        return FinanceSentryApiClient.NotYetAvailable("No contract search endpoint exists yet.");
    }

    [McpServerTool(Name = "get_price_history", ReadOnly = true, Destructive = false)]
    [Description("IBKR-compatible: get price history. Stubbed until Finance Sentry stores price history.")]
    public static JsonElement GetPriceHistory([Description("Contract identifier.")] string conid)
    {
        _ = conid;
        return FinanceSentryApiClient.NotYetAvailable("No price history endpoint exists yet.");
    }
}
