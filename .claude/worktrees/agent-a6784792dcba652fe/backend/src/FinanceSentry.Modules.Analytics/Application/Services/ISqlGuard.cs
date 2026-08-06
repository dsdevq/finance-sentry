namespace FinanceSentry.Modules.Analytics.Application.Services;

/// <summary>Outcome of validating a submitted statement.</summary>
public sealed record SqlGuardResult(bool IsValid, string? Reason)
{
    public static SqlGuardResult Valid { get; } = new(true, null);

    public static SqlGuardResult Invalid(string reason) => new(false, reason);
}

/// <summary>
/// Defense-in-depth validator (feature 033, FR-005): accepts exactly one read-only
/// <c>SELECT</c>/<c>WITH…SELECT</c> and rejects multi-statement, writes, DDL, and data-modifying CTEs
/// BEFORE execution. The <c>fs_readonly</c> role is the primary guarantee; this is the second layer.
/// </summary>
public interface ISqlGuard
{
    SqlGuardResult Validate(string? sql);
}
