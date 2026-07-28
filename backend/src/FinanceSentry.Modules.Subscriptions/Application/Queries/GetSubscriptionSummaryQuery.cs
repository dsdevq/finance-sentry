namespace FinanceSentry.Modules.Subscriptions.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Core.Utils;
using FinanceSentry.Modules.Subscriptions.API.Responses;
using FinanceSentry.Modules.Subscriptions.Domain;
using FinanceSentry.Modules.Subscriptions.Domain.Repositories;

public record GetSubscriptionSummaryQuery(string UserId) : IQuery<SubscriptionSummaryResponse>;

public class GetSubscriptionSummaryQueryHandler(IDetectedSubscriptionRepository repository)
    : IQueryHandler<GetSubscriptionSummaryQuery, SubscriptionSummaryResponse>
{
    private readonly IDetectedSubscriptionRepository _repository = repository;

    public async Task<SubscriptionSummaryResponse> Handle(
        GetSubscriptionSummaryQuery request, CancellationToken cancellationToken)
    {
        var active = await _repository.GetActiveByUserIdAsync(request.UserId, cancellationToken);
        var all = await _repository.GetByUserIdAsync(request.UserId, false, cancellationToken);

        var potentiallyCancelled = all.Count(s => s.Status == SubscriptionStatus.PotentiallyCancelled);

        // Subscriptions can span multiple currencies (e.g. Monobank UAH + Revolut EUR).
        // Normalize every amount to USD before summing so the total is meaningful.
        var totalMonthly = active.Sum(s =>
        {
            var monthly = s.Cadence == "annual" ? s.AverageAmount / 12m : s.AverageAmount;
            return CurrencyConverter.ToUsd(monthly, s.Currency);
        });

        var totalAnnual = totalMonthly * 12m;

        return new SubscriptionSummaryResponse(
            Math.Round(totalMonthly, 2),
            Math.Round(totalAnnual, 2),
            active.Count,
            potentiallyCancelled,
            "USD");
    }
}
