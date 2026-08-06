namespace FinanceSentry.Modules.Research.Domain.Repositories;

public interface ICandidateScoreRepository
{
    Task AppendAsync(CandidateScore score, CancellationToken ct = default);

    Task<CandidateScore?> LatestForCandidateAsync(Guid candidateId, CancellationToken ct = default);
}
