namespace FinanceSentry.Modules.Radar.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Radar.Application.Services;
using FinanceSentry.Modules.Radar.Domain.MarketStructure;
using FinanceSentry.Modules.Radar.Domain.Repositories;
using Microsoft.Extensions.Options;

public sealed record HistoricalValidationSummary(
    int TickersEvaluated,
    int TotalBars,
    IReadOnlyDictionary<string, int> DetectionsByType,
    IReadOnlyDictionary<string, decimal> DetectionsPerYearByType);

/// <summary>
/// One-off replay (FR-016) over ≥5y of persisted bars, counting signal frequency by type so
/// thresholds can be judged before alerting is enabled. Uses the same pure functions as the live
/// scanner; does not persist signals or raise Alerts.
/// </summary>
public sealed record RunHistoricalValidationCommand : ICommand<HistoricalValidationSummary>;

public sealed class RunHistoricalValidationCommandHandler(
    IRadarUniverseRepository universe,
    IDailyBarRepository bars,
    IOptions<RadarOptions> options)
    : ICommandHandler<RunHistoricalValidationCommand, HistoricalValidationSummary>
{
    private const decimal TradingDaysPerYear = 252m;

    private readonly RadarOptions _options = options.Value;

    public async Task<HistoricalValidationSummary> Handle(
        RunHistoricalValidationCommand command, CancellationToken cancellationToken)
    {
        var members = await universe.ListAllAsync(cancellationToken);
        var since = new DateOnly(1900, 1, 1);
        var thresholds = new HistoricalReplay.Thresholds(
            _options.UnusualMoveZScore, _options.ExtensionThreshold, Volatility.DefaultVolWindow);

        var counts = new Dictionary<string, int>();
        var tickersEvaluated = 0;
        var totalBars = 0;

        foreach (var member in members)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var series = await bars.GetSinceAsync(member.Ticker, since, cancellationToken);
            if (series.Count == 0)
            {
                continue;
            }

            tickersEvaluated++;
            totalBars += series.Count;

            foreach (var detection in HistoricalReplay.Replay(series, thresholds))
            {
                counts[detection.SignalType] = counts.TryGetValue(detection.SignalType, out var c) ? c + 1 : 1;
            }
        }

        var years = totalBars == 0 ? 1m : totalBars / TradingDaysPerYear;
        var perYear = counts.ToDictionary(kv => kv.Key, kv => kv.Value / years);

        return new HistoricalValidationSummary(tickersEvaluated, totalBars, counts, perYear);
    }
}
