namespace FinanceSentry.Modules.Research.Infrastructure.Sources;

using System.Text.Json;
using FinanceSentry.Modules.Research.Application.Services;
using FinanceSentry.Modules.Research.Domain;
using Microsoft.Extensions.Logging;

/// <summary>
/// Per-ticker analyst-actions source from Yahoo's <c>quoteSummary/upgradeDowngradeHistory</c> module
/// (feature 030, R1). Structured JSON on infrastructure we already run (crumb + cookie pattern);
/// corroborates the MarketBeat sweep and backfills tickers in the universe. This module carries
/// ratings (from/to grades) but no price targets. Registered as a singleton so the crumb is reused.
/// </summary>
public sealed class YahooAnalystActionsSource(
    IHttpClientFactory httpFactory,
    ILogger<YahooAnalystActionsSource> logger) : IAnalystActionsSource
{
    public const string HttpClientName = "yahoo-analyst";

    private const string CookieSeedUrl = "https://fc.yahoo.com/";
    private const string CrumbUrl = "https://query1.finance.yahoo.com/v1/test/getcrumb";

    private const int MaxConcurrentFetches = 6;
    private const int LookbackDays = 45;

    private static readonly TimeSpan CrumbTtl = TimeSpan.FromMinutes(30);

    private readonly SemaphoreSlim crumbLock = new(1, 1);

    private string? crumb;
    private DateTimeOffset crumbFetchedAt;

    public string SourceName => "yahoo";

    public async Task<IReadOnlyList<AnalystActionRecord>> FetchAsync(
        IReadOnlyCollection<string> universe, CancellationToken ct = default)
    {
        var tickers = universe
            .Select(t => t.Trim().ToUpperInvariant())
            .Where(t => t.Length > 0)
            .Distinct()
            .ToArray();

        if (tickers.Length == 0)
        {
            return [];
        }

        var client = httpFactory.CreateClient(HttpClientName);
        var crumbValue = await GetCrumbAsync(client, forceRefresh: false, ct);
        if (crumbValue is null)
        {
            throw new AnalystSourceParseException("Yahoo crumb acquisition failed — cannot fetch analyst actions.");
        }

        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-LookbackDays));
        using var throttle = new SemaphoreSlim(MaxConcurrentFetches);

        var tasks = tickers.Select(async ticker =>
        {
            await throttle.WaitAsync(ct);
            try
            {
                return await FetchTickerAsync(client, ticker, crumbValue, cutoff, ct);
            }
            finally
            {
                throttle.Release();
            }
        });

        var all = (await Task.WhenAll(tasks)).SelectMany(r => r).ToList();
        logger.LogInformation("Yahoo analyst-actions fetched {Count} actions across {Tickers} tickers",
            all.Count, tickers.Length);
        return all;
    }

    private async Task<IReadOnlyList<AnalystActionRecord>> FetchTickerAsync(
        HttpClient client, string ticker, string crumbValue, DateOnly cutoff, CancellationToken ct)
    {
        // Yahoo uses '-' where symbols carry a class suffix (e.g. BRK.B -> BRK-B); querying the dotted
        // form 404s. Normalize for the request but keep the caller's canonical symbol on the record.
        var yahooSymbol = ticker.Replace('.', '-');
        var url = $"https://query1.finance.yahoo.com/v10/finance/quoteSummary/{Uri.EscapeDataString(yahooSymbol)}"
            + $"?modules=upgradeDowngradeHistory&crumb={Uri.EscapeDataString(crumbValue)}";

        try
        {
            using var response = await client.GetAsync(url, ct);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var refreshed = await GetCrumbAsync(client, forceRefresh: true, ct);
                if (refreshed is null)
                {
                    return [];
                }

                return await FetchTickerAsync(client, ticker, refreshed, cutoff, ct);
            }

            // Delisted, unknown, or coverage-less symbols (and Yahoo's intermittent anti-scrape 404s)
            // are expected for a broad universe — skip quietly rather than logging a warning per ticker.
            if (response.StatusCode is System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.TooManyRequests)
            {
                logger.LogDebug("Yahoo analyst-actions returned {Status} for {Ticker} — no data", (int)response.StatusCode, ticker);
                return [];
            }

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(ct);
            return Parse(json, ticker, url).Where(r => r.ActionDate >= cutoff).ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Yahoo analyst-actions fetch failed for {Ticker}", ticker);
            return [];
        }
    }

    /// <summary>
    /// Parses the <c>upgradeDowngradeHistory</c> module JSON for one ticker. Public + static so the
    /// contract test can assert the JSON shape against a recorded fixture.
    /// </summary>
    public static IReadOnlyList<AnalystActionRecord> Parse(string json, string ticker, string? sourceUrl = null)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("quoteSummary", out var summary) ||
            !summary.TryGetProperty("result", out var results) ||
            results.ValueKind is not JsonValueKind.Array ||
            results.GetArrayLength() == 0 ||
            !results[0].TryGetProperty("upgradeDowngradeHistory", out var udh) ||
            !udh.TryGetProperty("history", out var history) ||
            history.ValueKind is not JsonValueKind.Array)
        {
            return [];
        }

        var upper = ticker.Trim().ToUpperInvariant();
        var records = new List<AnalystActionRecord>();

        foreach (var item in history.EnumerateArray())
        {
            var firm = ReadString(item, "firm");
            if (string.IsNullOrWhiteSpace(firm))
            {
                continue;
            }

            var date = ReadEpochDate(item, "epochGradeDate");
            if (date is null)
            {
                continue;
            }

            var fromGrade = ReadString(item, "fromGrade");
            var toGrade = ReadString(item, "toGrade");
            var actionType = MapAction(ReadString(item, "action"));

            records.Add(new AnalystActionRecord(
                upper,
                firm!.Trim(),
                actionType,
                string.IsNullOrWhiteSpace(fromGrade) ? null : fromGrade!.Trim(),
                string.IsNullOrWhiteSpace(toGrade) ? null : toGrade!.Trim(),
                null,
                null,
                date.Value,
                sourceUrl));
        }

        return records;
    }

    // Yahoo action codes: "up" | "down" | "init" | "main" | "reit".
    private static AnalystActionType MapAction(string? action) => action?.Trim().ToLowerInvariant() switch
    {
        "up" => AnalystActionType.Upgrade,
        "down" => AnalystActionType.Downgrade,
        "init" => AnalystActionType.Initiate,
        _ => AnalystActionType.Reiterate,
    };

    private static string? ReadString(JsonElement parent, string property)
        => parent.TryGetProperty(property, out var el) && el.ValueKind is JsonValueKind.String ? el.GetString() : null;

    private static DateOnly? ReadEpochDate(JsonElement parent, string property)
    {
        if (!parent.TryGetProperty(property, out var el) ||
            el.ValueKind is not JsonValueKind.Number ||
            !el.TryGetInt64(out var epoch))
        {
            return null;
        }

        return DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(epoch).UtcDateTime);
    }

    private async Task<string?> GetCrumbAsync(HttpClient client, bool forceRefresh, CancellationToken ct)
    {
        if (!forceRefresh && crumb is not null && DateTimeOffset.UtcNow - crumbFetchedAt < CrumbTtl)
        {
            return crumb;
        }

        await crumbLock.WaitAsync(ct);
        try
        {
            if (!forceRefresh && crumb is not null && DateTimeOffset.UtcNow - crumbFetchedAt < CrumbTtl)
            {
                return crumb;
            }

            try
            {
                using var seed = await client.GetAsync(CookieSeedUrl, ct);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Yahoo cookie seed request failed (non-fatal)");
            }

            using var response = await client.GetAsync(CrumbUrl, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Yahoo getcrumb returned {Status}", (int)response.StatusCode);
                return null;
            }

            var value = (await response.Content.ReadAsStringAsync(ct)).Trim();
            if (string.IsNullOrEmpty(value) || value.Contains('<'))
            {
                logger.LogWarning("Yahoo getcrumb returned an unexpected body");
                return null;
            }

            crumb = value;
            crumbFetchedAt = DateTimeOffset.UtcNow;
            return crumb;
        }
        finally
        {
            crumbLock.Release();
        }
    }
}
