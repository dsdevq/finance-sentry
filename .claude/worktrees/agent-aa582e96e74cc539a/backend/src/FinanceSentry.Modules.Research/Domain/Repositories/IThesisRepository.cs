namespace FinanceSentry.Modules.Research.Domain.Repositories;

public interface IThesisRepository
{
    Task<IReadOnlyList<InvestmentThesis>> ListAsync(Guid userId, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> GetUserIdsWithThesesAsync(CancellationToken ct = default);

    Task<InvestmentThesis?> FindAsync(Guid userId, Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<InvestmentThesis>> FindByTickerAsync(Guid userId, string ticker, CancellationToken ct = default);

    Task UpsertAsync(InvestmentThesis thesis, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default);
}
