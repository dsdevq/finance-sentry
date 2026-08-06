namespace FinanceSentry.Modules.BankSync.API.Responses;

public record TransactionPageResponse(
    string AccountId,
    string BankName,
    string Currency,
    IReadOnlyList<TransactionDto> Items,
    int TotalCount,
    int Offset,
    int Limit,
    bool HasMore);
