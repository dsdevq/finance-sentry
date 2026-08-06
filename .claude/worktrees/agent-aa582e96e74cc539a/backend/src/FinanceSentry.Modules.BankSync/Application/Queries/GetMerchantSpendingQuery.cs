namespace FinanceSentry.Modules.BankSync.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.BankSync.Domain.Repositories;

public record GetMerchantSpendingQuery(Guid UserId, DateOnly From, DateOnly To)
    : IQuery<IReadOnlyDictionary<string, decimal>>;

public class GetMerchantSpendingQueryHandler(ITransactionRepository transactions)
    : IQueryHandler<GetMerchantSpendingQuery, IReadOnlyDictionary<string, decimal>>
{
    private readonly ITransactionRepository _transactions = transactions;

    public async Task<IReadOnlyDictionary<string, decimal>> Handle(
        GetMerchantSpendingQuery request, CancellationToken cancellationToken)
    {
        var fromUtc = request.From.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = request.To.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var all = await _transactions.GetByUserIdAsync(request.UserId, cancellationToken);

        var result = all
            .Where(t =>
                t.IsActive &&
                t.TransactionType == "debit" &&
                t.PostedDate.HasValue &&
                t.PostedDate.Value >= fromUtc &&
                t.PostedDate.Value <= toUtc)
            .GroupBy(t => t.MerchantCategory ?? string.Empty)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        return result;
    }
}
