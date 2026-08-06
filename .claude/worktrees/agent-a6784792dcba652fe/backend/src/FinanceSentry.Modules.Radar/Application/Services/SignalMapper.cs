namespace FinanceSentry.Modules.Radar.Application.Services;

using FinanceSentry.Modules.Radar.Domain;
using FinanceSentry.Modules.Radar.Domain.MarketStructure;

/// <summary>Maps a persisted <see cref="RadarSignal"/> to its read DTO.</summary>
public static class SignalMapper
{
    public static RadarSignalDto ToDto(RadarSignal s) => new(
        s.Timestamp,
        s.Scanner,
        s.SignalType,
        s.Severity.ToString().ToLowerInvariant(),
        s.SubjectType,
        s.Subject,
        s.DedupKey,
        s.Payload,
        s.PayloadVersion);
}
