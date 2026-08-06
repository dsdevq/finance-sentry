namespace FinanceSentry.Modules.BankSync.API.Responses;

public record LinkTokenResponse(string LinkToken, int ExpiresIn, string RequestId);
