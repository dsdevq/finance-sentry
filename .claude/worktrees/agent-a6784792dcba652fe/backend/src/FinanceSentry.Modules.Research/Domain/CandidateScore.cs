namespace FinanceSentry.Modules.Research.Domain;

using FinanceSentry.Modules.Research.Domain.Opportunity;
using FinanceSentry.Modules.Research.Domain.Scoring;

/// <summary>
/// A single append-only score snapshot for an <see cref="OpportunityCandidate"/>. Re-scoring
/// appends a new row rather than mutating a prior one, so score history is auditable. There is
/// deliberately no composite/aggregate column (FR-007).
/// </summary>
public class CandidateScore
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CandidateId { get; set; }
    public DateTimeOffset ScoredAt { get; set; } = DateTimeOffset.UtcNow;
    public int? StructureScore { get; set; }
    public int? FundamentalsScore { get; set; }
    public CrowdingClass CrowdingClass { get; set; } = CrowdingClass.Normal;
    public IpsFitFacts IpsFit { get; set; } = IpsFitFacts.Unknown;
    public ScoreEvidence Evidence { get; set; } = ScoreEvidence.Empty;
    public int FormulaVersion { get; set; } = 1;
}
