namespace FinanceSentry.Modules.Radar.Infrastructure.Jobs;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Radar.Application.Commands;
using Hangfire;
using Microsoft.Extensions.Logging;

/// <summary>
/// Daily market-regime compute job (feature 021) — runs after the market close / FRED daily update.
/// Delegates to <see cref="ComputeMarketRegimeCommand"/>; a source outage on one axis never aborts
/// the other (handled inside the command).
/// </summary>
public sealed class RegimeComputeJob(
    ICommandHandler<ComputeMarketRegimeCommand, ComputeRegimeSummary> handler,
    ILogger<RegimeComputeJob> logger)
{
    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var summary = await handler.Handle(new ComputeMarketRegimeCommand(), ct);
        logger.LogInformation(
            "Regime compute job done: volatility={Vol} (available={VolAvail}, changed={VolChanged}), " +
            "rates={Rates} (available={RatesAvail}, changed={RatesChanged}).",
            summary.VolatilityRegime ?? "n/a", summary.VolatilityAvailable, summary.VolatilityChanged,
            summary.RatesRegime ?? "n/a", summary.RatesAvailable, summary.RatesChanged);
    }
}
