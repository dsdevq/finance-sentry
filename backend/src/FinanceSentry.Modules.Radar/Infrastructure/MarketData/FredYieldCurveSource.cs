namespace FinanceSentry.Modules.Radar.Infrastructure.MarketData;

using System.Globalization;
using System.Text.Json;
using FinanceSentry.Modules.Radar.Application.Services;
using FinanceSentry.Modules.Radar.Domain.Regime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// <see cref="IYieldCurveSource"/> over the FRED <c>series/observations</c> endpoint (feature 021).
/// Plain REST + JSON keyed by the <c>api_key</c> query param; no key ⇒ <see cref="IsConfigured"/>
/// is false and no request is issued (FR-005, mirroring the Finnhub keyless-silent precedent).
/// FRED's "." no-observation placeholders are skipped; the latest valid value per series is used
/// and the 10y-2y spread is computed by the caller. Contract:
/// <c>specs/021-market-regime/contracts/fred-series-observations.md</c>.
/// </summary>
public sealed class FredYieldCurveSource(
    IHttpClientFactory httpFactory,
    IOptions<RegimeOptions> options,
    ILogger<FredYieldCurveSource> logger) : IYieldCurveSource
{
    public const string HttpClientName = "regime-fred";

    private RegimeOptions.FredOptions Fred => options.Value.Fred;

    public bool IsConfigured => Fred.Enabled && !string.IsNullOrWhiteSpace(Fred.ApiKey);

    public async Task<YieldCurveReading?> GetLatestAsync(CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            return null;
        }

        var client = httpFactory.CreateClient(HttpClientName);
        var tenYear = await FetchLatestAsync(client, Fred.TenYearSeriesId, ct);
        var twoYear = await FetchLatestAsync(client, Fred.TwoYearSeriesId, ct);

        if (tenYear is null || twoYear is null)
        {
            logger.LogWarning(
                "FRED yield curve unavailable this run (10y={TenState}, 2y={TwoState})",
                tenYear is null ? "missing" : "ok",
                twoYear is null ? "missing" : "ok");
            return null;
        }

        var asOf = tenYear.Date >= twoYear.Date ? tenYear.Date : twoYear.Date;
        return new YieldCurveReading(tenYear.Value, twoYear.Value, asOf);
    }

    private async Task<YieldObservation?> FetchLatestAsync(HttpClient client, string seriesId, CancellationToken ct)
    {
        var url =
            $"series/observations?series_id={Uri.EscapeDataString(seriesId)}" +
            $"&api_key={Uri.EscapeDataString(Fred.ApiKey)}" +
            $"&file_type=json&sort_order=desc&limit={Fred.ObservationLimit}";

        try
        {
            using var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "FRED returned {Status} for series {Series} — rates axis unavailable this run",
                    (int)response.StatusCode, seriesId);
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            return YieldObservation.Latest(Parse(body));
        }
        catch (FredParseException ex)
        {
            logger.LogWarning(ex, "FRED body for series {Series} was not the documented contract", seriesId);
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "FRED fetch failed for series {Series} — rates axis unavailable this run", seriesId);
            return null;
        }
    }

    /// <summary>
    /// Parses a FRED <c>series/observations</c> JSON body into dated numeric yields. Public + static
    /// so the contract test asserts the documented shape. Skips "." placeholders and any non-numeric
    /// value; throws <see cref="FredParseException"/> on a structurally-broken body (no
    /// <c>observations</c> array — markup drift / challenge page), consistent with the Finnhub
    /// loud-on-broken-body precedent.
    /// </summary>
    public static IReadOnlyList<YieldObservation> Parse(string json)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new FredParseException($"FRED body is not JSON ({ex.Message}).");
        }

        using (doc)
        {
            if (!doc.RootElement.TryGetProperty("observations", out var observations) ||
                observations.ValueKind is not JsonValueKind.Array)
            {
                throw new FredParseException("FRED body has no 'observations' array — contract drift.");
            }

            var result = new List<YieldObservation>();
            foreach (var obs in observations.EnumerateArray())
            {
                if (obs.ValueKind is not JsonValueKind.Object ||
                    !obs.TryGetProperty("date", out var dateEl) ||
                    !obs.TryGetProperty("value", out var valueEl) ||
                    dateEl.ValueKind is not JsonValueKind.String ||
                    valueEl.ValueKind is not JsonValueKind.String)
                {
                    continue;
                }

                var raw = valueEl.GetString();
                if (raw is null || raw == "." ||
                    !decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ||
                    !DateOnly.TryParse(dateEl.GetString(), CultureInfo.InvariantCulture, out var date))
                {
                    continue;
                }

                result.Add(new YieldObservation(date, value));
            }

            return result;
        }
    }
}

/// <summary>Thrown when a FRED response body does not match the documented JSON contract.</summary>
public sealed class FredParseException(string message) : Exception(message);
