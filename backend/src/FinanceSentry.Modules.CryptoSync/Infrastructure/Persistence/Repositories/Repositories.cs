using FinanceSentry.Modules.CryptoSync.Domain;
using FinanceSentry.Modules.CryptoSync.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinanceSentry.Modules.CryptoSync.Infrastructure.Persistence.Repositories;

public sealed class BinanceCredentialRepository : IBinanceCredentialRepository
{
    private readonly CryptoSyncDbContext _context;

    public BinanceCredentialRepository(CryptoSyncDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(BinanceCredential credential, CancellationToken ct = default)
    {
        await _context.BinanceCredentials.AddAsync(credential, ct);
    }

    public async Task<BinanceCredential?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.BinanceCredentials
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);
    }

    public async Task<IReadOnlyList<BinanceCredential>> GetAllActiveAsync(CancellationToken ct = default)
    {
        return await _context.BinanceCredentials
            .Where(c => c.IsActive)
            .ToListAsync(ct);
    }

    public void Update(BinanceCredential credential)
    {
        _context.BinanceCredentials.Update(credential);
    }

    public void Delete(BinanceCredential credential)
    {
        _context.BinanceCredentials.Remove(credential);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}

public sealed class CryptoHoldingRepository : ICryptoHoldingRepository
{
    private readonly CryptoSyncDbContext _context;

    public CryptoHoldingRepository(CryptoSyncDbContext context)
    {
        _context = context;
    }

    public async Task UpsertRangeAsync(IReadOnlyList<CryptoHolding> holdings, CancellationToken ct = default)
    {
        foreach (var holding in holdings)
        {
            var existing = await _context.CryptoHoldings
                .FirstOrDefaultAsync(h => h.UserId == holding.UserId && h.Asset == holding.Asset, ct);

            if (existing is not null)
            {
                existing.Update(holding.FreeQuantity, holding.LockedQuantity, holding.UsdValue);
                _context.CryptoHoldings.Update(existing);
            }
            else
            {
                await _context.CryptoHoldings.AddAsync(holding, ct);
            }
        }
    }

    public async Task<IReadOnlyList<CryptoHolding>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.CryptoHoldings
            .Where(h => h.UserId == userId)
            .ToListAsync(ct);
    }

    public async Task DeleteByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        await _context.CryptoHoldings
            .Where(h => h.UserId == userId)
            .ExecuteDeleteAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
