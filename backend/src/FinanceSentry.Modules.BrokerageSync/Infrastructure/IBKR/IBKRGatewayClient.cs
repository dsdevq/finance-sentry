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
    public async Task<IBKRAuthStatusResponse> GetAuthStatusAsync(Uri baseUrl, CancellationToken ct = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.GetAsync(new Uri(baseUrl, "/v1/api/iserver/auth/status"), ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning("IBKR gateway {BaseUrl} unreachable: {Error}", baseUrl, ex.Message);
            return new IBKRAuthStatusResponse(false, false);
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        logger.LogInformation("IBKR auth/status → HTTP {Status}, body: {Body}", (int)response.StatusCode, body);

        if (!response.IsSuccessStatusCode)
            return new IBKRAuthStatusResponse(false, false);

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<IBKRAuthStatusResponse>(body)
                ?? new IBKRAuthStatusResponse(false, false);
        }
        catch (System.Text.Json.JsonException ex)
        {
            logger.LogWarning("IBKR auth/status JSON parse failed: {Error}", ex.Message);
            return new IBKRAuthStatusResponse(false, false);
        }
    }

    public async Task<IBKRAccountsResponse> GetAccountsAsync(Uri baseUrl, CancellationToken ct = default)
    {
        var response = await http.GetAsync(new Uri(baseUrl, "/v1/api/iserver/accounts"), ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<IBKRAccountsResponse>(cancellationToken: ct)
            ?? new IBKRAccountsResponse([]);
    }

    public async Task<IReadOnlyList<IBKRPositionResponse>> GetPositionsAsync(Uri baseUrl, string accountId, CancellationToken ct = default)
    {
        var response = await http.GetAsync(new Uri(baseUrl, $"/v1/api/portfolio/{accountId}/positions/0"), ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<IBKRPositionResponse>>(cancellationToken: ct) ?? [];
    }
}
