namespace FinanceSentry.Core.Interfaces;

/// <summary>
/// Cross-module read point for the shared <c>radar_signals</c> log. Defined in Core, implemented
/// in Modules.Radar; consumers (020's postmortem packet) read override signals without a Radar
/// module dependency.
/// </summary>
public interface IRadarSignalReader
{
    /// <summary>
    /// All override signals (risk-gate refusals proceeded past explicitly) recorded for the user
    /// in [<paramref name="from"/>, <paramref name="to"/>], newest first.
    /// </summary>
    Task<IReadOnlyList<RadarSignalRecord>> ListOverrideSignalsAsync(
        Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
}

public sealed record RadarSignalRecord(
    DateTimeOffset Timestamp,
    string Scanner,
    string SignalType,
    string Subject,
    IReadOnlyDictionary<string, object> Payload);
