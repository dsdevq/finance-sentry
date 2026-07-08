using FinanceSentry.Modules.Research.Application.Services;
using FinanceSentry.Modules.Research.Domain;

namespace FinanceSentry.Mcp.Tests.IntegrationTests;

/// <summary>
/// Deterministic test double for <see cref="ISecEdgarService"/> — the parity tests never hit
/// the live SEC EDGAR API. Returns whatever facts were seeded for the requested ticker.
/// </summary>
public sealed class FakeSecEdgarService(IReadOnlyDictionary<string, IReadOnlyList<FundamentalFact>> factsByTicker)
    : ISecEdgarService
{
    public Task<IReadOnlyList<EdgarFiling>> GetRecentFilingsAsync(
        string ticker, IReadOnlyCollection<string>? formTypes, int limit, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<EdgarFiling>>([]);

    public Task<IReadOnlyList<FundamentalFact>> GetFundamentalsAsync(
        string ticker, int maxPerConcept, CancellationToken ct = default)
        => Task.FromResult(
            factsByTicker.TryGetValue(ticker, out var facts) ? facts : (IReadOnlyList<FundamentalFact>)[]);
}

/// <summary>
/// Deterministic test double for <see cref="IMarketDataService"/> — always returns no price
/// history since the parity tests exercise fundamentals-based triggers only.
/// </summary>
public sealed class FakeMarketDataService : IMarketDataService
{
    public Task<IReadOnlyDictionary<string, QuoteCacheEntry>> GetQuotesAsync(
        IReadOnlyCollection<string> tickers, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyDictionary<string, QuoteCacheEntry>>(new Dictionary<string, QuoteCacheEntry>());

    public Task<IReadOnlyList<DailyClose>> GetDailyClosesAsync(
        string ticker, DateOnly since, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<DailyClose>>([]);
}
