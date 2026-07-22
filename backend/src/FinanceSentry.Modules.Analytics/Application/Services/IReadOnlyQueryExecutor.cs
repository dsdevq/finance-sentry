namespace FinanceSentry.Modules.Analytics.Application.Services;

/// <summary>Result of running a validated SELECT on the read-only connection.</summary>
/// <param name="Columns">Result column names in order.</param>
/// <param name="Rows">Row values (already capped at <c>MaxRows</c>).</param>
/// <param name="Truncated">True when the row cap clipped additional rows.</param>
/// <param name="TooLarge">True when a statement timeout / budget overrun stopped the query.</param>
/// <param name="Reason">Populated when <paramref name="TooLarge"/> is true.</param>
/// <param name="DurationMs">Wall-clock execution time.</param>
public sealed record QueryExecution(
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<object?>> Rows,
    bool Truncated,
    bool TooLarge,
    string? Reason,
    int DurationMs);

/// <summary>
/// Runs a pre-validated SELECT as the <c>fs_readonly</c> role inside a read-only transaction with the
/// caller pinned via <c>app.current_user_id</c> and a statement timeout + row cap (feature 033,
/// FR-002/004/006). No write path exists even if the validator is bypassed.
/// </summary>
public interface IReadOnlyQueryExecutor
{
    Task<QueryExecution> ExecuteAsync(Guid userId, string sql, CancellationToken ct = default);
}
