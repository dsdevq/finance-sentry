namespace FinanceSentry.Integration;

using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Radar.Application.Queries;
using FinanceSentry.Modules.Radar.Domain.MarketStructure;
using FinanceSentry.Modules.Research.Domain.Ports;

/// <summary>
/// Feature 421 — implements <see cref="IAssetSignalReader"/> by delegating to the Radar module's
/// <see cref="ListSignalsQuery"/>. Lives in Integration so Modules.Research never references
/// Modules.Radar directly.
/// </summary>
public sealed class AssetSignalAdapter(
    IQueryHandler<ListSignalsQuery, IReadOnlyList<RadarSignalDto>> listSignals) : IAssetSignalReader
{
    private static readonly int LookbackDays = 30;

    public async Task<IReadOnlyList<DossierSignalItem>> GetRecentAsync(
        string symbol, int limit, CancellationToken ct = default)
    {
        var since = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-LookbackDays));

        var signals = await listSignals.Handle(
            new ListSignalsQuery(since, null, null, symbol, null),
            ct);

        return signals
            .OrderByDescending(s => s.Timestamp)
            .Take(limit)
            .Select(s => new DossierSignalItem(
                Timestamp: s.Timestamp,
                Scanner: s.Scanner,
                SignalType: s.SignalType,
                Severity: s.Severity,
                Payload: s.Payload))
            .ToList();
    }
}
