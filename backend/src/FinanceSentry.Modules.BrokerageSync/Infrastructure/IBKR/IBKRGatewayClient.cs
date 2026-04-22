using FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

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
        var json = JsonConvert.SerializeObject(payload);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _http.PostAsync("/v1/api/iserver/auth/ssodh/init", content, ct);
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

        var body = await response.Content.ReadAsStringAsync(ct);
        return JsonConvert.DeserializeObject<IBKRAuthStatusResponse>(body)
            ?? new IBKRAuthStatusResponse(false, false);
    }

    public async Task<IBKRAccountsResponse> GetAccountsAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("/v1/api/iserver/accounts", ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        return JsonConvert.DeserializeObject<IBKRAccountsResponse>(body)
            ?? new IBKRAccountsResponse([]);
    }

    public async Task<IReadOnlyList<IBKRPositionResponse>> GetPositionsAsync(string accountId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"/v1/api/portfolio/{accountId}/positions/0", ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        return JsonConvert.DeserializeObject<List<IBKRPositionResponse>>(body) ?? [];
    }
}
