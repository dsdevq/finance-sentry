namespace FinanceSentry.Modules.Radar.Domain;

using FinanceSentry.Core.Interfaces;

/// <summary>
/// A row in the shared append-only <c>radar_signals</c> log. Written by market-structure and,
/// via <see cref="IRadarSignalWriter"/>, any other scanner.
/// </summary>
public sealed class RadarSignal
{
    public Guid Id { get; init; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string Scanner { get; set; } = string.Empty;
    public string SignalType { get; set; } = string.Empty;
    public SignalSeverity Severity { get; set; }
    public string SubjectType { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;

    /// <summary>Set for holder-scoped signals; null for global ones (breadth, rotation).</summary>
    public Guid? UserId { get; set; }

    public string DedupKey { get; set; } = string.Empty;

    /// <summary>Computed evidence, persisted as jsonb. Values deserialize as JsonElement.</summary>
    public Dictionary<string, object> Payload { get; set; } = new();

    public int PayloadVersion { get; set; } = 1;
}
