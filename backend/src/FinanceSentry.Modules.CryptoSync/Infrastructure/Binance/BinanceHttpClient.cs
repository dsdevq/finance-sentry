using System.Security.Cryptography;
using System.Text;
using FinanceSentry.Modules.CryptoSync.Domain.Exceptions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace FinanceSentry.Modules.CryptoSync.Infrastructure.Binance;

public sealed class BinanceHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly int _recvWindowMs;

    public BinanceHttpClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _baseUrl = configuration["Binance:BaseUrl"] ?? "https://api.binance.com";
        _recvWindowMs = int.TryParse(configuration["Binance:RecvWindowMs"], out var rw) ? rw : 5000;
    }

    public async Task<BinanceAccountResponse> GetAccountAsync(
        string apiKey,
        string apiSecret,
        CancellationToken ct = default)
    {
        var query = BuildSignedQuery(apiSecret, $"recvWindow={_recvWindowMs}");
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/v3/account?{query}");
        request.Headers.Add("X-MBX-APIKEY", apiKey);

        var response = await _httpClient.SendAsync(request, ct);
        return await DeserializeAsync<BinanceAccountResponse>(response, ct);
    }

    public async Task<IReadOnlyList<BinancePriceTicker>> GetAllPricesAsync(CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/v3/ticker/price", ct);
        return await DeserializeAsync<IReadOnlyList<BinancePriceTicker>>(response, ct);
    }

    private static string BuildSignedQuery(string apiSecret, string queryParams)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var payload = $"{queryParams}&timestamp={timestamp}";
        var signature = ComputeHmacSha256(apiSecret, payload);
        return $"{payload}&signature={signature}";
    }

    private static string ComputeHmacSha256(string secret, string data)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var hash = HMACSHA256.HashData(keyBytes, dataBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static async Task<T> DeserializeAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = JsonConvert.DeserializeObject<BinanceErrorResponse>(body);
            throw new BinanceException(
                error?.Message ?? $"Binance API error: HTTP {(int)response.StatusCode}",
                error?.Code);
        }

        return JsonConvert.DeserializeObject<T>(body)
            ?? throw new BinanceException("Binance API returned empty response.");
    }
}
