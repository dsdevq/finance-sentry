namespace FinanceSentry.Modules.BankSync.API.Responses;

public record TransferItemDto(
    Guid TransactionId,
    Guid AccountId,
    decimal Amount,
    DateTime Date,
    string Description);

public record TransferPairDto(TransferItemDto Debit, TransferItemDto Credit);

public record TransferPairsResponse(IReadOnlyList<TransferPairDto> Transfers, int Count);
