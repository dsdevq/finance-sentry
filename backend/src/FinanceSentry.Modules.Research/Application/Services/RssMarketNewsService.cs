namespace FinanceSentry.Modules.Research.Application.Services;

using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Repositories;
using Microsoft.Extensions.Logging;

public class RssMarketNewsService(
    IHttpClientFactory httpFactory,
    INewsRepository repo,
    ILogger<RssMarketNewsService> logger) : IMarketNewsService
{
    public const string HttpClientName = "market-news";

    private const string FedPressUrl = "https://www.federalreserve.gov/feeds/press_all.xml";

    public async Task<int> IngestForTickersAsync(IReadOnlyCollection<string> tickers, CancellationToken ct = default)
    {
        var articles = new List<NewsArticle>();
        foreach (var ticker in tickers.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            articles.AddRange(await FetchYahooTickerAsync(ticker, ct));
        }

        return await repo.InsertNewAsync(articles, ct);
    }

    public async Task<int> IngestFedPressAsync(CancellationToken ct = default)
    {
        var articles = await FetchFeedAsync(FedPressUrl, "fed-press", ct);
        foreach (var article in articles)
        {
            article.Categories.Add("macro");
            article.Categories.Add("fed");
        }

        return await repo.InsertNewAsync(articles, ct);
    }

    private async Task<List<NewsArticle>> FetchYahooTickerAsync(string ticker, CancellationToken ct)
    {
        var upper = ticker.Trim().ToUpperInvariant();
        var url = $"https://feeds.finance.yahoo.com/rss/2.0/headline?s={Uri.EscapeDataString(upper)}&region=US&lang=en-US";
        var articles = await FetchFeedAsync(url, $"yahoo:{upper}", ct);
        foreach (var article in articles)
        {
            if (!article.Tickers.Contains(upper))
            {
                article.Tickers.Add(upper);
            }
        }

        return articles;
    }

    private async Task<List<NewsArticle>> FetchFeedAsync(string url, string sourceLabel, CancellationToken ct)
    {
        var client = httpFactory.CreateClient(HttpClientName);
        try
        {
            using var response = await client.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            var xml = await response.Content.ReadAsStringAsync(ct);
            var doc = XDocument.Parse(xml);

            var items = doc.Descendants("item").ToList();
            var result = new List<NewsArticle>(items.Count);

            foreach (var item in items)
            {
                var title = item.Element("title")?.Value?.Trim();
                var link = item.Element("link")?.Value?.Trim();
                var description = item.Element("description")?.Value?.Trim();
                var pubDate = item.Element("pubDate")?.Value?.Trim();

                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(link))
                {
                    continue;
                }

                if (!DateTimeOffset.TryParse(pubDate, out var published))
                {
                    published = DateTimeOffset.UtcNow;
                }

                result.Add(new NewsArticle
                {
                    Source = sourceLabel,
                    Title = TrimTo(title, 500),
                    Url = TrimTo(link, 2000),
                    Summary = description is null ? null : TrimTo(description, 4000),
                    PublishedAt = published,
                    ContentHash = HashContent(link, title),
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "RSS fetch failed for {Url}", url);
            return [];
        }
    }

    private static string HashContent(string url, string title)
    {
        var bytes = Encoding.UTF8.GetBytes($"{url}\n{title}");
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static string TrimTo(string value, int max)
        => value.Length <= max ? value : value[..max];
}
