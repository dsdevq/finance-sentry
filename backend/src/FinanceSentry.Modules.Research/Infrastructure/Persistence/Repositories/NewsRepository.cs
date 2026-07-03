namespace FinanceSentry.Modules.Research.Infrastructure.Persistence.Repositories;

using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

public class NewsRepository(ResearchDbContext db) : INewsRepository
{
    private const int MaxLimit = 100;

    private const int PrefilterMultiplier = 5;

    public async Task<IReadOnlyList<NewsArticle>> SearchAsync(
        string? query,
        IReadOnlyCollection<string>? tickers,
        DateTimeOffset? since,
        int limit,
        CancellationToken ct = default)
    {
        var effective = Math.Clamp(limit, 1, MaxLimit);
        var q = db.News.AsNoTracking().AsQueryable();

        if (since is { } s)
        {
            q = q.Where(a => a.PublishedAt >= s);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var like = $"%{query.Trim()}%";
            q = q.Where(a => EF.Functions.ILike(a.Title, like) || EF.Functions.ILike(a.Summary ?? string.Empty, like));
        }

        if (tickers is { Count: > 0 })
        {
            var upper = tickers.Select(t => t.ToUpperInvariant()).ToHashSet(StringComparer.Ordinal);
            var candidates = await q.OrderByDescending(a => a.PublishedAt)
                .Take(effective * PrefilterMultiplier)
                .ToListAsync(ct);
            return candidates
                .Where(a => a.Tickers.Any(upper.Contains))
                .Take(effective)
                .ToList();
        }

        return await q.OrderByDescending(a => a.PublishedAt)
            .Take(effective)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<NewsArticle>> GetForTickerAsync(
        string ticker, DateTimeOffset? since, int limit, CancellationToken ct = default)
    {
        var upper = ticker.ToUpperInvariant();
        var effective = Math.Clamp(limit, 1, MaxLimit);
        var q = db.News.AsNoTracking().AsQueryable();
        if (since is { } s)
        {
            q = q.Where(a => a.PublishedAt >= s);
        }

        var candidates = await q.OrderByDescending(a => a.PublishedAt)
            .Take(effective * PrefilterMultiplier)
            .ToListAsync(ct);

        return candidates
            .Where(a => a.Tickers.Any(t => string.Equals(t, upper, StringComparison.Ordinal)))
            .Take(effective)
            .ToList();
    }

    public async Task<int> InsertNewAsync(IReadOnlyCollection<NewsArticle> articles, CancellationToken ct = default)
    {
        if (articles.Count == 0)
        {
            return 0;
        }

        var deduped = articles
            .GroupBy(a => a.ContentHash, StringComparer.Ordinal)
            .Select(g => g.First())
            .ToList();

        var hashes = deduped.Select(a => a.ContentHash).ToArray();
        var existing = await db.News.AsNoTracking()
            .Where(a => hashes.Contains(a.ContentHash))
            .Select(a => a.ContentHash)
            .ToListAsync(ct);
        var existingSet = existing.ToHashSet(StringComparer.Ordinal);

        var toInsert = deduped.Where(a => !existingSet.Contains(a.ContentHash)).ToList();
        if (toInsert.Count == 0)
        {
            return 0;
        }

        await db.News.AddRangeAsync(toInsert, ct);
        await db.SaveChangesAsync(ct);
        return toInsert.Count;
    }
}
