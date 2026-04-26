using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;

/// <summary>
/// HTTP client for the IBKR Client Portal Gateway, served by the IBeam sidecar.
///
/// The gateway holds its own session — authenticated by IBeam from
/// <c>IBKR_ACCOUNT</c>/<c>IBKR_PASSWORD</c> env vars. We never POST credentials
/// from this client; we only check session status and read portfolio data.
/// </summary>
public sealed class IBKRGatewayClient
{
    private readonly HttpClient _http;

    public IBKRGatewayClient(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        var baseUrl = configuration["IBKR:GatewayBaseUrl"] ?? "https://ibkr-gateway:5000";
        _http.BaseAddress = new Uri(baseUrl);
    }

    public async Task<IBKRAuthStatusResponse> GetAuthStatusAsync(CancellationToken ct = default)
    {
        // Any non-success response (404 while IBeam boots, 401 before login completes,
        // network failure if the sidecar is down) collapses to "not authenticated".
        // EnsureSessionAsync turns that into a friendly BrokerAuthException upstream.
        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync("/v1/api/iserver/auth/status", ct);
        }
        catch (HttpRequestException)
        {
            return new IBKRAuthStatusResponse(false, false);
        }

        if (!response.IsSuccessStatusCode)
            return new IBKRAuthStatusResponse(false, false);

        try
        {
            return await response.Content.ReadFromJsonAsync<IBKRAuthStatusResponse>(cancellationToken: ct)
                ?? new IBKRAuthStatusResponse(false, false);
        }
        catch (System.Text.Json.JsonException)
        {
            return new IBKRAuthStatusResponse(false, false);
        }
    }

    public async Task<IBKRAccountsResponse> GetAccountsAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("/v1/api/iserver/accounts", ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<IBKRAccountsResponse>(cancellationToken: ct)
            ?? new IBKRAccountsResponse([]);
    }

    public async Task<IReadOnlyList<IBKRPositionResponse>> GetPositionsAsync(string accountId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"/v1/api/portfolio/{accountId}/positions/0", ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<IBKRPositionResponse>>(cancellationToken: ct) ?? [];
    }
}
