namespace FinanceSentry.Modules.Research.Domain.Repositories;

public interface INewsRepository
{
    Task<IReadOnlyList<NewsArticle>> SearchAsync(
        string? query,
        IReadOnlyCollection<string>? tickers,
        Guid? thesisId,
        DateTimeOffset? since,
        int limit,
        CancellationToken ct = default);

    Task<IReadOnlyList<NewsArticle>> GetForTickerAsync(
        string ticker, DateTimeOffset? since, int limit, CancellationToken ct = default);

    Task<int> InsertNewAsync(IReadOnlyCollection<NewsArticle> articles, CancellationToken ct = default);
}
