namespace FinanceSentry.Modules.Research.Infrastructure.Persistence.Repositories;

using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Opportunity;
using FinanceSentry.Modules.Research.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

public sealed class CandidateRepository(ResearchDbContext db) : ICandidateRepository
{
    public async Task<(OpportunityCandidate Candidate, bool IsNew)> UpsertActiveAsync(
        Guid userId, string ticker, CandidateSource source, TimeSpan ttl, CancellationToken ct = default)
    {
        var existing = await FindActiveByTickerAsync(userId, ticker, ct);
        if (existing is not null)
        {
            return (existing, false);
        }

        var candidate = new OpportunityCandidate
        {
            UserId = userId,
            Ticker = ticker,
            Source = source,
            Status = CandidateStatus.Active,
            ExpiresAt = DateTimeOffset.UtcNow + ttl,
        };

        db.OpportunityCandidates.Add(candidate);
        await db.SaveChangesAsync(ct);

        return (candidate, true);
    }

    public Task<OpportunityCandidate?> FindActiveByTickerAsync(Guid userId, string ticker, CancellationToken ct = default)
        => db.OpportunityCandidates
            .FirstOrDefaultAsync(c =>
                c.UserId == userId && c.Ticker == ticker && c.Status == CandidateStatus.Active, ct);

    public Task<OpportunityCandidate?> GetAsync(Guid userId, Guid id, CancellationToken ct = default)
        => db.OpportunityCandidates.FirstOrDefaultAsync(c => c.UserId == userId && c.Id == id, ct);

    public async Task<IReadOnlyList<OpportunityCandidate>> ListAsync(
        Guid userId, CandidateStatus? status = null, CandidateSource? source = null, CancellationToken ct = default)
    {
        var query = db.OpportunityCandidates.AsNoTracking().Where(c => c.UserId == userId);

        if (status is { } s)
        {
            query = query.Where(c => c.Status == s);
        }

        if (source is { } src)
        {
            query = query.Where(c => c.Source == src);
        }

        return await query.OrderByDescending(c => c.CreatedAt).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<OpportunityCandidate>> ListExpiredAsync(DateTimeOffset asOf, CancellationToken ct = default)
        => await db.OpportunityCandidates
            .Where(c => c.Status == CandidateStatus.Active && c.ExpiresAt <= asOf)
            .ToListAsync(ct);

    public async Task UpdateAsync(OpportunityCandidate candidate, CancellationToken ct = default)
    {
        db.OpportunityCandidates.Update(candidate);
        await db.SaveChangesAsync(ct);
    }
}
