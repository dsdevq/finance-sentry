namespace FinanceSentry.Modules.BrokerageSync.Domain.Repositories;

public interface IIBKRCredentialRepository
{
    Task AddAsync(IBKRCredential credential, CancellationToken ct = default);
    Task<IBKRCredential?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<IBKRCredential>> GetAllActiveAsync(CancellationToken ct = default);
    void Update(IBKRCredential credential);
    void Delete(IBKRCredential credential);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IBrokerageHoldingRepository
{
    Task UpsertRangeAsync(IEnumerable<BrokerageHolding> holdings, CancellationToken ct = default);
    Task<IReadOnlyList<BrokerageHolding>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task DeleteByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
