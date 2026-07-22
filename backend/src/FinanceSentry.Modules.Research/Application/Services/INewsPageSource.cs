namespace FinanceSentry.Modules.Research.Application.Services;

/// <summary>
/// A <c>Page</c>-kind news source (feature 030, FR-007): a website with no RSS feed whose article list
/// is scraped into candidates for the shared ingestion pipeline. Each implementation handles one site
/// (e.g. TrendForce → DRAM thesis). Markup drift MUST throw so the failure surfaces (FR-009) rather
/// than silently ingesting nothing.
/// </summary>
public interface INewsPageSource
{
    /// <summary>True when this implementation knows how to scrape the given registered-source URL.</summary>
    bool CanHandle(string url);

    /// <summary>Scrape the page's current article list. Throws on unreachable page or markup drift.</summary>
    Task<IReadOnlyList<NewsPageArticle>> FetchAsync(string url, CancellationToken ct = default);
}

/// <summary>A scraped article candidate before it is stamped with source/thesis and persisted.</summary>
public sealed record NewsPageArticle(string Title, string Url, DateTimeOffset PublishedAt, string? Summary);
