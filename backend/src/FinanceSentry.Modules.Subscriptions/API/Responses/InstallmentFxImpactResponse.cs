namespace FinanceSentry.Modules.Subscriptions.API.Responses;

/// <summary>
/// What exchange-rate movement has done to one foreign-currency plan. The native payment
/// is contractually fixed; only what it costs in the base currency moves.
/// </summary>
/// <param name="BaselineDate">
/// The date the comparison starts from: the plan's user-set start date when known,
/// otherwise the first charge detection actually observed.
/// </param>
/// <param name="BaselineIsObserved">
/// True when the baseline is only the first *observed* charge rather than the real start —
/// detection sees ~13 months, so an older plan understates how far the rate has moved
/// until its start date is set.
/// </param>
public record InstallmentFxImpact(
    Guid Id,
    string Merchant,
    string Currency,
    decimal MonthlyNative,
    DateOnly BaselineDate,
    decimal BaselineUnitsPerBase,
    decimal BaselineCost,
    DateOnly CurrentDate,
    decimal CurrentUnitsPerBase,
    decimal CurrentCost,
    decimal ChangeAmount,
    decimal ChangePercent,
    bool BaselineIsObserved);

/// <param name="Points">
/// Monthly cost of the whole foreign-currency plan set in the base currency, so the trend
/// shows what rate movement did to the total rather than to one plan.
/// </param>
public record InstallmentFxImpactResponse(
    string BaseCurrency,
    IReadOnlyList<InstallmentFxImpact> Plans,
    decimal BaselineCostTotal,
    decimal CurrentCostTotal,
    decimal ChangeAmountTotal,
    decimal ChangePercentTotal,
    IReadOnlyList<FxCostPoint> Points);

public record FxCostPoint(DateOnly Date, decimal UnitsPerBase, decimal MonthlyCost);
