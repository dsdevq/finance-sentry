namespace FinanceSentry.Modules.BankSync.Application.Queries;

using FinanceSentry.Core.Api;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.BankSync.Domain.Repositories;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record GlobalTransactionDto(
    Guid TransactionId,
    Guid AccountId,
    string BankName,
    decimal Amount,
    DateTime Date,
    DateTime? PostedDate,
    string Description,
    string? TransactionType,
    string? MerchantCategory,
    bool IsPending,
    DateTime CreatedAt);

public record AllTransactionsResult(
    IReadOnlyList<GlobalTransactionDto> Transactions,
    int TotalCount,
    bool HasMore,
    int Offset,
    int Limit);

// ── Query ────────────────────────────────────────────────────────────────────

public record GetAllTransactionsQuery(
    Guid UserId,
    PagedRequest Paging,
    DateTime? From = null,
    DateTime? To = null
) : IQuery<AllTransactionsResult>;

// ── Handler ──────────────────────────────────────────────────────────────────

public class GetAllTransactionsQueryHandler(
    ITransactionRepository transactions,
    IBankAccountRepository accounts)
    : IQueryHandler<GetAllTransactionsQuery, AllTransactionsResult>
{
    private readonly ITransactionRepository _transactions = transactions;
    private readonly IBankAccountRepository _accounts = accounts;

    public async Task<AllTransactionsResult> Handle(GetAllTransactionsQuery request, CancellationToken ct)
    {
        var accountList = await _accounts.GetByUserIdAsync(request.UserId, ct);
        var accountMap = accountList.ToDictionary(a => a.Id, a => a.BankName);

        var all = await _transactions.GetByUserIdAsync(request.UserId, ct);

        var filtered = all.Where(t => t.IsActive);

        if (request.From.HasValue)
            filtered = filtered.Where(t => (t.PostedDate ?? t.TransactionDate) >= request.From.Value);
        if (request.To.HasValue)
            filtered = filtered.Where(t => (t.PostedDate ?? t.TransactionDate) <= request.To.Value);

        var ordered = filtered
            .OrderByDescending(t => t.PostedDate ?? t.TransactionDate)
            .ToList();

        var totalCount = ordered.Count;
        var page = ordered.Skip(request.Paging.Offset).Take(request.Paging.Limit).ToList();

        var dtos = page.Select(t => new GlobalTransactionDto(
            t.Id,
            t.AccountId,
            accountMap.GetValueOrDefault(t.AccountId, "Unknown"),
            t.Amount,
            t.TransactionDate,
            t.PostedDate,
            t.Description,
            t.TransactionType,
            t.MerchantCategory,
            t.IsPending,
            t.CreatedAt
        )).ToList();

        return new AllTransactionsResult(dtos, totalCount, request.Paging.Offset + request.Paging.Limit < totalCount, request.Paging.Offset, request.Paging.Limit);
    }
}
