using System.Net.Http.Json;
using FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;
using Microsoft.Extensions.Configuration;

namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;

public sealed class IBKRGatewayClient
{
    private readonly HttpClient _http;

    public IBKRGatewayClient(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        var baseUrl = configuration["IBKR:GatewayBaseUrl"] ?? "http://ibkr-gateway:5000";
        _http.BaseAddress = new Uri(baseUrl);
    }

    public async Task AuthenticateAsync(string username, string password, CancellationToken ct = default)
    {
        var payload = new IBKRAuthInitRequest(username, password);

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync("/v1/api/iserver/auth/ssodh/init", payload, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new BrokerAuthException(
                $"IBKR gateway is unreachable at {_http.BaseAddress}. Start the IBKR Client Portal Gateway and retry.",
                "IBKR",
                ex);
        }

        if (!response.IsSuccessStatusCode)
            throw new BrokerAuthException("IBKR authentication failed.", "IBKR");

        var status = await GetAuthStatusAsync(ct);
        if (!status.Authenticated)
            throw new BrokerAuthException("IBKR gateway did not confirm authentication.", "IBKR");
    }

    public async Task<IBKRAuthStatusResponse> GetAuthStatusAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("/v1/api/iserver/auth/status", ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<IBKRAuthStatusResponse>(cancellationToken: ct)
            ?? new IBKRAuthStatusResponse(false, false);
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
