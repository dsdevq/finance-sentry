namespace FinanceSentry.Core.Interfaces;

/// <summary>
/// Cross-module append point for the shared <c>radar_signals</c> log. Defined in Core,
/// implemented once in Modules.Radar; other scanners (017/019) inject it to append signals
/// with no dependency on the Radar module.
/// </summary>
public interface IRadarSignalWriter
{
    /// <summary>
    /// Appends a signal. <c>notable</c>+ signals are suppressed within the configured silence
    /// window by <see cref="RadarSignalRequest.DedupKey"/>; <c>info</c> is recorded every run.
    /// A <see cref="RadarSignalRequest.OneTime"/> signal (any severity) is suppressed forever
    /// once its DedupKey has ever been recorded. Returns true when the signal was appended,
    /// false when suppressed — callers gating an Alert on a fresh signal use the return value.
    /// </summary>
    Task<bool> AppendSignalAsync(RadarSignalRequest request, CancellationToken ct = default);
}

/// <summary>Severity of a radar signal. Lives in Core because it crosses the module boundary.</summary>
public enum SignalSeverity
{
    Info,
    Notable,
    Alerted,
}

public sealed record RadarSignalRequest(
    string Scanner,
    string SignalType,
    SignalSeverity Severity,
    string SubjectType,
    string Subject,
    Guid? UserId,
    string DedupKey,
    object Payload,
    int PayloadVersion = 1,
    bool OneTime = false);
