namespace FinanceSentry.Modules.Analytics.Domain;

/// <summary>Terminal disposition of an analytics query (feature 033, FR-008).</summary>
public enum QueryOutcome
{
    /// <summary>Passed the guard and ran on the read-only connection.</summary>
    Executed,

    /// <summary>Blocked by the SQL guard (not a single SELECT / forbidden keyword) — never reached the database.</summary>
    Rejected,
}
