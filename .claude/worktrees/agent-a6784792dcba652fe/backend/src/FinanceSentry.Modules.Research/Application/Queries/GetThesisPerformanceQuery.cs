namespace FinanceSentry.Modules.Research.Application.Queries;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.Application.Services;
using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Repositories;
using Microsoft.Extensions.Options;

public record GetThesisPerformanceQuery(Guid UserId, Guid? Id, string? Ticker) : IQuery<ThesisPerformanceResult?>;

/// <summary>
/// Resolves a thesis by id or ticker, then computes create→latest performance (US2). v0 sources
/// the "latest" price live from <see cref="IMarketDataService"/> (018 persisted daily bars don't
/// exist yet — R2).
/// </summary>
public class GetThesisPerformanceQueryHandler(
    IThesisRepository thesisRepo,
    IThesisEventRepository eventRepo,
    IMarketDataService marketData,
    IThesisPerformanceCalculator calculator,
    IOptions<FrictionConfig> frictionOptions)
    : IQueryHandler<GetThesisPerformanceQuery, ThesisPerformanceResult?>
{
    public async Task<ThesisPerformanceResult?> Handle(GetThesisPerformanceQuery query, CancellationToken ct)
    {
        var thesis = await ResolveThesisAsync(query, ct);
        if (thesis is null)
        {
            return null;
        }

        var createdEvent = await FindCreatedEventAsync(thesis, ct);
        if (createdEvent is null)
        {
            return null;
        }

        var quotes = await marketData.GetQuotesAsync(
            [thesis.Ticker, ThesisEventRecorder.DefaultBenchmarkTicker], ct);

        var hasSubject = quotes.TryGetValue(thesis.Ticker, out var subjectQuote);
        var hasBenchmark = quotes.TryGetValue(ThesisEventRecorder.DefaultBenchmarkTicker, out var benchmarkQuote);

        var input = new ThesisPerformanceInput(
            thesis.Id,
            ThesisEventType.Created,
            createdEvent.Timestamp,
            createdEvent.SubjectPrice,
            createdEvent.BenchmarkPrice,
            ThesisEventType.Snapshot,
            DateTimeOffset.UtcNow,
            hasSubject ? subjectQuote!.Price : null,
            hasBenchmark ? benchmarkQuote!.Price : null,
            thesis.Ticker,
            frictionOptions.Value);

        return calculator.Calculate(input);
    }

    private async Task<InvestmentThesis?> ResolveThesisAsync(GetThesisPerformanceQuery query, CancellationToken ct)
    {
        if (query.Id is { } id)
        {
            return await thesisRepo.FindAsync(query.UserId, id, ct);
        }

        if (!string.IsNullOrWhiteSpace(query.Ticker))
        {
            var matches = await thesisRepo.FindByTickerAsync(query.UserId, query.Ticker.Trim().ToUpperInvariant(), ct);
            return matches.FirstOrDefault();
        }

        return null;
    }

    private async Task<ThesisEvent?> FindCreatedEventAsync(InvestmentThesis thesis, CancellationToken ct)
    {
        var events = await eventRepo.ListAsync(thesis.UserId, thesis.Id, ct);
        return events.FirstOrDefault(e => e.EventType == ThesisEventType.Created);
    }
}
