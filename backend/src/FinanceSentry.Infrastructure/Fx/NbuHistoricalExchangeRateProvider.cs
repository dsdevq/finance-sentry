namespace FinanceSentry.Infrastructure.Fx;

using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

/// <summary>
/// Historical UAH rates from the National Bank of Ukraine (free, no API key, full history).
///
/// NBU is the authoritative source for the hryvnia and the only free feed that carries it —
/// ECB-backed feeds like Frankfurter omit UAH entirely. The endpoint returns "UAH per 1 USD"
/// (e.g. 38.002), which is inverted to the USD-per-unit multiplier used everywhere else.
/// </summary>
public sealed class NbuHistoricalExchangeRateProvider : IHistoricalExchangeRateProvider
{
    /// <summary>NBU quotes every currency against the hryvnia, so it can only answer for UAH here.</summary>
    private const string SupportedCurrency = "UAH";

    private const string DateFormat = "yyyyMMdd";
    private const string ResponseDateFormat = "dd.MM.yyyy";

    private readonly HttpClient _http;
    private readonly ILogger<NbuHistoricalExchangeRateProvider> _logger;

    public NbuHistoricalExchangeRateProvider(
        HttpClient http, ILogger<NbuHistoricalExchangeRateProvider> logger)
    {
        _http = http;
        _logger = logger;
    }

    public bool Supports(string currency) =>
        string.Equals(currency, SupportedCurrency, StringComparison.OrdinalIgnoreCase);

    public async Task<IReadOnlyDictionary<DateOnly, decimal>> GetUsdPerUnitSeriesAsync(
        string currency, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (!Supports(currency))
            return new Dictionary<DateOnly, decimal>();

        var path = "NBU_Exchange/exchange_site"
            + $"?start={from.ToString(DateFormat, CultureInfo.InvariantCulture)}"
            + $"&end={to.ToString(DateFormat, CultureInfo.InvariantCulture)}"
            + "&valcode=usd&sort=exchangedate&order=asc&json";

        try
        {
            var rows = await _http.GetFromJsonAsync<List<NbuRateRow>>(path, ct);
            if (rows is null || rows.Count == 0)
            {
                _logger.LogWarning("NBU returned no rates for {From}..{To}.", from, to);
                return new Dictionary<DateOnly, decimal>();
            }

            var series = new Dictionary<DateOnly, decimal>(rows.Count);
            foreach (var row in rows)
            {
                // "rate" is UAH per 1 USD; a non-positive value would make the inversion
                // meaningless, so skip rather than emit a bogus multiplier.
                if (row.Rate <= 0m) continue;

                if (!DateOnly.TryParseExact(
                        row.ExchangeDate, ResponseDateFormat, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var date))
                {
                    continue;
                }

                series[date] = 1m / row.Rate;
            }

            return series;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "NBU historical rate fetch failed for {From}..{To}.", from, to);
            return new Dictionary<DateOnly, decimal>();
        }
    }

    private sealed record NbuRateRow(
        [property: JsonPropertyName("exchangedate")] string ExchangeDate,
        [property: JsonPropertyName("cc")] string CurrencyCode,
        [property: JsonPropertyName("rate")] decimal Rate);
}
