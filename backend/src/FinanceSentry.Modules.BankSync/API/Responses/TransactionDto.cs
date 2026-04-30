namespace FinanceSentry.Modules.BankSync.API.Responses;

public record TransactionDto(
    Guid TransactionId,
    Guid AccountId,
    decimal Amount,
    DateTime Date,
    DateTime? PostedDate,
    string Description,
    string? TransactionType,
    string? MerchantCategory,
    bool IsPending,
    DateTime CreatedAt);
