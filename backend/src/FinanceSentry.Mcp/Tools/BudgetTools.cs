namespace FinanceSentry.Mcp.Tools;

using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

[McpServerToolType]
public sealed class BudgetTools(FinanceSentryApiClient apiClient)
{
    private readonly FinanceSentryApiClient _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));

    [McpServerTool(Name = "get_spending_by_category", ReadOnly = true, Destructive = false)]
    [Description("Get budget/spending summary for a month.")]
    public Task<JsonElement> GetSpendingByCategory(
        [Description("Optional year filter.")] int? year,
        [Description("Optional month filter.")] int? month,
        CancellationToken cancellationToken)
    {
        var query = QueryStringBuilder.Create()
            .Add("year", year?.ToString())
            .Add("month", month?.ToString())
            .ToString();

        return _apiClient.GetJsonAsync($"budgets/summary{query}", cancellationToken);
    }
}
