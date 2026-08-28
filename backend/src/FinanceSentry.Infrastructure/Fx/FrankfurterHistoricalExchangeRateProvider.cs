namespace FinanceSentry.Infrastructure.Fx;

using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

/// <summary>
/// Historical rates for ECB-published currencies (EUR, GBP, …) from Frankfurter — free,
/// no API key, bulk date ranges. It does not carry UAH; that comes from
/// <see cref="NbuHistoricalExchangeRateProvider"/>.
///
/// The feed is queried with the foreign currency as base and USD as symbol, so each row is
/// already "USD per 1 unit" and needs no inversion.
/// </summary>
public sealed class FrankfurterHistoricalExchangeRateProvider : IHistoricalExchangeRateProvider
{
    private const string QuoteCurrency = "USD";
    private const string DateFormat = "yyyy-MM-dd";

    // ECB publishes these against the euro; the list is deliberately narrow — the app's
    // non-UAH accounts are EUR/GBP/USD — so an unlisted currency falls through to the flat
    // live rate rather than silently returning nothing useful.
    private static readonly string[] SupportedCurrencies = ["EUR", "GBP", "USD"];

    private readonly HttpClient _http;
    private readonly ILogger<FrankfurterHistoricalExchangeRateProvider> _logger;

    public FrankfurterHistoricalExchangeRateProvider(
        HttpClient http, ILogger<FrankfurterHistoricalExchangeRateProvider> logger)
    {
        _http = http;
        _logger = logger;
    }

    public bool Supports(string currency) =>
        SupportedCurrencies.Contains(currency, StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyDictionary<DateOnly, decimal>> GetUsdPerUnitSeriesAsync(
        string currency, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (!Supports(currency))
            return new Dictionary<DateOnly, decimal>();

        // USD against itself is always 1 — no call needed, and the feed would reject it.
        if (string.Equals(currency, QuoteCurrency, StringComparison.OrdinalIgnoreCase))
            return new Dictionary<DateOnly, decimal>();

        var path = $"v1/{from.ToString(DateFormat, CultureInfo.InvariantCulture)}"
            + $"..{to.ToString(DateFormat, CultureInfo.InvariantCulture)}"
            + $"?base={currency.ToUpperInvariant()}&symbols={QuoteCurrency}";

        try
        {
            var response = await _http.GetFromJsonAsync<FrankfurterResponse>(path, ct);
            if (response?.Rates is null || response.Rates.Count == 0)
            {
                _logger.LogWarning(
                    "Frankfurter returned no {Currency} rates for {From}..{To}.", currency, from, to);
                return new Dictionary<DateOnly, decimal>();
            }

            var series = new Dictionary<DateOnly, decimal>(response.Rates.Count);
            foreach (var (rawDate, quotes) in response.Rates)
            {
                if (!DateOnly.TryParseExact(
                        rawDate, DateFormat, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var date))
                {
                    continue;
                }

                if (quotes.TryGetValue(QuoteCurrency, out var usdPerUnit) && usdPerUnit > 0m)
                    series[date] = usdPerUnit;
            }

            return series;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(
                ex, "Frankfurter historical rate fetch failed for {Currency} {From}..{To}.",
                currency, from, to);
            return new Dictionary<DateOnly, decimal>();
        }
    }

    private sealed record FrankfurterResponse(Dictionary<string, Dictionary<string, decimal>>? Rates);
}
