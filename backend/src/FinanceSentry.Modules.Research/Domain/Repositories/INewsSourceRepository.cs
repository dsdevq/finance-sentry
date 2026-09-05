namespace FinanceSentry.Modules.Research.Domain.Repositories;

using FinanceSentry.Modules.Research.Domain;

public interface INewsSourceRepository
{
    Task<IReadOnlyList<NewsSource>> ListEnabledAsync(CancellationToken ct = default);

    Task<IReadOnlyList<NewsSource>> ListAllAsync(CancellationToken ct = default);

    Task<NewsSource?> GetByUrlAsync(string url, CancellationToken ct = default);

    Task<Guid> AddAsync(NewsSource source, CancellationToken ct = default);

    Task UpdateAsync(NewsSource source, CancellationToken ct = default);

    /// <summary>Drops a source row outright — used to retire a superseded duplicate (issue #318).</summary>
    Task RemoveAsync(NewsSource source, CancellationToken ct = default);
}
