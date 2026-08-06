namespace FinanceSentry.Modules.Research.Application.Services;

using FinanceSentry.Modules.Research.Domain;

public interface IMarketNewsService
{
    Task<int> IngestForTickersAsync(IReadOnlyCollection<string> tickers, CancellationToken ct = default);

    Task<int> IngestFedPressAsync(CancellationToken ct = default);

    /// <summary>
    /// Fetch and parse an arbitrary RSS/Atom feed into article candidates (not persisted). Used by the
    /// registered-source ingestion path (feature 030) so it reuses the existing feed parser. Returns
    /// empty and logs on a network/parse failure — the caller decides how to escalate.
    /// </summary>
    Task<IReadOnlyList<NewsArticle>> FetchFeedArticlesAsync(
        string url, string sourceLabel, CancellationToken ct = default);
}
