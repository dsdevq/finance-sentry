using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;

/// <summary>
/// HTTP client for the IBKR Client Portal Gateway, served by the IBeam sidecar.
///
/// The gateway holds its own session — authenticated by IBeam from
/// <c>IBKR_ACCOUNT</c>/<c>IBKR_PASSWORD</c> env vars. We never POST credentials
/// from this client; we only check session status and read portfolio data.
/// </summary>
public sealed class IBKRGatewayClient(HttpClient http, IConfiguration configuration, ILogger<IBKRGatewayClient> logger)
{
    private readonly HttpClient _http = InitHttp(http, configuration);

    private static HttpClient InitHttp(HttpClient http, IConfiguration configuration)
    {
        http.BaseAddress = new Uri(configuration["IBKR:GatewayBaseUrl"] ?? "https://ibkr-gateway:5000");
        return http;
    }

    public async Task<IBKRAuthStatusResponse> GetAuthStatusAsync(CancellationToken ct = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync("/v1/api/iserver/auth/status", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning("IBKR gateway unreachable: {Error}", ex.Message);
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
