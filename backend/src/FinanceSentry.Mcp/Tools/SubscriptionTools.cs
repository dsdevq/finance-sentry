namespace FinanceSentry.Mcp.Tools;

using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

[McpServerToolType]
public sealed class SubscriptionTools(FinanceSentryApiClient apiClient)
{
    private readonly FinanceSentryApiClient _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));

    [McpServerTool(Name = "get_subscriptions", ReadOnly = true, Destructive = false)]
    [Description("Get detected subscriptions for the authenticated user.")]
    public Task<JsonElement> GetSubscriptions(
        [Description("Include dismissed subscriptions.")] bool includeDismissed,
        CancellationToken cancellationToken)
    {
        var query = QueryStringBuilder.Create()
            .Add("includeDismissed", includeDismissed.ToString().ToLowerInvariant())
            .ToString();

        return _apiClient.GetJsonAsync($"subscriptions{query}", cancellationToken);
    }
}
