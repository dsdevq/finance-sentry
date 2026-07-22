namespace FinanceSentry.Modules.Research.Infrastructure.Jobs;

using System.Security.Cryptography;
using System.Text;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Research.Application.Services;
using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Repositories;
using FinanceSentry.Modules.Research.Infrastructure.Persistence;
using FinanceSentry.Modules.Research.Infrastructure.Sources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public sealed class NewsIngestionJob(
    ResearchDbContext research,
    IBrokerageHoldingsReader brokerage,
    ICryptoHoldingsReader crypto,
    IMarketNewsService news,
    INewsSourceRepository sourceRepo,
    INewsRepository newsRepo,
    IEnumerable<INewsPageSource> pageSources,
    IAlertGeneratorService alerts,
    IBankingTotalsReader banking,
    ILogger<NewsIngestionJob> logger)
{
    private const int MaxTickersPerRun = 60;

    public async Task IngestTickersAsync(CancellationToken ct = default)
    {
        var tickers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var watchlistTickers = await research.WatchlistItems.AsNoTracking()
            .Select(w => w.Ticker)
            .ToListAsync(ct);
        foreach (var t in watchlistTickers)
        {
            tickers.Add(t);
        }

        var thesisTickers = await research.Theses.AsNoTracking()
            .Select(t => t.Ticker)
            .ToListAsync(ct);
        foreach (var t in thesisTickers)
        {
            tickers.Add(t);
        }

        var userIds = await research.WatchlistItems.AsNoTracking()
            .Select(w => w.UserId)
            .Union(research.Theses.AsNoTracking().Select(t => t.UserId))
            .Distinct()
            .ToListAsync(ct);

        foreach (var userId in userIds)
        {
            foreach (var h in await brokerage.GetHoldingsAsync(userId, ct))
            {
                tickers.Add(h.Symbol);
            }

            foreach (var h in await crypto.GetHoldingsAsync(userId, ct))
            {
                tickers.Add(h.Asset + "-USD");
            }
        }

        var toFetch = tickers.Take(MaxTickersPerRun).ToArray();
        var inserted = await news.IngestForTickersAsync(toFetch, ct);
        logger.LogInformation("NewsIngestionJob (tickers) ingested {Count} new articles across {Tickers} tickers", inserted, toFetch.Length);

        // Registered market-wide + thesis sources ride the same 30-min cadence (feature 030, R9).
        await IngestRegisteredSourcesAsync(ct);
    }

    public async Task IngestFedAsync(CancellationToken ct = default)
    {
        var inserted = await news.IngestFedPressAsync(ct);
        logger.LogInformation("NewsIngestionJob (fed) ingested {Count} new articles", inserted);
    }

    /// <summary>
    /// Iterates the enabled <c>news_sources</c> registry (feature 030, FR-007/FR-008). RSS feeds go
    /// through the existing parser; Page sources through their <see cref="INewsPageSource"/>. Articles
    /// are tagged with the source's thesis, and each source tracks its own consecutive failures — a
    /// sync-failure alert fires at the threshold while other sources keep going (FR-009).
    /// </summary>
    public async Task IngestRegisteredSourcesAsync(CancellationToken ct = default)
    {
        var sources = await sourceRepo.ListEnabledAsync(ct);
        foreach (var source in sources)
        {
            await IngestSourceAsync(source, ct);
        }
    }

    private async Task IngestSourceAsync(NewsSource source, CancellationToken ct)
    {
        try
        {
            var articles = source.Kind == NewsSourceKind.Rss
                ? await FetchRssAsync(source, ct)
                : await FetchPageAsync(source, ct);

            foreach (var article in articles)
            {
                article.ThesisIds = NewsSourceTagging.ResolveThesisIds(source, article.Title, article.Summary).ToList();
            }

            var inserted = await newsRepo.InsertNewAsync(articles, ct);
            NewsSourceHealthTracker.RecordSuccess(source);
            await sourceRepo.UpdateAsync(source, ct);
            logger.LogInformation(
                "News source {Source}: {Fetched} fetched, {Inserted} new", source.Name, articles.Count, inserted);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var shouldAlert = NewsSourceHealthTracker.RecordFailure(source, ex.Message);
            await sourceRepo.UpdateAsync(source, ct);
            logger.LogError(ex,
                "News source {Source} failed ({Consecutive} consecutive)", source.Name, source.ConsecutiveFailures);

            if (shouldAlert)
            {
                await RaiseFailureAlertAsync(source.Name, ex.Message, ct);
            }
        }
    }

    private async Task<IReadOnlyList<NewsArticle>> FetchRssAsync(NewsSource source, CancellationToken ct)
        => await news.FetchFeedArticlesAsync(source.Url, $"src:{source.Name}", ct);

    private async Task<IReadOnlyList<NewsArticle>> FetchPageAsync(NewsSource source, CancellationToken ct)
    {
        var pageSource = pageSources.FirstOrDefault(p => p.CanHandle(source.Url))
            ?? throw new NewsSourceParseException(
                $"No page source registered to handle '{source.Url}'.");

        var candidates = await pageSource.FetchAsync(source.Url, ct);
        return candidates
            .Select(c => new NewsArticle
            {
                Source = $"src:{source.Name}",
                Title = Trim(c.Title, 500),
                Url = Trim(c.Url, 2000),
                Summary = c.Summary is null ? null : Trim(c.Summary, 4000),
                PublishedAt = c.PublishedAt,
                ContentHash = HashContent(c.Url, c.Title),
            })
            .ToList();
    }

    private async Task RaiseFailureAlertAsync(string sourceName, string reason, CancellationToken ct)
    {
        var provider = $"news-source:{sourceName}";
        var userIds = await banking.GetActiveUserIdsAsync(ct);
        foreach (var userId in userIds)
        {
            await alerts.GenerateSyncFailureAlertAsync(userId, provider, null, null, reason, ct);
        }
    }

    private static string HashContent(string url, string title)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{url}\n{title}")));

    private static string Trim(string value, int max)
        => value.Length <= max ? value : value[..max];
}
