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

    /// <summary>
    /// Fetches incremental transaction updates via /transactions/sync.
    /// Pass null cursor for the initial full sync; pass the previous NextCursor for incremental updates.
    /// Automatically paginates until has_more = false.
    /// </summary>
    Task<PlaidSyncResponse> SyncTransactionsAsync(
        string accessToken, string? cursor = null, int count = 500, CancellationToken ct = default);

    /// <summary>Revokes access token and unlinks the Plaid item.</summary>
    Task RevokeAccessAsync(string accessToken, CancellationToken ct = default);

    /// <summary>
    /// Returns recurring outflow streams (subscriptions, bills) via /transactions/recurring/get.
    /// Streams with status TOMBSTONED (cancelled by Plaid) are excluded.
    /// </summary>
    Task<PlaidRecurringResponse> GetRecurringTransactionsAsync(string accessToken, CancellationToken ct = default);
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

public record PlaidSyncResponse(
    IReadOnlyList<PlaidTransaction> Added,
    IReadOnlyList<PlaidTransaction> Modified,
    IReadOnlyList<string> Removed,
    string NextCursor,
    bool HasMore,
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

public record PlaidRecurringResponse(
    IReadOnlyList<PlaidRecurringStream> OutflowStreams);

public record PlaidRecurringStream(
    string StreamId,
    string AccountId,
    string Description,
    string? MerchantName,
    string? PersonalFinanceCategory,
    string? FirstDate,
    string? LastDate,
    string Frequency,
    IReadOnlyList<string> TransactionIds,
    decimal AverageAmount,
    string? IsoCurrencyCode,
    decimal LastAmount,
    string Status);
