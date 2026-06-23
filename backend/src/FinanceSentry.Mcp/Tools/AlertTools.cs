namespace FinanceSentry.Mcp.Tools;

using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

[McpServerToolType]
public sealed class AlertTools(FinanceSentryApiClient apiClient)
{
    private readonly FinanceSentryApiClient _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));

    [McpServerTool(Name = "get_alerts", ReadOnly = true, Destructive = false)]
    [Description("Get alerts for the authenticated user.")]
    public Task<JsonElement> GetAlerts(
        [Description("Alert filter, e.g. all or unread.")] string filter,
        [Description("Page number.")] int page,
        [Description("Page size.")] int pageSize,
        CancellationToken cancellationToken)
    {
        var query = QueryStringBuilder.Create()
            .Add("filter", string.IsNullOrWhiteSpace(filter) ? "all" : filter)
            .Add("page", page <= 0 ? "1" : page.ToString())
            .Add("pageSize", pageSize <= 0 ? "20" : pageSize.ToString())
            .ToString();

        return _apiClient.GetJsonAsync($"alerts{query}", cancellationToken);
    }
}
