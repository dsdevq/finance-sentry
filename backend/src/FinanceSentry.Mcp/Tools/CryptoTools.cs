namespace FinanceSentry.Mcp.Tools;

using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

[McpServerToolType]
public sealed class CryptoTools(FinanceSentryApiClient apiClient)
{
    private readonly FinanceSentryApiClient _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));

    [McpServerTool(Name = "get_crypto_positions", ReadOnly = true, Destructive = false)]
    [Description("Get crypto positions/holdings for the authenticated user.")]
    public Task<JsonElement> GetCryptoPositions(CancellationToken cancellationToken)
    {
        return _apiClient.GetJsonAsync("crypto/holdings", cancellationToken);
    }
}
