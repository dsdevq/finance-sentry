namespace FinanceSentry.Modules.Research.Tests.Companion;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Research.Application.Services;
using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Repositories;

/// <summary>Shared test doubles for the companion-layer (feature 030) unit tests.</summary>
internal sealed class FakeAnalystUniverseRepository : IAnalystUniverseRepository
{
    public List<AnalystUniverseMember> Members { get; } = [];

    public List<string> Deactivated { get; } = [];

    public Task<IReadOnlyList<AnalystUniverseMember>> ListActiveAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<AnalystUniverseMember>>(Members.Where(m => m.Active).ToList());

    public Task<IReadOnlyList<AnalystUniverseMember>> ListAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<AnalystUniverseMember>>(Members.ToList());

    public Task UpsertMembersAsync(IReadOnlyCollection<AnalystUniverseMember> members, CancellationToken ct = default)
    {
        foreach (var member in members)
        {
            var existing = Members.FirstOrDefault(m => m.Ticker == member.Ticker);
            if (existing is null)
            {
                Members.Add(member);
            }
            else
            {
                existing.Active = true;
                existing.Reason = member.Reason;
            }
        }

        return Task.CompletedTask;
    }

    public Task DeactivateAsync(IReadOnlyCollection<string> tickers, CancellationToken ct = default)
    {
        foreach (var ticker in tickers)
        {
            Deactivated.Add(ticker);
            var member = Members.FirstOrDefault(m => m.Ticker == ticker);
            if (member is not null)
            {
                member.Active = false;
            }
        }

        return Task.CompletedTask;
    }

    public Task<bool> IsInUniverseAsync(string ticker, CancellationToken ct = default)
    {
        var upper = ticker.Trim().ToUpperInvariant();
        return Task.FromResult(Members.Any(m => m.Ticker == upper && m.Active));
    }
}

internal sealed class FakeAnalystActionRepository : IAnalystActionRepository
{
    public List<AnalystAction> Actions { get; } = [];

    public string? LastTickerFilter { get; private set; }

    public AnalystActionType? LastTypeFilter { get; private set; }

    public Task<int> UpsertAsync(IReadOnlyCollection<AnalystAction> actions, CancellationToken ct = default)
    {
        Actions.AddRange(actions);
        return Task.FromResult(actions.Count);
    }

    public Task<IReadOnlyList<AnalystAction>> QueryAsync(
        string? ticker, DateOnly since, AnalystActionType? actionType, int limit, CancellationToken ct = default)
    {
        LastTickerFilter = ticker;
        LastTypeFilter = actionType;

        var q = Actions.Where(a => a.ActionDate >= since);
        if (!string.IsNullOrWhiteSpace(ticker))
        {
            var upper = ticker.Trim().ToUpperInvariant();
            q = q.Where(a => a.Ticker == upper);
        }

        if (actionType is { } type)
        {
            q = q.Where(a => a.ActionType == type);
        }

        return Task.FromResult<IReadOnlyList<AnalystAction>>(
            q.OrderByDescending(a => a.ActionDate).Take(limit).ToList());
    }

    public Task<AnalystAction?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(Actions.FirstOrDefault(a => a.Id == id));
}

internal sealed class FakeBankingTotalsReader(params Guid[] userIds) : IBankingTotalsReader
{
    public Task<IReadOnlyList<Guid>> GetActiveUserIdsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Guid>>(userIds.ToList());

    public Task<decimal> GetTotalUsdAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(0m);
}

internal sealed class FakeBrokerageReader(IReadOnlyList<BrokerageHoldingSummary>? holdings = null)
    : IBrokerageHoldingsReader
{
    public Task<IReadOnlyList<BrokerageHoldingSummary>> GetHoldingsAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(holdings ?? []);
}

internal sealed class FakeSecEdgarService(IReadOnlyList<FundamentalFact>? facts = null) : ISecEdgarService
{
    public Task<IReadOnlyList<EdgarFiling>> GetRecentFilingsAsync(
        string ticker, IReadOnlyCollection<string>? formTypes, int limit, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<EdgarFiling>>([]);

    public Task<IReadOnlyList<FundamentalFact>> GetFundamentalsAsync(
        string ticker, int maxPerConcept, CancellationToken ct = default)
        => Task.FromResult(facts ?? []);
}

internal sealed class FakeMarketDataService(IReadOnlyList<DailyClose>? closes = null) : IMarketDataService
{
    public Task<IReadOnlyDictionary<string, QuoteCacheEntry>> GetQuotesAsync(
        IReadOnlyCollection<string> tickers, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyDictionary<string, QuoteCacheEntry>>(
            new Dictionary<string, QuoteCacheEntry>());

    public Task<IReadOnlyList<DailyClose>> GetDailyClosesAsync(
        string ticker, DateOnly since, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<DailyClose>>(
            (closes ?? []).Where(c => c.Date >= since).ToList());
}

internal sealed class FakeValuationDataService : IValuationDataService
{
    public Dictionary<string, ValuationCurrentMetrics?> Metrics { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<string> DefaultPeers { get; } = [];

    public Task<ValuationCurrentMetrics?> GetCurrentMetricsAsync(string ticker, CancellationToken ct = default)
        => Task.FromResult(Metrics.TryGetValue(ticker.Trim().ToUpperInvariant(), out var m) ? m : null);

    public Task<IReadOnlyList<string>> GetPeerSymbolsAsync(string ticker, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<string>>(DefaultPeers.ToList());
}

internal sealed class FakeValuationHistoryService(TrailingPeHistory? history = null) : IValuationHistoryService
{
    public Task<TrailingPeHistory> GetTrailingPeHistoryAsync(string ticker, CancellationToken ct = default)
        => Task.FromResult(history ?? new TrailingPeHistory(null, null));
}

internal sealed class FakeValuationSnapshotRepository : IValuationSnapshotRepository
{
    public List<ValuationSnapshot> Added { get; } = [];

    public Task AddAsync(ValuationSnapshot snapshot, CancellationToken ct = default)
    {
        Added.Add(snapshot);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ValuationSnapshot>> GetRecentAsync(
        string ticker, int limit, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ValuationSnapshot>>(
            Added.Where(s => s.Ticker == ticker.Trim().ToUpperInvariant()).ToList());
}
