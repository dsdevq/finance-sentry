namespace FinanceSentry.Modules.BankSync.Application.Queries;

using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Domain.Repositories;
using FinanceSentry.Core.Cqrs;

// ──────────────────────────────────────────────
// Query
// ──────────────────────────────────────────────

/// <summary>
/// Returns all active bank accounts for the authenticated user.
/// Optionally filter by sync status or currency.
/// FR-009: All queries MUST be scoped to the authenticated user.
/// </summary>
public record GetAccountsQuery(
    Guid UserId,
    string? SyncStatusFilter = null,
    string? CurrencyFilter = null
) : IQuery<GetAccountsResult>;

// ──────────────────────────────────────────────
// Result DTO
// ──────────────────────────────────────────────

public record GetAccountsResult(
    IReadOnlyList<BankAccountDto> Accounts,
    int TotalCount,
    IReadOnlyDictionary<string, decimal> CurrencyTotals
);

public record BankAccountDto(
    Guid AccountId,
    string BankName,
    string AccountType,
    string AccountNumberLast4,
    string Currency,
    decimal? CurrentBalance,
    decimal? AvailableBalance,
    string SyncStatus,
    DateTime? LastSyncTimestamp,
    DateTime CreatedAt,
    string Provider
);

// ──────────────────────────────────────────────
// Handler
// ──────────────────────────────────────────────

public class GetAccountsQueryHandler(IBankAccountRepository accounts) : IQueryHandler<GetAccountsQuery, GetAccountsResult>
{
    private readonly IBankAccountRepository _accounts = accounts;

    public async Task<GetAccountsResult> Handle(
          GetAccountsQuery request, CancellationToken cancellationToken)
    {
        var accounts = await _accounts.GetByUserIdAsync(request.UserId, cancellationToken);

        // Apply optional filters
        IEnumerable<BankAccount> filtered = accounts;

        if (!string.IsNullOrWhiteSpace(request.SyncStatusFilter))
            filtered = filtered.Where(a => a.SyncStatus == request.SyncStatusFilter);

        if (!string.IsNullOrWhiteSpace(request.CurrencyFilter))
            filtered = filtered.Where(a => a.Currency == request.CurrencyFilter.ToUpperInvariant());

        var list = filtered.ToList();

        var dtos = list.Select(a => new BankAccountDto(
            a.Id,
            a.BankName,
            a.AccountType,
            a.AccountNumberLast4,
            a.Currency,
            a.CurrentBalance,
            null,
            a.SyncStatus,
            null,
            a.CreatedAt,
            a.Provider
        )).ToList();

        // Aggregate current balances by currency (only accounts with known balance)
        var currencyTotals = list
            .Where(a => a.CurrentBalance.HasValue)
            .GroupBy(a => a.Currency)
            .ToDictionary(g => g.Key, g => g.Sum(a => a.CurrentBalance!.Value));

        return new GetAccountsResult(dtos, dtos.Count, currencyTotals);
    }
}
