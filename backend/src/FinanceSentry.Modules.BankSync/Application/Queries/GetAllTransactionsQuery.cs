namespace FinanceSentry.Modules.BankSync.Application.Queries;

using FinanceSentry.Core.Api;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Core.Utils;
using FinanceSentry.Modules.BankSync.Domain.Repositories;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record GlobalTransactionDto(
    Guid TransactionId,
    Guid AccountId,
    string BankName,
    string Currency,
    decimal Amount,
    decimal AmountUsd,
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
        var accountMap = accountList.ToDictionary(a => a.Id, a => (a.BankName, a.Currency));

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

        var dtos = page.Select(t =>
        {
            var meta = accountMap.TryGetValue(t.AccountId, out var m) ? m : ("Unknown", "USD");
            return new GlobalTransactionDto(
                t.Id,
                t.AccountId,
                meta.Item1,
                meta.Item2,
                t.Amount,
                CurrencyConverter.ToUsd(t.Amount, meta.Item2),
                t.TransactionDate,
                t.PostedDate,
                t.Description,
                t.TransactionType,
                t.MerchantCategory,
                t.IsPending,
                t.CreatedAt);
        }).ToList();

        return new AllTransactionsResult(dtos, totalCount, request.Paging.Offset + request.Paging.Limit < totalCount, request.Paging.Offset, request.Paging.Limit);
    }
}
