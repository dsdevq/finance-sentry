namespace FinanceSentry.Modules.Research.Domain.Repositories;

public interface IQuoteCacheRepository
{
    Task<IReadOnlyDictionary<string, QuoteCacheEntry>> GetFreshAsync(
        IReadOnlyCollection<string> tickers, TimeSpan maxAge, CancellationToken ct = default);

    Task UpsertManyAsync(IReadOnlyCollection<QuoteCacheEntry> entries, CancellationToken ct = default);
}
