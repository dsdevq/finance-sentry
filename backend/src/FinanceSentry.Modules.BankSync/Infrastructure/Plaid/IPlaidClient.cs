namespace FinanceSentry.Modules.BankSync.Infrastructure.Plaid;

/// <summary>
/// Low-level interface for Plaid REST API calls.
/// Implemented by PlaidHttpClient (uses HttpClient). Mocked in unit tests.
/// Separates HTTP concerns from domain mapping concerns (PlaidAdapter).
/// </summary>
public interface IPlaidClient
{
    /// <summary>Creates a Plaid Link token for the frontend to open Plaid Link.</summary>
    Task<PlaidLinkTokenResponse> CreateLinkTokenAsync(string userId, CancellationToken ct = default);

    /// <summary>Exchanges a short-lived public token for a long-lived access token.</summary>
    Task<PlaidExchangeTokenResponse> ExchangePublicTokenAsync(string publicToken, CancellationToken ct = default);

    /// <summary>Returns all accounts and balances for the given access token.</summary>
    Task<PlaidAccountsResponse> GetAccountsAsync(string accessToken, CancellationToken ct = default);

    /// <summary>Returns paginated transactions in the given date range.</summary>
    Task<PlaidTransactionsResponse> GetTransactionsAsync(
        string accessToken, DateTime startDate, DateTime endDate,
        int offset = 0, int count = 500, CancellationToken ct = default);

    /// <summary>Revokes access token and unlinks the Plaid item.</summary>
    Task RevokeAccessAsync(string accessToken, CancellationToken ct = default);
}

// ── Response DTOs (Plaid API shapes) ────────────────────────────────────────

public record PlaidLinkTokenResponse(
    string LinkToken,
    string RequestId,
    DateTime Expiration);

public record PlaidExchangeTokenResponse(
    string AccessToken,
    string ItemId,
    string RequestId);

public record PlaidAccountsResponse(
    IReadOnlyList<PlaidAccount> Accounts,
    string RequestId);

public record PlaidAccount(
    string AccountId,
    string Name,
    string OfficialName,
    string Type,
    string Subtype,
    string Mask,
    decimal? CurrentBalance,
    decimal? AvailableBalance,
    string CurrencyCode);

public record PlaidTransactionsResponse(
    IReadOnlyList<PlaidTransaction> Transactions,
    int TotalTransactions,
    string RequestId);

public record PlaidTransaction(
    string TransactionId,
    string AccountId,
    decimal Amount,
    string? IsoCurrencyCode,
    string Name,
    string? MerchantName,
    string? PersonalFinanceCategory,
    DateTime Date,
    DateTime? AuthorizedDate,
    bool Pending);
