namespace FinanceSentry.Modules.Radar.Application.Commands;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Radar.Domain;
using FinanceSentry.Modules.Radar.Domain.Ports;
using Microsoft.Extensions.Logging;

/// <summary>
/// Feature 043 — daily portfolio-state compute. For each user with an IPS or risk rules,
/// reads the canonical book and emits four silent radar signals:
/// allocation_drift, concentration_weight, cash_buffer, sync_health.
/// All signals use OneTime=true + date-keyed DedupKey so re-runs on the same day are no-ops.
/// </summary>
public sealed record ComputePortfolioSignalsCommand : ICommand<PortfolioScanSummary>;

public sealed record PortfolioScanSummary(
    int UsersScanned,
    int SignalsEmitted,
    int SignalsSuppressed);

public sealed class ComputePortfolioSignalsCommandHandler(
    IPortfolioScanDataReader scanData,
    IRadarSignalWriter signals,
    ILogger<ComputePortfolioSignalsCommandHandler> logger)
    : ICommandHandler<ComputePortfolioSignalsCommand, PortfolioScanSummary>
{
    public async Task<PortfolioScanSummary> Handle(ComputePortfolioSignalsCommand command, CancellationToken ct)
    {
        var userIds = await scanData.GetScanUserIdsAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var emitted = 0;
        var suppressed = 0;

        foreach (var userId in userIds)
        {
            try
            {
                var (e, s) = await ScanUserAsync(userId, today, ct);
                emitted += e;
                suppressed += s;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Portfolio scan failed for user {UserId}; skipping.", userId);
            }
        }

        logger.LogInformation(
            "Portfolio scan complete: users={Users}, emitted={Emitted}, suppressed={Suppressed}.",
            userIds.Count, emitted, suppressed);

        return new PortfolioScanSummary(userIds.Count, emitted, suppressed);
    }

    private async Task<(int Emitted, int Suppressed)> ScanUserAsync(
        Guid userId, DateOnly today, CancellationToken ct)
    {
        var data = await scanData.ReadAsync(userId, ct);
        if (data is null || data.TotalUsd <= 0)
        {
            return (0, 0);
        }

        var emitted = 0;
        var suppressed = 0;

        void Track(bool appended) { if (appended) emitted++; else suppressed++; }

        // ── allocation drift (per sleeve, only when IPS exists) ────────────────
        if (data.HasIps)
        {
            foreach (var sleeve in data.DriftRows)
            {
                var severity = sleeve.Status is "OverBand" or "UnderBand"
                    ? SignalSeverity.Notable
                    : SignalSeverity.Info;

                Track(await signals.AppendSignalAsync(new RadarSignalRequest(
                    RadarScanners.Portfolio,
                    RadarSignalTypes.AllocationDrift,
                    severity,
                    RadarSubjectTypes.AssetClass,
                    sleeve.AssetClass,
                    userId,
                    DedupKey(userId, RadarSignalTypes.AllocationDrift, sleeve.AssetClass, today),
                    new
                    {
                        targetPct = sleeve.TargetPct,
                        actualPct = sleeve.ActualPct,
                        driftPct = sleeve.DriftPct,
                        status = sleeve.Status,
                        totalUsd = data.TotalUsd,
                    },
                    OneTime: true), ct));
            }
        }

        // ── top-position concentration ─────────────────────────────────────────
        var top = data.TopPositions.Count > 0 ? data.TopPositions[0] : null;
        if (top is not null)
        {
            var overLimit = data.MaxPositionWeightPct.HasValue &&
                            top.WeightPct > data.MaxPositionWeightPct.Value;

            Track(await signals.AppendSignalAsync(new RadarSignalRequest(
                RadarScanners.Portfolio,
                RadarSignalTypes.ConcentrationWeight,
                overLimit ? SignalSeverity.Notable : SignalSeverity.Info,
                RadarSubjectTypes.Ticker,
                top.Symbol,
                userId,
                DedupKey(userId, RadarSignalTypes.ConcentrationWeight, top.Symbol, today),
                new
                {
                    weightPct = top.WeightPct,
                    usdValue = top.UsdValue,
                    limitPct = data.MaxPositionWeightPct,
                    overLimit,
                },
                OneTime: true), ct));
        }

        // ── cash buffer ────────────────────────────────────────────────────────
        if (data.MinCashBufferPct.HasValue)
        {
            var belowThreshold = data.CashPct < data.MinCashBufferPct.Value;

            Track(await signals.AppendSignalAsync(new RadarSignalRequest(
                RadarScanners.Portfolio,
                RadarSignalTypes.CashBuffer,
                belowThreshold ? SignalSeverity.Notable : SignalSeverity.Info,
                RadarSubjectTypes.Portfolio,
                "portfolio",
                userId,
                DedupKey(userId, RadarSignalTypes.CashBuffer, "portfolio", today),
                new
                {
                    cashPct = data.CashPct,
                    cashUsd = data.CashUsd,
                    minCashBufferPct = data.MinCashBufferPct,
                    totalUsd = data.TotalUsd,
                    compliant = !belowThreshold,
                },
                OneTime: true), ct));
        }

        // ── sync health ────────────────────────────────────────────────────────
        Track(await signals.AppendSignalAsync(new RadarSignalRequest(
            RadarScanners.Portfolio,
            RadarSignalTypes.SyncHealth,
            data.IsStale ? SignalSeverity.Notable : SignalSeverity.Info,
            RadarSubjectTypes.Portfolio,
            "portfolio",
            userId,
            DedupKey(userId, RadarSignalTypes.SyncHealth, "portfolio", today),
            new
            {
                isStale = data.IsStale,
                staleSources = data.StaleSources,
            },
            OneTime: true), ct));

        return (emitted, suppressed);
    }

    private static string DedupKey(Guid userId, string signalType, string subject, DateOnly date)
        => $"{RadarScanners.Portfolio}:{signalType}:{userId:N}:{subject}:{date:yyyy-MM-dd}";
}
