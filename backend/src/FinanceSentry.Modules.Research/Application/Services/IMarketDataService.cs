namespace FinanceSentry.Modules.Research.Application.Services;

using FinanceSentry.Modules.Research.Domain;

public interface IMarketDataService
{
    Task<IReadOnlyDictionary<string, QuoteCacheEntry>> GetQuotesAsync(
        IReadOnlyCollection<string> tickers, CancellationToken ct = default);
}
