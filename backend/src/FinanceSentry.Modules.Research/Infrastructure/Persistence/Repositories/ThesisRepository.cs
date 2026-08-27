namespace FinanceSentry.Modules.Research.Infrastructure.Persistence.Repositories;

using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

public class ThesisRepository(ResearchDbContext db) : IThesisRepository
{
    public async Task<IReadOnlyList<InvestmentThesis>> ListAsync(Guid userId, CancellationToken ct = default)
        => await db.Theses.AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Guid>> GetUserIdsWithThesesAsync(CancellationToken ct = default)
        => await db.Theses.AsNoTracking()
            .Select(t => t.UserId)
            .Distinct()
            .ToListAsync(ct);

    public Task<InvestmentThesis?> FindAsync(Guid userId, Guid id, CancellationToken ct = default)
        => db.Theses.FirstOrDefaultAsync(t => t.UserId == userId && t.Id == id, ct);

    public async Task<IReadOnlyList<InvestmentThesis>> FindByTickerAsync(
        Guid userId, string ticker, CancellationToken ct = default)
        => await db.Theses.AsNoTracking()
            .Where(t => t.UserId == userId && t.Ticker == ticker)
            .ToListAsync(ct);

    public async Task UpsertAsync(InvestmentThesis thesis, CancellationToken ct = default)
    {
        var existing = await db.Theses.FirstOrDefaultAsync(
            t => t.UserId == thesis.UserId && t.Id == thesis.Id, ct);

        if (existing is null)
        {
            db.Theses.Add(thesis);
        }
        else
        {
            existing.ThesisText = thesis.ThesisText;
            existing.Ticker = thesis.Ticker;
            existing.KeyDataPoints = thesis.KeyDataPoints;
            existing.Catalysts = thesis.Catalysts;
            existing.InvalidationTriggers = thesis.InvalidationTriggers;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            existing.EntryPrice = thesis.EntryPrice;
            existing.BrokenAt = thesis.BrokenAt;
            existing.BrokenReason = thesis.BrokenReason;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var affected = await db.Theses
            .Where(t => t.UserId == userId && t.Id == id)
            .ExecuteDeleteAsync(ct);
        return affected > 0;
    }
}
