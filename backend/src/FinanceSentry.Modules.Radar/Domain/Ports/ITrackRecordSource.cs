namespace FinanceSentry.Modules.Radar.Domain.Ports;

/// <summary>
/// Cross-module port (feature 414, US4). Supplies the weekly brief with the thesis track record so
/// the digest can state whether Denys' own calls beat the benchmark. The adapter lives in
/// FinanceSentry.Integration so Modules.Radar never depends on Modules.Research directly.
/// </summary>
public interface ITrackRecordSource
{
    /// <summary>
    /// Returns null when the user has no evaluable thesis record at all, so the brief can omit
    /// the line rather than print zeros.
    /// </summary>
    Task<TrackRecordDelta?> GetDeltaAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>
/// One non-blended slice of the track record. Terminal (closed + broken) records are reported when
/// any exist, otherwise the open ones — feature 020 R4 forbids averaging the two together.
/// Percentages are 0–100.
/// </summary>
/// <param name="IsTerminal">True when the numbers describe closed/broken calls, false for open ones.</param>
/// <param name="Count">Records behind the numbers.</param>
/// <param name="HitRatePct">Share of records that beat the benchmark.</param>
/// <param name="AverageExcessReturnPct">Mean return above the benchmark.</param>
/// <param name="LowSample">True when too few closed records exist to trust the hit rate.</param>
public sealed record TrackRecordDelta(
    bool IsTerminal,
    int Count,
    decimal? HitRatePct,
    decimal? AverageExcessReturnPct,
    bool LowSample);
