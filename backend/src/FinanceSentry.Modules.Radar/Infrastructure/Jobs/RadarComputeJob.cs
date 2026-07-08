namespace FinanceSentry.Modules.Radar.Infrastructure.Jobs;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Radar.Application.Commands;
using FinanceSentry.Modules.Radar.Domain.MarketStructure;
using Hangfire;
using Microsoft.Extensions.Logging;

/// <summary>Daily market-structure compute + signal emission (shortly after ingestion).</summary>
public sealed class RadarComputeJob(
    ICommandHandler<ComputeMarketStructureCommand, ComputeRunSummary> handler,
    ILogger<RadarComputeJob> logger)
{
    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var summary = await handler.Handle(new ComputeMarketStructureCommand(), ct);
        logger.LogInformation(
            "Radar compute complete: {Computed} tickers, signals={Signals}.",
            summary.TickersComputed,
            string.Join(",", summary.SignalsByType.Select(kv => $"{kv.Key}={kv.Value}")));
    }
}
