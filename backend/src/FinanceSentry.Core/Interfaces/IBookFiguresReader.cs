namespace FinanceSentry.Core.Interfaces;

/// <summary>
/// The single canonical source for book-level figures: cash (banking + idle brokerage),
/// invested value, per-position detail, and totals. All three aggregation surfaces
/// (get_portfolio_snapshot, get_allocation_vs_target, risk check_risk_rules) must derive
/// their cashUsd / investedValueUsd / totalValueUsd from this reader and nowhere else.
/// Implementation lives in the Risk module; the interface lives in Core so Research and
/// Risk modules can both depend on it without circular references.
/// </summary>
public interface IBookFiguresReader
{
    Task<BookFigures> ReadAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>
/// Canonical book snapshot. Cash is split: banking accounts vs idle brokerage currency
/// balances (CASH instrument type). Positions excludes idle brokerage cash — those are
/// already counted in BrokerageCashUsd.
/// </summary>
public sealed record BookFigures(
    decimal TotalUsd,
    decimal CashUsd,
    decimal BankingCashUsd,
    decimal BrokerageCashUsd,
    decimal InvestedValueUsd,
    IReadOnlyList<BookFiguresPosition> Positions,
    bool IsStale,
    IReadOnlyList<string> StaleSources)
{
    public static BookFigures Empty { get; } = new(0m, 0m, 0m, 0m, 0m, [], false, []);
}

/// <summary>One non-cash holding in the book.</summary>
// AssetClass: normalized (Equities / Bonds / Crypto / RealEstate / Commodities / Other).
// Provider: "ibkr", "binance", etc.
// WeightPct: fraction of total book value (0–1); set by the reader after summing all sources.
public sealed record BookFiguresPosition(
    string Symbol,
    string AssetClass,
    string Provider,
    decimal Quantity,
    decimal UsdValue,
    decimal? CostBasisUsd,
    decimal WeightPct);
