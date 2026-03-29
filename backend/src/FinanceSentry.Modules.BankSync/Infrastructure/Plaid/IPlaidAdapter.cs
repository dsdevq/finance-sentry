namespace FinanceSentry.Modules.BankSync.Infrastructure.Plaid;

using FinanceSentry.Modules.BankSync.Application.Services;

/// <summary>
/// Abstraction over the Plaid API surface used by application services.
/// Enables unit-testing without a real Plaid connection.
/// </summary>
public interface IPlaidAdapter
{
    /// <summary>Creates a Plaid Link token for the frontend (step 1 of account connection).</summary>
    Task<LinkTokenResult> CreateLinkTokenAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Exchanges short-lived public token for long-lived access token (step 2).</summary>
    Task<ExchangeResult> ExchangePublicTokenAsync(string publicToken, CancellationToken ct = default);

    /// <summary>Returns domain-mapped accounts with current balances.</summary>
    Task<IReadOnlyList<PlaidAccountInfo>> GetAccountsWithBalanceAsync(
        string accessToken, CancellationToken ct = default);

    /// <summary>
    /// Returns transaction candidates for a given account + user, ready for deduplication.
    /// </summary>
    Task<IReadOnlyList<TransactionCandidate>> GetTransactionsAsync(
        string accessToken, Guid accountId, Guid userId,
        DateTime startDate, DateTime endDate, CancellationToken ct = default);

    /// <summary>Revokes access — called when user disconnects their bank account.</summary>
    Task RevokeAccessAsync(string accessToken, CancellationToken ct = default);
}
