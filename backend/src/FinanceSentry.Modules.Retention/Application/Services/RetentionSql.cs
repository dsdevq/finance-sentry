namespace FinanceSentry.Modules.Retention.Application.Services;

using FinanceSentry.Modules.Retention.Domain;

/// <summary>
/// Builds the raw SQL for the generic purge (feature 024). Table/column identifiers come only from the
/// compiled <see cref="RetentionPolicy"/> and are double-quoted with exact case; the cutoff is always a
/// bound <c>@cutoff</c> parameter — there is no user-controlled input, so no injection surface. Kept
/// separate from the service so the statement shape is unit-testable without a database.
/// </summary>
public static class RetentionSql
{
    /// <summary>Counts rows older than the cutoff (used for the run's <c>examined</c> figure and dry runs).</summary>
    public static string Count(RetentionPolicy policy) =>
        $"SELECT COUNT(*)::bigint FROM {policy.QuotedTable} WHERE {policy.QuotedTimestamp} < @cutoff";

    /// <summary>
    /// Deletes one bounded batch of out-of-policy rows via a <c>ctid</c> sub-select, so lock duration is
    /// capped and a killed run resumes without double-deleting (idempotent — the cutoff is far from now).
    /// </summary>
    public static string PurgeBatch(RetentionPolicy policy, int batchSize) =>
        $"DELETE FROM {policy.QuotedTable} WHERE ctid IN " +
        $"(SELECT ctid FROM {policy.QuotedTable} WHERE {policy.QuotedTimestamp} < @cutoff LIMIT {batchSize})";
}
