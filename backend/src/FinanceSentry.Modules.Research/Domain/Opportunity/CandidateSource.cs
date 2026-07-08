namespace FinanceSentry.Modules.Research.Domain.Opportunity;

/// <summary>How a candidate entered the pipeline. <c>Scan</c> is reserved for US2 (v2), unused in v1.</summary>
public enum CandidateSource
{
    User,
    Scan,
}
