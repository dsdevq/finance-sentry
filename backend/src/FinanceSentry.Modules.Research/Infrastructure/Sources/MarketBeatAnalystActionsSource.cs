namespace FinanceSentry.Modules.Research.Infrastructure.Sources;

using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using AngleSharp.Dom;
using FinanceSentry.Modules.Research.Application.Services;
using FinanceSentry.Modules.Research.Domain;
using Microsoft.Extensions.Logging;

/// <summary>
/// Market-wide analyst-actions sweep from MarketBeat's daily ratings page — one HTML table listing
/// the day's upgrades/downgrades/target changes across the whole market, with price targets
/// (feature 030, R1). Structural assertions throw <see cref="AnalystSourceParseException"/> on
/// markup drift so the failure is visible (FR-009). Ignores the per-ticker universe (market-wide).
/// </summary>
public sealed partial class MarketBeatAnalystActionsSource(
    IHttpClientFactory httpFactory,
    ILogger<MarketBeatAnalystActionsSource> logger) : IAnalystActionsSource
{
    public const string HttpClientName = "marketbeat";
    private const string RatingsUrl = "https://www.marketbeat.com/ratings/";

    private static readonly Regex TickerToken = TickerRegex();

    public string SourceName => "marketbeat";

    public async Task<IReadOnlyList<AnalystActionRecord>> FetchAsync(
        IReadOnlyCollection<string> universe, CancellationToken ct = default)
    {
        var client = httpFactory.CreateClient(HttpClientName);
        using var response = await client.GetAsync(RatingsUrl, ct);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync(ct);

        var records = await ParseAsync(html, RatingsUrl, ct);
        logger.LogInformation("MarketBeat ratings sweep parsed {Count} actions", records.Count);
        return records;
    }

    /// <summary>
    /// Parses the ratings table out of a MarketBeat HTML page. Public + static so the contract test
    /// can drive it against a recorded fixture with no HTTP. Throws if the expected table/columns are
    /// absent (markup drift).
    /// </summary>
    public static async Task<IReadOnlyList<AnalystActionRecord>> ParseAsync(
        string html, string? sourceUrl, CancellationToken ct = default)
    {
        var parser = new HtmlParser();
        using var doc = await parser.ParseDocumentAsync(html, ct);

        var table = FindRatingsTable(doc)
            ?? throw new AnalystSourceParseException(
                "MarketBeat ratings table not found — page markup may have changed.");

        var headers = ReadHeaderMap(table);
        RequireColumn(headers, "company");
        RequireColumn(headers, "action");
        RequireColumn(headers, "brokerage");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var records = new List<AnalystActionRecord>();

        foreach (var row in table.QuerySelectorAll("tbody > tr"))
        {
            var cells = row.QuerySelectorAll("td");
            if (cells.Length == 0)
            {
                continue;
            }

            var ticker = ExtractTicker(CellAt(cells, headers, "company"));
            if (ticker is null)
            {
                continue;
            }

            var actionText = CellText(cells, headers, "action");
            var brokerage = ExtractFirm(CellAt(cells, headers, "brokerage"));
            if (string.IsNullOrWhiteSpace(brokerage))
            {
                continue;
            }

            var (priorRating, newRating) = SplitPair(CellText(cells, headers, "rating"));
            var (priorTarget, newTarget) = SplitTargets(CellText(cells, headers, "target"));
            var actionType = ClassifyAction(actionText, priorRating, newRating, priorTarget, newTarget);

            records.Add(new AnalystActionRecord(
                ticker,
                NormalizeFirm(brokerage),
                actionType,
                priorRating,
                newRating,
                priorTarget,
                newTarget,
                today,
                sourceUrl));
        }

        return records;
    }

    private static IElement? FindRatingsTable(IDocument doc)
    {
        foreach (var table in doc.QuerySelectorAll("table"))
        {
            var headerText = string.Join(
                " ",
                table.QuerySelectorAll("thead th, thead td, tr:first-child th")
                    .Select(h => h.TextContent.Trim().ToLowerInvariant()));

            if (headerText.Contains("brokerage") && (headerText.Contains("company") || headerText.Contains("ticker")))
            {
                return table;
            }
        }

        return null;
    }

    private static Dictionary<string, int> ReadHeaderMap(IElement table)
    {
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        var headerCells = table.QuerySelectorAll("thead th, thead td");
        if (headerCells.Length == 0)
        {
            headerCells = table.QuerySelectorAll("tr:first-child th, tr:first-child td");
        }

        for (var i = 0; i < headerCells.Length; i++)
        {
            var text = headerCells[i].TextContent.Trim().ToLowerInvariant();
            if (text.Contains("company") || text.Contains("ticker"))
            {
                map.TryAdd("company", i);
            }
            else if (text.Contains("action"))
            {
                map.TryAdd("action", i);
            }
            else if (text.Contains("brokerage") || text.Contains("firm") || text.Contains("analyst"))
            {
                map.TryAdd("brokerage", i);
            }
            else if (text.Contains("rating"))
            {
                map.TryAdd("rating", i);
            }
            else if (text.Contains("price target") || text.Contains("target"))
            {
                map.TryAdd("target", i);
            }
        }

        return map;
    }

    private static void RequireColumn(Dictionary<string, int> headers, string key)
    {
        if (!headers.ContainsKey(key))
        {
            throw new AnalystSourceParseException(
                $"MarketBeat ratings table missing expected '{key}' column — page markup may have changed.");
        }
    }

    private static IElement? CellAt(IHtmlCollection<IElement> cells, Dictionary<string, int> headers, string key)
        => headers.TryGetValue(key, out var idx) && idx < cells.Length ? cells[idx] : null;

    private static string CellText(IHtmlCollection<IElement> cells, Dictionary<string, int> headers, string key)
        => CellAt(cells, headers, key)?.TextContent.Trim() ?? string.Empty;

    // The brokerage cell contains the firm-profile link plus an "All Access" promo anchor whose
    // sr-only text ("Subscribe to MarketBeat All Access…") pollutes the cell's TextContent.
    // Prefer the firm-profile link's own text; fall back to stripping the known promo substring.
    private const string PromoMarker = "Subscribe to MarketBeat All Access";

    private static string ExtractFirm(IElement? brokerageCell)
    {
        if (brokerageCell is null)
        {
            return string.Empty;
        }

        // The first by-issuer anchor is the logo image link (whitespace-only text); the firm name
        // is the first by-issuer anchor that actually carries text.
        var firmLink = brokerageCell
            .QuerySelectorAll("a[href*='/ratings/by-issuer/']")
            .FirstOrDefault(a => !string.IsNullOrWhiteSpace(a.TextContent));
        var text = firmLink?.TextContent ?? brokerageCell.TextContent;

        var promoIdx = text.IndexOf(PromoMarker, StringComparison.OrdinalIgnoreCase);
        if (promoIdx >= 0)
        {
            text = text[..promoIdx];
        }

        return text.Trim();
    }

    private static string? ExtractTicker(IElement? companyCell)
    {
        if (companyCell is null)
        {
            return null;
        }

        var badge = companyCell.QuerySelector(".ticker-area, .ticker, [data-ticker]");
        var candidate = badge?.GetAttribute("data-ticker") ?? badge?.TextContent ?? companyCell.TextContent;
        var match = TickerToken.Match(candidate ?? string.Empty);
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }

    // MarketBeat action labels: "Upgrade", "Downgrade", "Initiated Coverage", "Boost Price Target",
    // "Lower Price Target", "Reiterated Rating", etc. Fall back to rating/target deltas when ambiguous.
    private static AnalystActionType ClassifyAction(
        string actionText, string? priorRating, string? newRating, decimal? priorTarget, decimal? newTarget)
    {
        var t = actionText.ToLowerInvariant();
        if (t.Contains("upgrade"))
        {
            return AnalystActionType.Upgrade;
        }

        if (t.Contains("downgrade"))
        {
            return AnalystActionType.Downgrade;
        }

        if (t.Contains("initiat") || t.Contains("assume") || t.Contains("new coverage") || t.Contains("start"))
        {
            return AnalystActionType.Initiate;
        }

        if (t.Contains("target") || (priorTarget is not null && newTarget is not null && priorTarget != newTarget))
        {
            return AnalystActionType.TargetChange;
        }

        return AnalystActionType.Reiterate;
    }

    private static (string? Prior, string? New) SplitPair(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return (null, null);
        }

        var parts = SplitArrow(text);
        if (parts.Length == 2)
        {
            return (Clean(parts[0]), Clean(parts[1]));
        }

        return (null, Clean(text));
    }

    private static (decimal? Prior, decimal? New) SplitTargets(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return (null, null);
        }

        var parts = SplitArrow(text);
        if (parts.Length == 2)
        {
            return (ParseMoney(parts[0]), ParseMoney(parts[1]));
        }

        return (null, ParseMoney(text));
    }

    private static string[] SplitArrow(string text)
        => text.Split(["→", "->", "➝", " to "], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static decimal? ParseMoney(string text)
    {
        var cleaned = text.Replace("$", string.Empty).Replace(",", string.Empty).Trim();
        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    private static string? Clean(string text)
    {
        var trimmed = text.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static string NormalizeFirm(string firm)
        => string.Join(' ', firm.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();

    [GeneratedRegex(@"\b[A-Z]{1,5}(?:\.[A-Z])?\b")]
    private static partial Regex TickerRegex();
}
