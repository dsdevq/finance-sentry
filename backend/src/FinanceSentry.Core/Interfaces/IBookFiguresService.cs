namespace FinanceSentry.Core.Interfaces;

/// <summary>
/// A single position in the invested (non-cash) portion of the book, already normalized
/// to a canonical asset class by <see cref="Domain.AssetClassNormalizer"/>.
/// </summary>
public sealed record BookFigurePosition(
    string Symbol,
    string AssetClass,
    decimal Quantity,
    decimal? CostBasisUsd,
    decimal UsdValue,
    string Provider);

/// <summary>
/// The canonical book snapshot — one source of truth for cash, invested value, and totals.
/// Produced by <see cref="IBookFiguresService"/> and consumed by all three surfaces
/// (portfolio snapshot, allocation-drift, and risk compliance) so they are guaranteed
/// to agree on cashUsd / investedValueUsd / totalValueUsd.
/// </summary>
public sealed record BookFigures(
    decimal CashUsd,
    decimal BankingCashUsd,
    decimal BrokerageCashUsd,
    decimal InvestedValueUsd,
    decimal TotalValueUsd,
    IReadOnlyList<BookFigurePosition> Positions,
    bool IsStale,
    IReadOnlyList<string> StaleSources)
{
    public static BookFigures Empty { get; } =
        new(0m, 0m, 0m, 0m, 0m, [], false, []);
}

/// <summary>
/// Aggregates banking, brokerage, and crypto sources into a <see cref="BookFigures"/>
/// snapshot. Idle brokerage cash (instrument type "CASH") is bucketed into
/// <see cref="BookFigures.BrokerageCashUsd"/>, not into the invested positions list —
/// the same convention used by the allocation-drift tool. Each source degrades
/// gracefully: a failed read contributes zero and is recorded in
/// <see cref="BookFigures.StaleSources"/>.
/// </summary>
public interface IBookFiguresService
{
    Task<BookFigures> ReadAsync(Guid userId, CancellationToken ct = default);
}
