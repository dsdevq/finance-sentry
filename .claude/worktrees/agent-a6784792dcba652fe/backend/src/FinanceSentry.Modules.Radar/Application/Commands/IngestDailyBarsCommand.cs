namespace FinanceSentry.Modules.Radar.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Radar.Application.Services;
using FinanceSentry.Modules.Radar.Domain;
using FinanceSentry.Modules.Radar.Domain.MarketStructure;
using FinanceSentry.Modules.Radar.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>Syncs the universe then ingests daily bars for every active ticker. Per-ticker isolated.</summary>
public sealed record IngestDailyBarsCommand : ICommand<IngestRunSummary>;

public sealed class IngestDailyBarsCommandHandler(
    IRadarUniverseService universe,
    IMarketHistorySource history,
    IDailyBarRepository bars,
    IOptions<RadarOptions> options,
    ILogger<IngestDailyBarsCommandHandler> logger)
    : ICommandHandler<IngestDailyBarsCommand, IngestRunSummary>
{
    private readonly RadarOptions _options = options.Value;

    public async Task<IngestRunSummary> Handle(IngestDailyBarsCommand command, CancellationToken cancellationToken)
    {
        var members = await universe.SyncAsync(cancellationToken);

        var lookbackSince = DateOnly.FromDateTime(DateTime.UtcNow)
            .AddDays(-CalendarDaysFor(_options.LookbackTradingDays));

        var ingested = 0;
        var barsAdded = 0;
        var failed = new List<string>();

        foreach (var member in members)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var latest = await bars.GetLatestDateAsync(member.Ticker, cancellationToken);
                var since = latest is not null ? latest.Value.AddDays(1) : lookbackSince;

                var fetched = await history.GetDailyBarsAsync(member.Ticker, since, cancellationToken);
                if (fetched.Count == 0)
                {
                    ingested++;
                    continue;
                }

                var entities = fetched.Select(b => new DailyBar
                {
                    Ticker = member.Ticker,
                    Date = b.Date,
                    Open = b.Open,
                    High = b.High,
                    Low = b.Low,
                    Close = b.Close,
                    AdjClose = b.AdjClose,
                    Volume = b.Volume,
                }).ToList();

                barsAdded += await bars.UpsertRangeAsync(entities, cancellationToken);
                ingested++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Radar ingestion failed for {Ticker}; continuing.", member.Ticker);
                failed.Add(member.Ticker);
            }
        }

        return new IngestRunSummary(ingested, barsAdded, failed.Count, failed);
    }

    // Convert a trading-day lookback to a calendar-day window with a weekend/holiday cushion (~1.5x).
    private static int CalendarDaysFor(int tradingDays) => (int)Math.Ceiling(tradingDays * 1.5);
}
