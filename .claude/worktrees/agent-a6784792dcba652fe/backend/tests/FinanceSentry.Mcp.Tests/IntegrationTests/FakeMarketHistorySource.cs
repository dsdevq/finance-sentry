using FinanceSentry.Core.Interfaces;

namespace FinanceSentry.Mcp.Tests.IntegrationTests;

/// <summary>
/// Deterministic test double for <see cref="IMarketHistorySource"/> — the Radar parity tests never
/// hit live Yahoo. Returns whatever bars were seeded for the requested ticker (empty by default).
/// </summary>
public sealed class FakeMarketHistorySource(
    IReadOnlyDictionary<string, IReadOnlyList<DailyBarData>>? barsByTicker = null) : IMarketHistorySource
{
    public Task<IReadOnlyList<DailyBarData>> GetDailyBarsAsync(
        string ticker, DateOnly since, CancellationToken ct = default)
        => Task.FromResult(
            barsByTicker is not null && barsByTicker.TryGetValue(ticker, out var bars)
                ? bars
                : (IReadOnlyList<DailyBarData>)[]);
}
