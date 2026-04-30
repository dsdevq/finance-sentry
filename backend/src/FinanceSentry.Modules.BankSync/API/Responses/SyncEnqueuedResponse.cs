namespace FinanceSentry.Modules.BankSync.API.Responses;

public record SyncEnqueuedResponse(string JobId, string Message);
