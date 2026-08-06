namespace FinanceSentry.Infrastructure.Fx;

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

/// <summary>
/// Live FX rates from open.er-api.com (free, no API key, ~160 currencies
/// including UAH — which ECB-backed feeds like Frankfurter omit).
///
/// The feed returns "units per 1 USD" (e.g. UAH ≈ 41.5); we invert to the
/// "USD per 1 unit" multiplier that <see cref="Core.Utils.CurrencyConverter"/> wants.
/// </summary>
public sealed class OpenErApiExchangeRateProvider : IExchangeRateProvider
{
    internal const string RatesPath = "v6/latest/USD";

    private readonly HttpClient _http;
    private readonly ILogger<OpenErApiExchangeRateProvider> _logger;

    public OpenErApiExchangeRateProvider(
        HttpClient http, ILogger<OpenErApiExchangeRateProvider> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<string, decimal>?> GetUsdRatesAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<OpenErApiResponse>(RatesPath, ct);

            if (response is null || !string.Equals(response.Result, "success", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Exchange rate feed returned a non-success result; keeping existing rates.");
                return null;
            }

            if (response.Rates is null || response.Rates.Count == 0)
            {
                _logger.LogWarning("Exchange rate feed returned no rates; keeping existing rates.");
                return null;
            }

            var usdPerUnit = new Dictionary<string, decimal>(response.Rates.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var (currency, unitsPerUsd) in response.Rates)
            {
                if (unitsPerUsd > 0m)
                    usdPerUnit[currency] = 1m / unitsPerUsd;
            }

            _logger.LogInformation("Refreshed {Count} exchange rates from open.er-api.com.", usdPerUnit.Count);
            return usdPerUnit;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Exchange rate refresh failed; keeping existing rates.");
            return null;
        }
    }
}

internal sealed record OpenErApiResponse
{
    [JsonPropertyName("result")]
    public string? Result { get; init; }

    [JsonPropertyName("base_code")]
    public string? BaseCode { get; init; }

    [JsonPropertyName("rates")]
    public Dictionary<string, decimal>? Rates { get; init; }
}
