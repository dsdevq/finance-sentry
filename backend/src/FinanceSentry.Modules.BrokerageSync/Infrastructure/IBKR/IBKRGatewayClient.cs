using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;

/// <summary>
/// HTTP client for the IBKR Client Portal Gateway, served by a per-user IBeam
/// sidecar container. The base URL is supplied per call so a single instance of
/// this client can talk to any user's gateway.
/// </summary>
public sealed class IBKRGatewayClient(HttpClient http, ILogger<IBKRGatewayClient> logger)
{
    public async Task<IBKRAccountsResponse> GetAccountsAsync(Uri baseUrl, CancellationToken ct = default)
    {
        // /portfolio/accounts is served by the read-only Portal session (tier 1)
        // and needs NO brokerage session — so it works for read-only IBKR users,
        // unlike /iserver/accounts which requires a tier-2 /iserver session that
        // demands trading permissions.
        var response = await http.GetAsync(new Uri(baseUrl, "/v1/api/portfolio/accounts"), ct);
        response.EnsureSuccessStatusCode();

        var accounts = await response.Content
            .ReadFromJsonAsync<List<IBKRPortfolioAccountResponse>>(cancellationToken: ct) ?? [];

        var accountIds = accounts
            .Select(a => a.AccountId ?? a.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .ToList();

        logger.LogInformation("IBKR portfolio/accounts → {Count} account(s)", accountIds.Count);
        return new IBKRAccountsResponse(accountIds);
    }

    public async Task<IReadOnlyList<IBKRPositionResponse>> GetPositionsAsync(Uri baseUrl, string accountId, CancellationToken ct = default)
    {
        var response = await http.GetAsync(new Uri(baseUrl, $"/v1/api/portfolio/{accountId}/positions/0"), ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<IBKRPositionResponse>>(cancellationToken: ct) ?? [];
    }
}
