namespace FinanceSentry.Modules.Research.Domain.Opportunity;

/// <summary>How a candidate entered the pipeline.</summary>
public enum CandidateSource
{
    User,
    Scan,

    /// <summary>Nominated by Ledger (the companion advisor agent) from its own research (feature 030).</summary>
    Ledger,
}
