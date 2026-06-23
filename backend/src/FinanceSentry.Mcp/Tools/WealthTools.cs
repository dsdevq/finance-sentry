namespace FinanceSentry.Mcp.Tools;

using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

[McpServerToolType]
public sealed class WealthTools(FinanceSentryApiClient apiClient)
{
    private readonly FinanceSentryApiClient _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));

    [McpServerTool(Name = "get_net_worth", ReadOnly = true, Destructive = false)]
    [Description("Get the user's current normalized net worth summary from Finance Sentry.")]
    public Task<JsonElement> GetNetWorth(CancellationToken cancellationToken)
    {
        return _apiClient.GetJsonAsync("wealth/summary", cancellationToken);
    }

    [McpServerTool(Name = "get_net_worth_history", ReadOnly = true, Destructive = false)]
    [Description("Get historical net worth snapshots for the authenticated user.")]
    public Task<JsonElement> GetNetWorthHistory(
        [Description("Optional inclusive start date in yyyy-MM-dd format.")] string? from,
        [Description("Optional inclusive end date in yyyy-MM-dd format.")] string? to,
        CancellationToken cancellationToken)
    {
        var query = QueryStringBuilder.Create()
            .Add("from", from)
            .Add("to", to)
            .ToString();

        return _apiClient.GetJsonAsync($"net-worth/history{query}", cancellationToken);
    }

    [McpServerTool(Name = "get_cashflow_summary", ReadOnly = true, Destructive = false)]
    [Description("Get aggregate transaction/cashflow summary for a date range.")]
    public Task<JsonElement> GetCashflowSummary(
        [Description("Inclusive start date in yyyy-MM-dd format.")] string from,
        [Description("Inclusive end date in yyyy-MM-dd format.")] string to,
        [Description("Optional source category: banking, crypto, brokerage, or other.")] string? category,
        [Description("Optional provider filter.")] string? provider,
        CancellationToken cancellationToken)
    {
        var query = QueryStringBuilder.Create()
            .Add("from", from)
            .Add("to", to)
            .Add("category", category)
            .Add("provider", provider)
            .ToString();

        return _apiClient.GetJsonAsync($"wealth/transactions/summary{query}", cancellationToken);
    }

    [McpServerTool(Name = "get_total_exposure", ReadOnly = true, Destructive = false)]
    [Description("Get total portfolio exposure. Stubbed until Finance Sentry exposes an exposure endpoint.")]
    public static JsonElement GetTotalExposure()
    {
        return FinanceSentryApiClient.NotYetAvailable("No total exposure endpoint exists yet.");
    }

    [McpServerTool(Name = "get_fx_exposure", ReadOnly = true, Destructive = false)]
    [Description("Get FX exposure. Stubbed until Finance Sentry exposes currency exposure analytics.")]
    public static JsonElement GetFxExposure()
    {
        return FinanceSentryApiClient.NotYetAvailable("No FX exposure endpoint exists yet.");
    }
}
