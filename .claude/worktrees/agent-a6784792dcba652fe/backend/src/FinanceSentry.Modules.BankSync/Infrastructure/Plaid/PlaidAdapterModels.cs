namespace FinanceSentry.Modules.BankSync.Infrastructure.Plaid;

/// <summary>Domain-facing result of CreateLinkToken.</summary>
public record LinkTokenResult(string LinkToken, TimeSpan ExpiresIn, string RequestId);

/// <summary>Domain-facing result of ExchangePublicToken.</summary>
public record ExchangeResult(string AccessToken, string ItemId);

/// <summary>Domain-facing account info from Plaid.</summary>
public record PlaidAccountInfo(
    string PlaidAccountId,
    string Name,
    string AccountType,
    string AccountNumberLast4,
    decimal? CurrentBalance,
    decimal? AvailableBalance,
    string Currency);
