namespace FinanceSentry.Modules.BankSync.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Domain.Repositories;

/// <summary>
/// Entity Framework Core implementation of IBankAccountRepository.
/// </summary>
public class BankAccountRepository : IBankAccountRepository
{
    private readonly BankSyncDbContext _context;

    public BankAccountRepository(BankSyncDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<BankAccount> AddAsync(BankAccount account, CancellationToken cancellationToken = default)
    {
        account.ValidateInvariants();
        await _context.BankAccounts.AddAsync(account, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<BankAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.BankAccounts
            .Include(ba => ba.EncryptedCredential)
            .FirstOrDefaultAsync(ba => ba.Id == id && ba.IsActive, cancellationToken);
    }

    public async Task<BankAccount?> GetByPlaidItemIdAsync(string plaidItemId, CancellationToken cancellationToken = default)
    {
        return await _context.BankAccounts
            .Include(ba => ba.EncryptedCredential)
            .FirstOrDefaultAsync(ba => ba.PlaidItemId == plaidItemId && ba.IsActive, cancellationToken);
    }

    public async Task<IEnumerable<BankAccount>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.BankAccounts
            .Where(ba => ba.UserId == userId && ba.IsActive)
            .Include(ba => ba.EncryptedCredential)
            .OrderByDescending(ba => ba.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<BankAccount> UpdateAsync(BankAccount account, CancellationToken cancellationToken = default)
    {
        account.ValidateInvariants();
        _context.BankAccounts.Update(account);
        await _context.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await _context.BankAccounts.FirstOrDefaultAsync(ba => ba.Id == id, cancellationToken);
        if (account == null)
            return false;

        account.IsActive = false;
        _context.BankAccounts.Update(account);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IEnumerable<BankAccount>> GetBySyncStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        return await _context.BankAccounts
            .Where(ba => ba.SyncStatus == status && ba.IsActive)
            .Include(ba => ba.EncryptedCredential)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<BankAccount>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.BankAccounts
            .Where(ba => ba.IsActive)
            .Include(ba => ba.EncryptedCredential)
            .OrderBy(ba => ba.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Entity Framework Core implementation of ITransactionRepository.
/// </summary>
public class TransactionRepository : ITransactionRepository
{
    private readonly BankSyncDbContext _context;

    public TransactionRepository(BankSyncDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        transaction.ValidateInvariants();
        await _context.Transactions.AddAsync(transaction, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return transaction;
    }

    public async Task<IEnumerable<Transaction>> AddRangeAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default)
    {
        var txList = transactions.ToList();
        foreach (var tx in txList)
            tx.ValidateInvariants();

        await _context.Transactions.AddRangeAsync(txList, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return txList;
    }

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.PostedDate ?? t.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetByAccountIdAsync(Guid accountId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.PostedDate ?? t.TransactionDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetByAccountIdAndDateAsync(Guid accountId, DateTime desde, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .Where(t => t.AccountId == accountId && (t.PostedDate >= desde || t.TransactionDate >= desde))
            .OrderByDescending(t => t.PostedDate ?? t.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByUniqueHashAsync(Guid accountId, string uniqueHash, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .AnyAsync(t => t.AccountId == accountId && t.UniqueHash == uniqueHash, cancellationToken);
    }

    public async Task<int> CountByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .CountAsync(t => t.AccountId == accountId, cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.PostedDate ?? t.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetByUserIdSinceAsync(Guid userId, DateTime since, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .Where(t => t.UserId == userId && (t.PostedDate >= since || t.TransactionDate >= since))
            .OrderByDescending(t => t.PostedDate ?? t.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task SoftDeleteByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        // IgnoreQueryFilters() bypasses the IsActive global filter so already-inactive rows
        // are also covered, making this operation idempotent.
        var transactions = await _context.Transactions
            .IgnoreQueryFilters()
            .Where(t => t.AccountId == accountId && t.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var t in transactions)
        {
            t.IsActive = false;
            t.DeletedAt = now;
            t.ArchivedReason = "account_deleted";
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Entity Framework Core implementation of ISyncJobRepository.
/// </summary>
public class SyncJobRepository : ISyncJobRepository
{
    private readonly BankSyncDbContext _context;

    public SyncJobRepository(BankSyncDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<SyncJob> AddAsync(SyncJob job, CancellationToken cancellationToken = default)
    {
        await _context.SyncJobs.AddAsync(job, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return job;
    }

    public async Task<SyncJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SyncJobs
            .FirstOrDefaultAsync(sj => sj.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<SyncJob>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _context.SyncJobs
            .Where(sj => sj.AccountId == accountId)
            .OrderByDescending(sj => sj.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<SyncJob?> GetLatestByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _context.SyncJobs
            .Where(sj => sj.AccountId == accountId)
            .OrderByDescending(sj => sj.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<SyncJob>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        return await _context.SyncJobs
            .Where(sj => sj.Status == status)
            .OrderByDescending(sj => sj.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<SyncJob> UpdateAsync(SyncJob job, CancellationToken cancellationToken = default)
    {
        _context.SyncJobs.Update(job);
        await _context.SaveChangesAsync(cancellationToken);
        return job;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await _context.SyncJobs.FirstOrDefaultAsync(sj => sj.Id == id, cancellationToken);
        if (job == null)
            return false;

        _context.SyncJobs.Remove(job);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> HasRunningJobAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _context.SyncJobs
            .AnyAsync(sj => sj.AccountId == accountId && sj.Status == "running", cancellationToken);
    }

    public async Task<SyncJob?> GetLatestSuccessfulByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.SyncJobs
            .Where(sj => sj.UserId == userId && sj.Status == "success")
            .OrderByDescending(sj => sj.CompletedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Entity Framework Core implementation of IEncryptedCredentialRepository.
/// </summary>
public class EncryptedCredentialRepository : IEncryptedCredentialRepository
{
    private readonly BankSyncDbContext _context;

    public EncryptedCredentialRepository(BankSyncDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<EncryptedCredential> AddAsync(EncryptedCredential credential, CancellationToken cancellationToken = default)
    {
        credential.ValidateInvariants();
        await _context.EncryptedCredentials.AddAsync(credential, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return credential;
    }

    public async Task<EncryptedCredential?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.EncryptedCredentials
            .FirstOrDefaultAsync(ec => ec.Id == id, cancellationToken);
    }

    public async Task<EncryptedCredential?> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _context.EncryptedCredentials
            .FirstOrDefaultAsync(ec => ec.AccountId == accountId, cancellationToken);
    }

    public async Task<EncryptedCredential> UpdateAsync(EncryptedCredential credential, CancellationToken cancellationToken = default)
    {
        credential.ValidateInvariants();
        _context.EncryptedCredentials.Update(credential);
        await _context.SaveChangesAsync(cancellationToken);
        return credential;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var credential = await _context.EncryptedCredentials.FirstOrDefaultAsync(ec => ec.Id == id, cancellationToken);
        if (credential == null)
            return false;

        _context.EncryptedCredentials.Remove(credential);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
