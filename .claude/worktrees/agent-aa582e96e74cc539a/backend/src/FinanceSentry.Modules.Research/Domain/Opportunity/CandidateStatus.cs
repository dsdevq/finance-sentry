namespace FinanceSentry.Modules.Research.Domain.Opportunity;

/// <summary>Lifecycle status of an <see cref="OpportunityCandidate"/> (FR US1.4/US3).</summary>
public enum CandidateStatus
{
    Active,
    Promoted,
    Rejected,
    Expired,
}
