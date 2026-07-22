namespace FinanceSentry.Modules.Research.Domain.Repositories;

using FinanceSentry.Modules.Research.Domain;

public interface IAnalystUniverseRepository
{
    Task<IReadOnlyList<AnalystUniverseMember>> ListActiveAsync(CancellationToken ct = default);

    Task<IReadOnlyList<AnalystUniverseMember>> ListAllAsync(CancellationToken ct = default);

    /// <summary>Insert new members / re-activate existing ones (matched by ticker), setting their reason.</summary>
    Task UpsertMembersAsync(IReadOnlyCollection<AnalystUniverseMember> members, CancellationToken ct = default);

    /// <summary>Flip the given tickers to inactive (rows retained).</summary>
    Task DeactivateAsync(IReadOnlyCollection<string> tickers, CancellationToken ct = default);

    /// <summary>True when the ticker is an active universe member (drives the coverage distinction).</summary>
    Task<bool> IsInUniverseAsync(string ticker, CancellationToken ct = default);
}
