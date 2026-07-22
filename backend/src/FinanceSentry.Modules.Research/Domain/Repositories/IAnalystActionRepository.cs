namespace FinanceSentry.Modules.Research.Domain.Repositories;

using FinanceSentry.Modules.Research.Domain;

public interface IAnalystActionRepository
{
    /// <summary>
    /// Upserts a batch, deduplicating by logical identity (Ticker + Firm + ActionDate + ActionType).
    /// On conflict the richer record is kept — NULL target/rating fields are filled from the incoming
    /// row; an existing populated field is never overwritten. Returns the number of NEW rows inserted.
    /// </summary>
    Task<int> UpsertAsync(IReadOnlyCollection<AnalystAction> actions, CancellationToken ct = default);

    /// <summary>
    /// Query actions filterable by ticker, cutoff date (inclusive), and action type, newest first.
    /// </summary>
    Task<IReadOnlyList<AnalystAction>> QueryAsync(
        string? ticker,
        DateOnly since,
        AnalystActionType? actionType,
        int limit,
        CancellationToken ct = default);
}
