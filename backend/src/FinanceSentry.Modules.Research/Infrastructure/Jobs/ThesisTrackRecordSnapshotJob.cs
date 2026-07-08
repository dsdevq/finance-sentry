namespace FinanceSentry.Modules.Research.Infrastructure.Jobs;

using FinanceSentry.Modules.Research.Application.Services;
using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Repositories;
using Hangfire;
using Microsoft.Extensions.Logging;

/// <summary>
/// Weekly job (R5): (1) backfills any <see cref="ThesisEvent"/> rows still marked
/// <see cref="ThesisEvent.PricesPending"/>, and (2) appends a <see cref="ThesisEventType.Snapshot"/>
/// event for every thesis with no terminal event yet, so history plots have a regular cadence
/// independent of lifecycle event density (User Story 3, Acceptance Scenario 3).
/// </summary>
public sealed class ThesisTrackRecordSnapshotJob(
    IThesisEventRepository eventRepo,
    IThesisRepository thesisRepo,
    IMarketDataService marketData,
    ILogger<ThesisTrackRecordSnapshotJob> logger)
{
    private static readonly ThesisEventType[] TerminalEventTypes =
    [
        ThesisEventType.Closed,
        ThesisEventType.Rejected,
        ThesisEventType.Expired,
    ];

    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        await BackfillPendingAsync(ct);
        await SnapshotActiveThesesAsync(ct);
    }

    private async Task BackfillPendingAsync(CancellationToken ct)
    {
        var pending = await eventRepo.ListPendingAsync(ct);

        foreach (var thesisEvent in pending)
        {
            try
            {
                var quotes = await marketData.GetQuotesAsync(
                    [thesisEvent.Ticker, thesisEvent.BenchmarkTicker], ct);

                var hasSubject = quotes.TryGetValue(thesisEvent.Ticker, out var subjectQuote);
                var hasBenchmark = quotes.TryGetValue(thesisEvent.BenchmarkTicker, out var benchmarkQuote);

                if (!hasSubject || !hasBenchmark)
                {
                    continue;
                }

                thesisEvent.SubjectPrice = subjectQuote!.Price;
                thesisEvent.BenchmarkPrice = benchmarkQuote!.Price;
                thesisEvent.PricesPending = false;

                await eventRepo.UpdatePricesAsync(thesisEvent, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex, "ThesisTrackRecordSnapshotJob: backfill failed for event {EventId} ({Ticker})",
                    thesisEvent.Id, thesisEvent.Ticker);
            }
        }
    }

    private async Task SnapshotActiveThesesAsync(CancellationToken ct)
    {
        var userIds = await thesisRepo.GetUserIdsWithThesesAsync(ct);

        foreach (var userId in userIds)
        {
            var theses = await thesisRepo.ListAsync(userId, ct);
            var events = await eventRepo.ListAsync(userId, subjectId: null, ct);
            var eventsBySubject = events
                .GroupBy(e => e.SubjectId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var thesis in theses)
            {
                var hasTerminalEvent = eventsBySubject.TryGetValue(thesis.Id, out var subjectEvents) &&
                    subjectEvents.Exists(e => TerminalEventTypes.Contains(e.EventType));

                if (hasTerminalEvent)
                {
                    continue;
                }

                try
                {
                    var quotes = await marketData.GetQuotesAsync(
                        [thesis.Ticker, ThesisEventRecorder.DefaultBenchmarkTicker], ct);

                    var hasSubject = quotes.TryGetValue(thesis.Ticker, out var subjectQuote);
                    var hasBenchmark = quotes.TryGetValue(
                        ThesisEventRecorder.DefaultBenchmarkTicker, out var benchmarkQuote);

                    await eventRepo.AppendAsync(
                        new ThesisEvent
                        {
                            UserId = userId,
                            SubjectType = ThesisSubjectType.Thesis,
                            SubjectId = thesis.Id,
                            Ticker = thesis.Ticker,
                            EventType = ThesisEventType.Snapshot,
                            Timestamp = DateTimeOffset.UtcNow,
                            SubjectPrice = hasSubject ? subjectQuote!.Price : null,
                            BenchmarkPrice = hasBenchmark ? benchmarkQuote!.Price : null,
                            BenchmarkTicker = ThesisEventRecorder.DefaultBenchmarkTicker,
                            PricesPending = !(hasSubject && hasBenchmark),
                        },
                        ct);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex, "ThesisTrackRecordSnapshotJob: snapshot failed for thesis {ThesisId} ({Ticker})",
                        thesis.Id, thesis.Ticker);
                }
            }
        }
    }
}
