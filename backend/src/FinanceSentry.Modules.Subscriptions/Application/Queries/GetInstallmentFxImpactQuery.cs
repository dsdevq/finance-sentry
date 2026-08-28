namespace FinanceSentry.Modules.Subscriptions.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Infrastructure.Fx;
using FinanceSentry.Modules.Subscriptions.API.Responses;
using FinanceSentry.Modules.Subscriptions.Domain;
using FinanceSentry.Modules.Subscriptions.Domain.Repositories;

public record GetInstallmentFxImpactQuery(string UserId) : IQuery<InstallmentFxImpactResponse>;

/// <summary>
/// Prices foreign-currency installments against the exchange rate over time.
///
/// A UAH installment repaid out of euro income has a payment that never changes in
/// hryvnia — what changes is how much hard currency it takes to make that payment. This
/// compares each plan's cost at its baseline against today, and builds a monthly series
/// for the whole foreign-currency set so the trend is visible.
/// </summary>
public class GetInstallmentFxImpactQueryHandler(
    IDetectedSubscriptionRepository repository,
    IHistoricalExchangeRateService rates)
    : IQueryHandler<GetInstallmentFxImpactQuery, InstallmentFxImpactResponse>
{
    private const string BaseCurrency = "USD";

    private readonly IDetectedSubscriptionRepository _repository = repository;
    private readonly IHistoricalExchangeRateService _rates = rates;

    public async Task<InstallmentFxImpactResponse> Handle(
        GetInstallmentFxImpactQuery request, CancellationToken cancellationToken)
    {
        var active = await _repository.GetActiveByUserIdAsync(request.UserId, cancellationToken);

        // Only foreign-currency plans can be moved by the exchange rate; a plan already
        // billed in the base currency would show a flat, meaningless line.
        var plans = active
            .Where(s => s.Kind == SubscriptionKinds.Installment
                     && !string.Equals(s.Currency, BaseCurrency, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (plans.Count == 0)
        {
            return new InstallmentFxImpactResponse(
                BaseCurrency, [], 0m, 0m, 0m, 0m, []);
        }

        var earliest = plans.Min(BaselineDateOf);
        var seriesByCurrency = new Dictionary<string, IReadOnlyDictionary<DateOnly, decimal>>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var currency in plans.Select(p => p.Currency).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            seriesByCurrency[currency] =
                await _rates.GetDailySeriesAsync(currency, earliest, today, cancellationToken);
        }

        var impacts = new List<InstallmentFxImpact>(plans.Count);

        foreach (var plan in plans)
        {
            var series = seriesByCurrency[plan.Currency];
            var baselineDate = BaselineDateOf(plan);

            var baselineRate = RateOn(series, baselineDate);
            var currentRate = RateOn(series, today);
            if (baselineRate <= 0m || currentRate <= 0m) continue;

            var baselineCost = Round(plan.AverageAmount * baselineRate);
            var currentCost = Round(plan.AverageAmount * currentRate);

            impacts.Add(new InstallmentFxImpact(
                plan.Id,
                plan.MerchantNameDisplay,
                plan.Currency,
                plan.AverageAmount,
                baselineDate,
                UnitsPerBase(baselineRate),
                baselineCost,
                today,
                UnitsPerBase(currentRate),
                currentCost,
                Round(currentCost - baselineCost),
                Percent(baselineCost, currentCost),
                plan.StartDate is null));
        }

        var baselineTotal = Round(impacts.Sum(i => i.BaselineCost));
        var currentTotal = Round(impacts.Sum(i => i.CurrentCost));

        return new InstallmentFxImpactResponse(
            BaseCurrency,
            impacts,
            baselineTotal,
            currentTotal,
            Round(currentTotal - baselineTotal),
            Percent(baselineTotal, currentTotal),
            BuildMonthlySeries(plans, seriesByCurrency, earliest, today));
    }

    /// <summary>
    /// Where the comparison starts. A user-set start date wins; otherwise fall back to the
    /// oldest charge detection saw, which for a plan older than the lookback window is a
    /// conservative (understated) baseline rather than a wrong one.
    /// </summary>
    private static DateOnly BaselineDateOf(DetectedSubscription plan) =>
        plan.StartDate ?? plan.LastChargeDate.AddMonths(-Math.Max(0, plan.OccurrenceCount - 1));

    /// <summary>
    /// Total monthly cost of every foreign plan alive on each month-end, so the line shows
    /// rate movement rather than plans being added — a plan contributes only from its own
    /// baseline onward.
    /// </summary>
    private static IReadOnlyList<FxCostPoint> BuildMonthlySeries(
        IReadOnlyList<DetectedSubscription> plans,
        IReadOnlyDictionary<string, IReadOnlyDictionary<DateOnly, decimal>> seriesByCurrency,
        DateOnly from,
        DateOnly to)
    {
        var points = new List<FxCostPoint>();
        var cursor = new DateOnly(from.Year, from.Month, 1);
        var last = new DateOnly(to.Year, to.Month, 1);

        while (cursor <= last)
        {
            var sampleDate = cursor > to ? to : cursor;
            var monthlyCost = 0m;
            decimal? headlineRate = null;

            foreach (var plan in plans)
            {
                if (BaselineDateOf(plan) > sampleDate) continue;

                var rate = RateOn(seriesByCurrency[plan.Currency], sampleDate);
                if (rate <= 0m) continue;

                monthlyCost += plan.AverageAmount * rate;
                headlineRate ??= rate;
            }

            if (headlineRate is decimal rateForMonth)
                points.Add(new FxCostPoint(sampleDate, UnitsPerBase(rateForMonth), Round(monthlyCost)));

            cursor = cursor.AddMonths(1);
        }

        return points;
    }

    /// <summary>
    /// The series is gap-filled by the rate service, so a direct hit is expected; walking
    /// back covers a sample date that predates the requested window.
    /// </summary>
    private static decimal RateOn(IReadOnlyDictionary<DateOnly, decimal> series, DateOnly date)
    {
        if (series.TryGetValue(date, out var rate)) return rate;

        return series.Count == 0
            ? 0m
            : series.Where(kv => kv.Key <= date)
                .OrderByDescending(kv => kv.Key)
                .Select(kv => kv.Value)
                .FirstOrDefault();
    }

    /// <summary>Units of the foreign currency per 1 base unit — how a rate is normally quoted.</summary>
    private static decimal UnitsPerBase(decimal basePerUnit) =>
        basePerUnit > 0m ? Math.Round(1m / basePerUnit, 4) : 0m;

    private static decimal Percent(decimal from, decimal to) =>
        from == 0m ? 0m : Math.Round((to - from) / from * 100m, 2);

    private static decimal Round(decimal amount) => Math.Round(amount, 2);
}
