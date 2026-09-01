namespace FinanceSentry.Modules.Radar.Domain.Ports;

/// <summary>
/// Cross-module port (feature 043). Provides the portfolio scanner with everything it needs
/// to emit the four daily radar signals. The concrete adapter lives in FinanceSentry.Integration
/// so Modules.Radar never depends on Modules.Risk or Modules.Research directly.
/// </summary>
public interface IPortfolioScanDataReader
{
    /// <summary>
    /// Users the scanner should process: union of users with an active IPS and users with risk
    /// rules on file. Users absent from both produce no meaningful portfolio signals.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetScanUserIdsAsync(CancellationToken ct = default);

    /// <summary>
    /// Full portfolio snapshot for one user. Returns null when the book is completely empty
    /// (no banking, brokerage, or crypto data), so the scanner can skip the user entirely.
    /// </summary>
    Task<PortfolioScanData?> ReadAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>
/// Combined portfolio snapshot consumed by the portfolio scanner. All percentages are 0–100.
/// </summary>
public sealed record PortfolioScanData(
    decimal TotalUsd,
    decimal CashUsd,
    bool IsStale,
    IReadOnlyList<string> StaleSources,
    /// <summary>Populated only when the user has an IPS; empty list when HasIps = false.</summary>
    IReadOnlyList<ScanSleeveDrift> DriftRows,
    /// <summary>Positions sorted descending by USD value.</summary>
    IReadOnlyList<ScanPosition> TopPositions,
    /// <summary>From the user's current risk rule set; null when no rule set exists.</summary>
    decimal? MaxPositionWeightPct,
    decimal? MinCashBufferPct)
{
    public bool HasIps => DriftRows.Count > 0;
    public decimal CashPct => TotalUsd > 0 ? Math.Round(CashUsd / TotalUsd * 100m, 2) : 0m;
}

/// <summary>Per-sleeve allocation drift vs IPS target. Percentages are 0–100.</summary>
public sealed record ScanSleeveDrift(
    string AssetClass,
    decimal TargetPct,
    decimal ActualPct,
    decimal DriftPct,
    string Status);

/// <summary>A single book position, sorted by USD value descending.</summary>
public sealed record ScanPosition(
    string Symbol,
    decimal UsdValue,
    decimal WeightPct);
