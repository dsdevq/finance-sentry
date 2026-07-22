namespace FinanceSentry.Modules.Research.Infrastructure.Sources;

using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using FinanceSentry.Modules.Research.Application.Services;
using Microsoft.Extensions.Logging;

/// <summary>
/// <c>Page</c>-kind news source for the TrendForce press center (feature 030, R2) — the DRAM/memory
/// thesis feed Denys's channels cite that has no RSS. Scrapes the article list with AngleSharp;
/// structural assertions throw <see cref="NewsSourceParseException"/> on markup drift (FR-009).
/// </summary>
public sealed class TrendForcePageSource(
    IHttpClientFactory httpFactory,
    ILogger<TrendForcePageSource> logger) : INewsPageSource
{
    public const string HttpClientName = "trendforce";

    private const string Host = "trendforce.com";

    private static readonly string[] ArticleSelectors =
        ["article", ".press-item", "li.item", "div.item", ".list li"];

    public bool CanHandle(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        uri.Host.Contains(Host, StringComparison.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<NewsPageArticle>> FetchAsync(string url, CancellationToken ct = default)
    {
        var client = httpFactory.CreateClient(HttpClientName);
        using var response = await client.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync(ct);

        var articles = await ParseAsync(html, url, ct);
        logger.LogInformation("TrendForce press center parsed {Count} articles", articles.Count);
        return articles;
    }

    /// <summary>
    /// Parses the press-center article list out of a TrendForce HTML page. Public + static so the
    /// contract test can drive it against a recorded fixture with no HTTP. Throws when no article
    /// nodes are present (markup drift).
    /// </summary>
    public static async Task<IReadOnlyList<NewsPageArticle>> ParseAsync(
        string html, string pageUrl, CancellationToken ct = default)
    {
        var parser = new HtmlParser();
        using var doc = await parser.ParseDocumentAsync(html, ct);

        var nodes = FindArticleNodes(doc);
        if (nodes.Count == 0)
        {
            throw new NewsSourceParseException(
                "TrendForce press-center article list not found — page markup may have changed.");
        }

        var baseUri = Uri.TryCreate(pageUrl, UriKind.Absolute, out var b) ? b : null;
        var articles = new List<NewsPageArticle>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in nodes)
        {
            var anchor = node.QuerySelector("a[href]");
            if (anchor is null)
            {
                continue;
            }

            var href = anchor.GetAttribute("href")?.Trim();
            var title = ExtractTitle(node, anchor);
            if (string.IsNullOrWhiteSpace(href) || string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var absoluteUrl = Resolve(baseUri, href);
            if (!seen.Add(absoluteUrl))
            {
                continue;
            }

            articles.Add(new NewsPageArticle(
                title!.Trim(),
                absoluteUrl,
                ExtractDate(node),
                ExtractSummary(node)));
        }

        return articles;
    }

    private static IReadOnlyList<IElement> FindArticleNodes(IDocument doc)
    {
        foreach (var selector in ArticleSelectors)
        {
            var nodes = doc.QuerySelectorAll(selector)
                .Where(n => n.QuerySelector("a[href]") is not null)
                .ToList();
            if (nodes.Count > 0)
            {
                return nodes;
            }
        }

        return [];
    }

    private static string? ExtractTitle(IElement node, IElement anchor)
    {
        var heading = node.QuerySelector("h1, h2, h3, h4, .title");
        var text = heading?.TextContent ?? anchor.TextContent;
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private static DateTimeOffset ExtractDate(IElement node)
    {
        var timeEl = node.QuerySelector("time[datetime]");
        var raw = timeEl?.GetAttribute("datetime")
            ?? node.QuerySelector("time, .date, .time")?.TextContent;

        return DateTimeOffset.TryParse(raw, out var parsed) ? parsed : DateTimeOffset.UtcNow;
    }

    private static string? ExtractSummary(IElement node)
    {
        var summary = node.QuerySelector("p, .summary, .desc")?.TextContent?.Trim();
        return string.IsNullOrWhiteSpace(summary) ? null : summary;
    }

    private static string Resolve(Uri? baseUri, string href)
    {
        var isAbsolute = Uri.TryCreate(href, UriKind.Absolute, out var absolute);
        if (isAbsolute && absolute!.Scheme is "http" or "https")
        {
            return absolute.ToString();
        }

        // AngleSharp rewrites root-relative hrefs to file:// when parsed without a base — recover the
        // original path and resolve it against the real page URL.
        var relative = isAbsolute && absolute!.Scheme == "file" ? absolute.PathAndQuery : href;
        return baseUri is not null && Uri.TryCreate(baseUri, relative, out var combined)
            ? combined.ToString()
            : href;
    }
}
