namespace FinanceSentry.Mcp.Tools;

using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

[McpServerToolType]
public sealed class BankingTools(FinanceSentryApiClient apiClient)
{
    private readonly FinanceSentryApiClient _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));

    [McpServerTool(Name = "get_all_accounts", ReadOnly = true, Destructive = false)]
    [Description("Get all connected bank accounts for the authenticated user.")]
    public Task<JsonElement> GetAllAccounts(
        [Description("Optional account status filter.")] string? status,
        [Description("Optional currency filter.")] string? currency,
        CancellationToken cancellationToken)
    {
        var query = QueryStringBuilder.Create()
            .Add("status", status)
            .Add("currency", currency)
            .ToString();

        return _apiClient.GetJsonAsync($"accounts{query}", cancellationToken);
    }

    [McpServerTool(Name = "get_bank_transactions", ReadOnly = true, Destructive = false)]
    [Description("Get paged bank transactions across all connected accounts.")]
    public Task<JsonElement> GetBankTransactions(
        [Description("Result offset.")] int offset,
        [Description("Maximum number of transactions to return.")] int limit,
        [Description("Optional start timestamp/date.")] string? from,
        [Description("Optional end timestamp/date.")] string? to,
        CancellationToken cancellationToken)
    {
        var query = QueryStringBuilder.Create()
            .Add("offset", offset.ToString())
            .Add("limit", limit.ToString())
            .Add("from", from)
            .Add("to", to)
            .ToString();

        return _apiClient.GetJsonAsync($"accounts/transactions{query}", cancellationToken);
    }
}
