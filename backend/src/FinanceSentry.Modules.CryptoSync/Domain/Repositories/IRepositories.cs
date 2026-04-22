namespace FinanceSentry.Modules.CryptoSync.Domain.Repositories;

public interface IBinanceCredentialRepository
{
    Task AddAsync(BinanceCredential credential, CancellationToken ct = default);
    Task<BinanceCredential?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<BinanceCredential>> GetAllActiveAsync(CancellationToken ct = default);
    void Update(BinanceCredential credential);
    void Delete(BinanceCredential credential);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface ICryptoHoldingRepository
{
    Task UpsertRangeAsync(IReadOnlyList<CryptoHolding> holdings, CancellationToken ct = default);
    Task<IReadOnlyList<CryptoHolding>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task DeleteByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
