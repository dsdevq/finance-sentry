namespace FinanceSentry.Modules.Radar.Infrastructure.MarketData;

using System.Text.Json;
using FinanceSentry.Core.Interfaces;
using Microsoft.Extensions.Logging;

/// <summary>
/// <see cref="IMarketHistorySource"/> over the Yahoo chart endpoint — reads full OHLCV + adjusted
/// close from <c>/v8/finance/chart/{ticker}</c>. On any failure returns an empty list so the caller
/// can isolate the ticker and continue.
/// </summary>
public sealed class YahooMarketHistorySource(
    IHttpClientFactory httpFactory,
    ILogger<YahooMarketHistorySource> logger) : IMarketHistorySource
{
    public const string HttpClientName = "radar-yahoo-finance";

    public async Task<IReadOnlyList<DailyBarData>> GetDailyBarsAsync(
        string ticker, DateOnly since, CancellationToken ct = default)
    {
        var normalized = ticker.Trim().ToUpperInvariant();
        if (normalized.Length == 0)
        {
            return [];
        }

        var range = ResolveRange(since);
        var client = httpFactory.CreateClient(HttpClientName);
        var url = $"/v8/finance/chart/{Uri.EscapeDataString(normalized)}?interval=1d&range={range}";

        try
        {
            using var response = await client.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            if (!doc.RootElement.TryGetProperty("chart", out var chart) ||
                !chart.TryGetProperty("result", out var resultArray) ||
                resultArray.ValueKind is not JsonValueKind.Array ||
                resultArray.GetArrayLength() == 0)
            {
                return [];
            }

            var first = resultArray[0];
            if (!first.TryGetProperty("timestamp", out var timestamps) ||
                timestamps.ValueKind is not JsonValueKind.Array ||
                !first.TryGetProperty("indicators", out var indicators) ||
                !indicators.TryGetProperty("quote", out var quoteArray) ||
                quoteArray.ValueKind is not JsonValueKind.Array ||
                quoteArray.GetArrayLength() == 0)
            {
                return [];
            }

            var quote = quoteArray[0];
            if (!quote.TryGetProperty("open", out var opens) ||
                !quote.TryGetProperty("high", out var highs) ||
                !quote.TryGetProperty("low", out var lows) ||
                !quote.TryGetProperty("close", out var closes) ||
                !quote.TryGetProperty("volume", out var volumes))
            {
                return [];
            }

            JsonElement? adjCloses = null;
            if (indicators.TryGetProperty("adjclose", out var adjArray) &&
                adjArray.ValueKind is JsonValueKind.Array &&
                adjArray.GetArrayLength() > 0 &&
                adjArray[0].TryGetProperty("adjclose", out var adj) &&
                adj.ValueKind is JsonValueKind.Array)
            {
                adjCloses = adj;
            }

            var result = new List<DailyBarData>();
            var count = timestamps.GetArrayLength();
            for (var i = 0; i < count; i++)
            {
                var close = ReadDecimal(closes, i);
                var open = ReadDecimal(opens, i);
                var high = ReadDecimal(highs, i);
                var low = ReadDecimal(lows, i);
                var volume = ReadLong(volumes, i);

                // Skip holidays/gaps where the core OHLC is null.
                if (close is null || open is null || high is null || low is null)
                {
                    continue;
                }

                var epochSeconds = timestamps[i].GetInt64();
                var date = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(epochSeconds).UtcDateTime);
                if (date < since)
                {
                    continue;
                }

                var adjClose = adjCloses is not null ? ReadDecimal(adjCloses.Value, i) ?? close.Value : close.Value;

                result.Add(new DailyBarData(
                    date, open.Value, high.Value, low.Value, close.Value, adjClose, volume ?? 0L));
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Radar Yahoo history fetch failed for {Ticker}", normalized);
            return [];
        }
    }

    private static decimal? ReadDecimal(JsonElement array, int index)
    {
        if (index >= array.GetArrayLength())
        {
            return null;
        }

        var el = array[index];
        return el.ValueKind is JsonValueKind.Number && el.TryGetDecimal(out var value) ? value : null;
    }

    private static long? ReadLong(JsonElement array, int index)
    {
        if (index >= array.GetArrayLength())
        {
            return null;
        }

        var el = array[index];
        return el.ValueKind is JsonValueKind.Number && el.TryGetInt64(out var value) ? value : null;
    }

    private static string ResolveRange(DateOnly since)
    {
        var daysAgo = DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - since.DayNumber;

        return daysAgo switch
        {
            <= 30 => "1mo",
            <= 90 => "3mo",
            <= 180 => "6mo",
            <= 365 => "1y",
            <= 730 => "2y",
            <= 1825 => "5y",
            <= 3650 => "10y",
            _ => "max",
        };
    }
}
