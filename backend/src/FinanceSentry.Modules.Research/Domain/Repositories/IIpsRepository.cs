namespace FinanceSentry.Modules.Research.Domain.Repositories;

public interface IIpsRepository
{
    /// <summary>The user's current (active) IPS, or null when none has been authored yet.</summary>
    Task<InvestmentPolicyStatement?> GetCurrentAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Full version history, newest first.</summary>
    Task<IReadOnlyList<InvestmentPolicyStatement>> ListVersionsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Highest existing version number for the user, or 0 when none exist.</summary>
    Task<int> GetMaxVersionAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Persists a new IPS version, demoting any prior current version.</summary>
    Task AddVersionAsync(InvestmentPolicyStatement ips, CancellationToken ct = default);

    /// <summary>Users with a current IPS on file — the books the opportunity scan nominates for.</summary>
    Task<IReadOnlyList<Guid>> GetUserIdsWithCurrentIpsAsync(CancellationToken ct = default);
}
