namespace FinanceSentry.Modules.Risk.Domain;

/// <summary>In-memory aggregate of the live book, built fresh per check run. Never persisted directly.</summary>
public sealed record BookSnapshot(
    decimal TotalUsd,
    decimal CashUsd,
    IReadOnlyList<BookPosition> Positions,
    bool IsStale,
    IReadOnlyList<string> StaleSources)
{
    public static BookSnapshot Empty { get; } = new(0m, 0m, [], false, []);
}

public sealed record BookPosition(string Symbol, string Sleeve, decimal Quantity, decimal UsdValue, decimal WeightPct);
