namespace FinanceSentry.Modules.Research.Infrastructure.Persistence.Repositories;

using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

public sealed class CandidateScoreRepository(ResearchDbContext db) : ICandidateScoreRepository
{
    public async Task AppendAsync(CandidateScore score, CancellationToken ct = default)
    {
        db.CandidateScores.Add(score);
        await db.SaveChangesAsync(ct);
    }

    public Task<CandidateScore?> LatestForCandidateAsync(Guid candidateId, CancellationToken ct = default)
        => db.CandidateScores.AsNoTracking()
            .Where(s => s.CandidateId == candidateId)
            .OrderByDescending(s => s.ScoredAt)
            .FirstOrDefaultAsync(ct);
}
