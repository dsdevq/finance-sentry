namespace FinanceSentry.Modules.Radar.Domain.Repositories;

using FinanceSentry.Modules.Radar.Domain;

public interface IRadarUniverseRepository
{
    Task<IReadOnlyList<RadarUniverseMember>> ListActiveAsync(CancellationToken ct = default);

    Task<IReadOnlyList<RadarUniverseMember>> ListAllAsync(CancellationToken ct = default);

    /// <summary>Inserts new members and re-activates returning ones. Idempotent on Ticker.</summary>
    Task UpsertMembersAsync(IReadOnlyCollection<RadarUniverseMember> members, CancellationToken ct = default);

    /// <summary>De-activates (does not delete) the given tickers.</summary>
    Task DeactivateAsync(IReadOnlyCollection<string> tickers, CancellationToken ct = default);
}
