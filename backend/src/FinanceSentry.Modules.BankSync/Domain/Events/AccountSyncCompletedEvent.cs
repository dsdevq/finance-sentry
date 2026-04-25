namespace FinanceSentry.Modules.BankSync.Domain.Events;

using FinanceSentry.Core.Cqrs;

/// <summary>
/// Published when a sync job finishes (either successfully or with a failure).
/// Status is "success" or "failed". Consumers update account state and notify the user.
/// </summary>
public record AccountSyncCompletedEvent(
    Guid AccountId,
    string Status,
    int TransactionCountFetched,
    string? ErrorMessage) : IEvent;
