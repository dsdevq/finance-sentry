namespace FinanceSentry.Modules.Research.Application.Services;

using System.Text.Json;
using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Repositories;
using Microsoft.Extensions.Logging;

public class YahooMarketDataService(
    IHttpClientFactory httpFactory,
    IQuoteCacheRepository cache,
    ILogger<YahooMarketDataService> logger) : IMarketDataService
{
    public const string HttpClientName = "yahoo-finance";

    private const int MaxConcurrentFetches = 6;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlyDictionary<string, QuoteCacheEntry>> GetQuotesAsync(
        IReadOnlyCollection<string> tickers, CancellationToken ct = default)
    {
        if (tickers.Count == 0)
        {
            return new Dictionary<string, QuoteCacheEntry>();
        }

        var normalized = tickers
            .Select(t => t.Trim().ToUpperInvariant())
            .Where(t => t.Length > 0)
            .Distinct()
            .ToArray();

        var cached = await cache.GetFreshAsync(normalized, CacheTtl, ct);
        var missing = normalized.Where(t => !cached.ContainsKey(t)).ToArray();

        if (missing.Length == 0)
        {
            return cached;
        }

        var fetched = await FetchManyAsync(missing, ct);

        if (fetched.Count > 0)
        {
            await cache.UpsertManyAsync(fetched.Values.ToArray(), ct);
        }

        var result = new Dictionary<string, QuoteCacheEntry>(cached);
        foreach (var (ticker, entry) in fetched)
        {
            result[ticker] = entry;
        }

        return result;
    }

    private async Task<Dictionary<string, QuoteCacheEntry>> FetchManyAsync(
        IReadOnlyCollection<string> tickers, CancellationToken ct)
    {
        var result = new Dictionary<string, QuoteCacheEntry>();
        using var throttle = new SemaphoreSlim(MaxConcurrentFetches);

        var tasks = tickers.Select(async ticker =>
        {
            await throttle.WaitAsync(ct);
            try
            {
                return await FetchOneAsync(ticker, ct);
            }
            finally
            {
                throttle.Release();
            }
        });

        foreach (var entry in await Task.WhenAll(tasks))
        {
            if (entry is not null)
            {
                result[entry.Ticker] = entry;
            }
        }

        return result;
    }

    private async Task<QuoteCacheEntry?> FetchOneAsync(string ticker, CancellationToken ct)
    {
        var client = httpFactory.CreateClient(HttpClientName);
        var url = $"/v8/finance/chart/{Uri.EscapeDataString(ticker)}?interval=1d&range=5d";

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
                return null;
            }

            var first = resultArray[0];
            if (!first.TryGetProperty("meta", out var meta))
            {
                return null;
            }

            var symbol = meta.TryGetProperty("symbol", out var symbolProp)
                ? symbolProp.GetString() ?? ticker
                : ticker;
            var price = ReadDecimal(meta, "regularMarketPrice");
            if (price is null)
            {
                return null;
            }

            var prev = ReadDecimal(meta, "chartPreviousClose") ?? ReadDecimal(meta, "previousClose");
            var currency = meta.TryGetProperty("currency", out var currencyProp)
                ? currencyProp.GetString() ?? "USD"
                : "USD";

            return new QuoteCacheEntry
            {
                Ticker = symbol.ToUpperInvariant(),
                Price = price.Value,
                PreviousClose = prev,
                Currency = currency,
                FetchedAt = DateTimeOffset.UtcNow,
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Yahoo Finance quote fetch failed for {Ticker}", ticker);
            return null;
        }
    }

    public async Task<IReadOnlyList<DailyClose>> GetDailyClosesAsync(
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
                quoteArray.GetArrayLength() == 0 ||
                !quoteArray[0].TryGetProperty("close", out var closes) ||
                closes.ValueKind is not JsonValueKind.Array)
            {
                return [];
            }

            var result = new List<DailyClose>();
            var count = Math.Min(timestamps.GetArrayLength(), closes.GetArrayLength());
            for (var i = 0; i < count; i++)
            {
                var closeElement = closes[i];
                if (closeElement.ValueKind is not JsonValueKind.Number ||
                    !closeElement.TryGetDecimal(out var close))
                {
                    continue;
                }

                var epochSeconds = timestamps[i].GetInt64();
                var date = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(epochSeconds).UtcDateTime);
                if (date < since)
                {
                    continue;
                }

                result.Add(new DailyClose(date, close));
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Yahoo Finance daily close fetch failed for {Ticker}", normalized);
            return [];
        }
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

    private static decimal? ReadDecimal(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var prop))
        {
            return null;
        }

        return prop.ValueKind switch
        {
            JsonValueKind.Number => prop.TryGetDecimal(out var value) ? value : null,
            _ => null,
        };
    }
}
