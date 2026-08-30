namespace FinanceSentry.Modules.BankSync.Infrastructure.Services;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.BankSync.Application.Queries;

/// <summary>
/// 039 read-port adapter: exposes <see cref="GetMerchantSpendingQuery"/> to other modules
/// (Budgets) without them referencing BankSync.
/// </summary>
public class MerchantSpendingReader(
    IQueryHandler<GetMerchantSpendingQuery, IReadOnlyDictionary<string, decimal>> handler)
    : IMerchantSpendingReader
{
    private readonly IQueryHandler<GetMerchantSpendingQuery, IReadOnlyDictionary<string, decimal>> _handler = handler;

    public Task<IReadOnlyDictionary<string, decimal>> GetSpendingByCategoryUsdAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default)
        => _handler.Handle(new GetMerchantSpendingQuery(userId, from, to), ct);
}
