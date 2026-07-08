namespace FinanceSentry.Modules.Radar.Domain.Repositories;

using FinanceSentry.Modules.Radar.Domain;

public sealed record SignalFilter(
    DateTimeOffset? Since = null,
    string? Scanner = null,
    string? SignalType = null,
    string? Subject = null,
    Guid? UserId = null,
    string? Severity = null);

public interface IRadarSignalRepository
{
    Task AppendAsync(RadarSignal signal, CancellationToken ct = default);

    /// <summary>True if a signal with this DedupKey exists at/after <paramref name="since"/> (silence window).</summary>
    Task<bool> HasRecentAsync(string dedupKey, DateTimeOffset since, CancellationToken ct = default);

    Task<IReadOnlyList<RadarSignal>> ListAsync(SignalFilter filter, CancellationToken ct = default);

    /// <summary>Prunes <c>info</c> signals older than <paramref name="cutoff"/>. Returns rows removed.</summary>
    Task<int> PruneInfoBeforeAsync(DateTimeOffset cutoff, CancellationToken ct = default);
}
