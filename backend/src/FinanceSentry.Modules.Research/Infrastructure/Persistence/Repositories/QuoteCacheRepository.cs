namespace FinanceSentry.Modules.Research.Infrastructure.Persistence.Repositories;

using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

public class QuoteCacheRepository(ResearchDbContext db) : IQuoteCacheRepository
{
    public async Task<IReadOnlyDictionary<string, QuoteCacheEntry>> GetFreshAsync(
        IReadOnlyCollection<string> tickers, TimeSpan maxAge, CancellationToken ct = default)
    {
        if (tickers.Count == 0)
        {
            return new Dictionary<string, QuoteCacheEntry>();
        }

        var threshold = DateTimeOffset.UtcNow - maxAge;
        var normalized = tickers.Select(t => t.ToUpperInvariant()).ToArray();

        var rows = await db.QuoteCache.AsNoTracking()
            .Where(q => normalized.Contains(q.Ticker) && q.FetchedAt >= threshold)
            .ToListAsync(ct);

        return rows.ToDictionary(q => q.Ticker, q => q);
    }

    public async Task UpsertManyAsync(IReadOnlyCollection<QuoteCacheEntry> entries, CancellationToken ct = default)
    {
        if (entries.Count == 0)
        {
            return;
        }

        var tickers = entries.Select(e => e.Ticker).ToArray();
        var existing = await db.QuoteCache
            .Where(q => tickers.Contains(q.Ticker))
            .ToDictionaryAsync(q => q.Ticker, ct);

        foreach (var entry in entries)
        {
            if (existing.TryGetValue(entry.Ticker, out var row))
            {
                row.Price = entry.Price;
                row.PreviousClose = entry.PreviousClose;
                row.Currency = entry.Currency;
                row.FetchedAt = entry.FetchedAt;
            }
            else
            {
                db.QuoteCache.Add(entry);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
