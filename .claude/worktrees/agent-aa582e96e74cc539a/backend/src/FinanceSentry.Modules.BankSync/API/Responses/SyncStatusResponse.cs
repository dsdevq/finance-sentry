namespace FinanceSentry.Modules.BankSync.API.Responses;

public record SyncStatusResponse(
    string Status,
    int TransactionCountFetched,
    int TransactionCountDeduped,
    string? ErrorMessage,
    DateTime? LastSyncTimestamp,
    DateTime? StartedAt,
    DateTime? CompletedAt);
