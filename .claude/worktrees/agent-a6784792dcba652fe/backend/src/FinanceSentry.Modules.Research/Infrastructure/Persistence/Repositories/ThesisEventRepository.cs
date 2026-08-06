namespace FinanceSentry.Modules.Research.Infrastructure.Persistence.Repositories;

using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

public class ThesisEventRepository(ResearchDbContext db) : IThesisEventRepository
{
    public async Task AppendAsync(ThesisEvent thesisEvent, CancellationToken ct = default)
    {
        db.ThesisEvents.Add(thesisEvent);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ThesisEvent>> ListAsync(
        Guid userId, Guid? subjectId = null, CancellationToken ct = default)
    {
        var query = db.ThesisEvents.AsNoTracking().Where(e => e.UserId == userId);

        if (subjectId is { } id)
        {
            query = query.Where(e => e.SubjectId == id);
        }

        return await query.OrderBy(e => e.Timestamp).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ThesisEvent>> ListPendingAsync(CancellationToken ct = default)
        => await db.ThesisEvents
            .Where(e => e.PricesPending)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ThesisEvent>> ListForPeriodAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var fromTimestamp = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toTimestamp = new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        return await db.ThesisEvents.AsNoTracking()
            .Where(e => e.UserId == userId && e.Timestamp >= fromTimestamp && e.Timestamp <= toTimestamp)
            .OrderBy(e => e.Timestamp)
            .ToListAsync(ct);
    }

    public Task<ThesisEvent?> GetLatestForSubjectAsync(
        ThesisSubjectType subjectType, Guid subjectId, CancellationToken ct = default)
        => db.ThesisEvents.AsNoTracking()
            .Where(e => e.SubjectType == subjectType && e.SubjectId == subjectId)
            .OrderByDescending(e => e.Timestamp)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Guid>> GetUserIdsWithEventsAsync(CancellationToken ct = default)
        => await db.ThesisEvents.AsNoTracking()
            .Select(e => e.UserId)
            .Distinct()
            .ToListAsync(ct);

    public async Task UpdatePricesAsync(ThesisEvent thesisEvent, CancellationToken ct = default)
    {
        var existing = await db.ThesisEvents.FirstOrDefaultAsync(e => e.Id == thesisEvent.Id, ct);
        if (existing is null)
        {
            return;
        }

        existing.SubjectPrice = thesisEvent.SubjectPrice;
        existing.BenchmarkPrice = thesisEvent.BenchmarkPrice;
        existing.PricesPending = thesisEvent.PricesPending;

        await db.SaveChangesAsync(ct);
    }
}
