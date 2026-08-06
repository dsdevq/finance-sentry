namespace FinanceSentry.Modules.Radar.Application.Services;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Radar.Domain.Repositories;

/// <summary><see cref="IRadarSignalReader"/> impl over the shared signal log.</summary>
public sealed class RadarSignalReader(IRadarSignalRepository signals) : IRadarSignalReader
{
    // Every scanner records an explicit human override under one of these types (FR-007 in 022,
    // FR-011b in 019). New override-producing scanners must be added here.
    private static readonly string[] OverrideSignalTypes = ["risk_override", "promotion_risk_override"];

    public async Task<IReadOnlyList<RadarSignalRecord>> ListOverrideSignalsAsync(
        Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        var records = new List<RadarSignalRecord>();
        foreach (var signalType in OverrideSignalTypes)
        {
            var rows = await signals.ListAsync(
                new SignalFilter(Since: from, SignalType: signalType, UserId: userId), ct);
            records.AddRange(rows
                .Where(s => s.Timestamp <= to)
                .Select(s => new RadarSignalRecord(s.Timestamp, s.Scanner, s.SignalType, s.Subject, s.Payload)));
        }

        return records.OrderByDescending(r => r.Timestamp).ToList();
    }
}
