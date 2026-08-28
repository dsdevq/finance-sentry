namespace FinanceSentry.Modules.Subscriptions.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Core.Utils;
using FinanceSentry.Modules.Subscriptions.API.Responses;
using FinanceSentry.Modules.Subscriptions.Domain;
using FinanceSentry.Modules.Subscriptions.Domain.Repositories;

public record GetSubscriptionSummaryQuery(string UserId) : IQuery<SubscriptionSummaryResponse>;

public class GetSubscriptionSummaryQueryHandler(IDetectedSubscriptionRepository repository)
    : IQueryHandler<GetSubscriptionSummaryQuery, SubscriptionSummaryResponse>
{
    private const int MonthsPerYear = 12;
    private const string BaseCurrency = "USD";

    private readonly IDetectedSubscriptionRepository _repository = repository;

    public async Task<SubscriptionSummaryResponse> Handle(
        GetSubscriptionSummaryQuery request, CancellationToken cancellationToken)
    {
        var active = await _repository.GetActiveByUserIdAsync(request.UserId, cancellationToken);
        var all = await _repository.GetByUserIdAsync(request.UserId, false, cancellationToken);

        var potentiallyCancelled = all.Count(s =>
            s.Kind == SubscriptionKinds.Subscription && s.Status == SubscriptionStatus.PotentiallyCancelled);

        // Subscriptions and installments are both committed outflow, but they annualize
        // differently — a subscription runs until cancelled, an installment stops when the
        // plan is paid off. They're reported as separate buckets plus a combined total so
        // the headline reflects real money leaving the account without pretending a plan
        // with one payment left costs a full year.
        var subscriptions = BuildSubscriptionBucket(
            active.Where(s => s.Kind == SubscriptionKinds.Subscription).ToList());
        var installments = BuildInstallmentBucket(
            active.Where(s => s.Kind == SubscriptionKinds.Installment).ToList());

        var combined = new SpendBucketResponse(
            Round(subscriptions.Monthly + installments.Monthly),
            Round(subscriptions.Next12Months + installments.Next12Months),
            // Open-ended subscriptions have no total owed, so a combined one would be a
            // half-truth — the finite figure lives on the installments bucket alone.
            RemainingCommitment: null,
            subscriptions.ActiveCount + installments.ActiveCount,
            subscriptions.HasUnknownSchedule || installments.HasUnknownSchedule);

        return new SubscriptionSummaryResponse(
            subscriptions, installments, combined, potentiallyCancelled, BaseCurrency);
    }

    private static SpendBucketResponse BuildSubscriptionBucket(IReadOnlyList<DetectedSubscription> items)
    {
        var monthly = items.Sum(MonthlyInBaseCurrency);

        return new SpendBucketResponse(
            Round(monthly),
            Round(monthly * MonthsPerYear),
            RemainingCommitment: null,
            items.Count,
            HasUnknownSchedule: false);
    }

    private static SpendBucketResponse BuildInstallmentBucket(IReadOnlyList<DetectedSubscription> items)
    {
        decimal monthly = 0m, next12 = 0m, remainingCommitment = 0m;
        var hasUnknownSchedule = false;

        foreach (var item in items)
        {
            var itemMonthly = MonthlyInBaseCurrency(item);
            monthly += itemMonthly;

            if (item.RemainingPayments is int remaining)
            {
                next12 += itemMonthly * Math.Min(MonthsPerYear, remaining);
                remainingCommitment += itemMonthly * remaining;
            }
            else
            {
                // No term and no end date — the only safe assumption is that it keeps
                // charging, which bounds the next 12 months but says nothing about a total.
                hasUnknownSchedule = true;
                next12 += itemMonthly * MonthsPerYear;
            }
        }

        return new SpendBucketResponse(
            Round(monthly),
            Round(next12),
            Round(remainingCommitment),
            items.Count,
            hasUnknownSchedule);
    }

    // Accounts span currencies (Monobank UAH, Revolut/AIB EUR, …) — convert at this reader
    // boundary so every total downstream is a single-currency sum.
    private static decimal MonthlyInBaseCurrency(DetectedSubscription item)
    {
        var monthly = item.Cadence == "annual" ? item.AverageAmount / MonthsPerYear : item.AverageAmount;
        return CurrencyConverter.ToUsd(monthly, item.Currency);
    }

    private static decimal Round(decimal amount) => Math.Round(amount, 2);
}
