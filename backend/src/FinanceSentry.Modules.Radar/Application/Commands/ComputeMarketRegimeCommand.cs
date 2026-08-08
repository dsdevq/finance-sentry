namespace FinanceSentry.Modules.Radar.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Radar.Application.Services;
using FinanceSentry.Modules.Radar.Domain;
using FinanceSentry.Modules.Radar.Domain.Regime;
using FinanceSentry.Modules.Radar.Domain.Repositories;
using FinanceSentry.Modules.Radar.Infrastructure.MarketData;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Daily market-regime compute (feature 021). Fetches the VIX (via the shared market-history
/// source) and the FRED 10y/2y yields (via the keyless-silent <see cref="IYieldCurveSource"/>),
/// classifies each axis deterministically, persists one <see cref="RegimeReading"/>, and appends
/// to the shared signal log: a daily <c>info</c> reading per classified axis and a
/// <c>regime_change</c> <c>notable</c> for an axis whose band moved versus the prior reading. Each
/// axis is independent: one source failing leaves the other classified; when both are unavailable
/// nothing is persisted and a warning is logged.
/// </summary>
public sealed record ComputeMarketRegimeCommand : ICommand<ComputeRegimeSummary>;

/// <summary>Outcome of one regime compute run (for logging/observability).</summary>
public sealed record ComputeRegimeSummary(
    bool VolatilityAvailable,
    bool RatesAvailable,
    string? VolatilityRegime,
    string? RatesRegime,
    bool VolatilityChanged,
    bool RatesChanged);

public sealed class ComputeMarketRegimeCommandHandler(
    IMarketHistorySource historySource,
    IYieldCurveSource yieldSource,
    IRegimeReadingRepository readings,
    IRadarSignalWriter signals,
    IOptions<RegimeOptions> options,
    ILogger<ComputeMarketRegimeCommandHandler> logger)
    : ICommandHandler<ComputeMarketRegimeCommand, ComputeRegimeSummary>
{
    private readonly RegimeOptions _options = options.Value;

    public async Task<ComputeRegimeSummary> Handle(ComputeMarketRegimeCommand command, CancellationToken ct)
    {
        var volatility = await AssessVolatilityAsync(ct);
        var rates = await AssessRatesAsync(ct);

        if (volatility is null && rates is null)
        {
            logger.LogWarning(
                "Market regime compute produced no reading: VIX fetch empty and FRED unavailable/keyless.");
            return new ComputeRegimeSummary(false, false, null, null, false, false);
        }

        var prior = await readings.LatestAsync(ct);

        var reading = new RegimeReading
        {
            ComputedAt = DateTimeOffset.UtcNow,
            VolatilityAvailable = volatility is not null,
            VolatilityRegime = volatility?.Regime,
            VixLevel = volatility?.Level,
            VixSma = volatility?.Sma,
            VixTrend = volatility?.Trend,
            RatesAvailable = rates is not null,
            RatesRegime = rates?.Regime,
            Dgs10 = rates?.Dgs10,
            Dgs2 = rates?.Dgs2,
            Spread = rates?.Spread,
            RecessionWarning = rates?.RecessionWarning ?? false,
            GrowthValueTilt = rates?.Tilt,
        };
        await readings.AppendAsync(reading, ct);

        var (volatilityChanged, ratesChanged) =
            await EmitSignalsAsync(reading, volatility, rates, prior, ct);

        logger.LogInformation(
            "Market regime compute: volatility={Vol} (changed={VolChanged}), rates={Rates} (changed={RatesChanged}).",
            volatility?.Regime.ToString() ?? "unavailable", volatilityChanged,
            rates?.Regime.ToString() ?? "unavailable", ratesChanged);

        return new ComputeRegimeSummary(
            volatility is not null,
            rates is not null,
            volatility?.Regime.ToString(),
            rates?.Regime.ToString(),
            volatilityChanged,
            ratesChanged);
    }

    private async Task<VolatilityAssessment?> AssessVolatilityAsync(CancellationToken ct)
    {
        var since = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-_options.VixLookbackDays);
        var bars = await historySource.GetDailyBarsAsync(_options.VixTicker, since, ct);
        var closes = bars.OrderBy(b => b.Date).Select(b => b.Close).ToList();
        return RegimeClassifier.AssessVolatility(closes, _options);
    }

    private async Task<RatesAssessment?> AssessRatesAsync(CancellationToken ct)
    {
        if (!yieldSource.IsConfigured)
        {
            return null;
        }

        var curve = await yieldSource.GetLatestAsync(ct);
        return curve is null ? null : RegimeClassifier.AssessRates(curve.Dgs10, curve.Dgs2, _options);
    }

    /// <summary>
    /// Appends the daily <c>info</c> reading per classified axis and a <c>regime_change</c>
    /// <c>notable</c> when an axis' band differs from the prior reading's band on the same axis. A
    /// first-ever reading (no comparable prior) is never a "change". Change signals are deduped by
    /// the silence window via their DedupKey (a same-day re-run to the same from→to is suppressed).
    /// </summary>
    private async Task<(bool VolatilityChanged, bool RatesChanged)> EmitSignalsAsync(
        RegimeReading reading,
        VolatilityAssessment? volatility,
        RatesAssessment? rates,
        RegimeReading? prior,
        CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(reading.ComputedAt.UtcDateTime);
        var volatilityChanged = false;
        var ratesChanged = false;

        if (volatility is not null)
        {
            await signals.AppendSignalAsync(new RadarSignalRequest(
                RadarScanners.MarketRegime,
                RadarSignalTypes.RegimeVolatility,
                SignalSeverity.Info,
                RadarSubjectTypes.Universe,
                "volatility",
                null,
                $"{RadarScanners.MarketRegime}:{RadarSignalTypes.RegimeVolatility}:volatility:{today:yyyy-MM-dd}",
                new
                {
                    regime = volatility.Regime.ToString(),
                    vixLevel = volatility.Level,
                    vixSma = volatility.Sma,
                    trend = volatility.Trend.ToString(),
                }), ct);

            if (prior is { VolatilityAvailable: true, VolatilityRegime: { } priorBand } &&
                priorBand != volatility.Regime)
            {
                volatilityChanged = await signals.AppendSignalAsync(new RadarSignalRequest(
                    RadarScanners.MarketRegime,
                    RadarSignalTypes.RegimeChange,
                    SignalSeverity.Notable,
                    RadarSubjectTypes.Universe,
                    "volatility",
                    null,
                    $"{RadarScanners.MarketRegime}:{RadarSignalTypes.RegimeChange}:volatility:{priorBand}-{volatility.Regime}",
                    new
                    {
                        axis = "volatility",
                        from = priorBand.ToString(),
                        to = volatility.Regime.ToString(),
                        vixLevel = volatility.Level,
                    }), ct);
            }
        }

        if (rates is not null)
        {
            await signals.AppendSignalAsync(new RadarSignalRequest(
                RadarScanners.MarketRegime,
                RadarSignalTypes.RegimeRates,
                SignalSeverity.Info,
                RadarSubjectTypes.Universe,
                "rates",
                null,
                $"{RadarScanners.MarketRegime}:{RadarSignalTypes.RegimeRates}:rates:{today:yyyy-MM-dd}",
                new
                {
                    regime = rates.Regime.ToString(),
                    dgs10 = rates.Dgs10,
                    dgs2 = rates.Dgs2,
                    spread = rates.Spread,
                    recessionWarning = rates.RecessionWarning,
                    tilt = rates.Tilt,
                }), ct);

            if (prior is { RatesAvailable: true, RatesRegime: { } priorBand } &&
                priorBand != rates.Regime)
            {
                ratesChanged = await signals.AppendSignalAsync(new RadarSignalRequest(
                    RadarScanners.MarketRegime,
                    RadarSignalTypes.RegimeChange,
                    SignalSeverity.Notable,
                    RadarSubjectTypes.Universe,
                    "rates",
                    null,
                    $"{RadarScanners.MarketRegime}:{RadarSignalTypes.RegimeChange}:rates:{priorBand}-{rates.Regime}",
                    new
                    {
                        axis = "rates",
                        from = priorBand.ToString(),
                        to = rates.Regime.ToString(),
                        spread = rates.Spread,
                        recessionWarning = rates.RecessionWarning,
                    }), ct);
            }
        }

        return (volatilityChanged, ratesChanged);
    }
}
