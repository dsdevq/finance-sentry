namespace FinanceSentry.Modules.BankSync.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using FinanceSentry.Modules.BankSync.Domain;

/// <summary>
/// Entity Framework Core DbContext for BankSync module.
/// Configures entities, relationships, indexes, and conversions.
/// </summary>
public class BankSyncDbContext : DbContext
{
    public BankSyncDbContext(DbContextOptions<BankSyncDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<BankAccount> BankAccounts { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<SyncJob> SyncJobs { get; set; } = null!;
    public DbSet<EncryptedCredential> EncryptedCredentials { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure BankAccount entity
        var bankAccountBuilder = modelBuilder.Entity<BankAccount>();
        
        bankAccountBuilder.HasKey(ba => ba.Id);
        
        bankAccountBuilder.HasIndex(ba => ba.UserId).HasDatabaseName("idx_bank_account_user_id");
        bankAccountBuilder.HasIndex(ba => ba.SyncStatus).HasDatabaseName("idx_bank_account_sync_status");
        bankAccountBuilder.HasIndex(ba => ba.PlaidItemId).IsUnique().HasDatabaseName("idx_bank_account_plaid_item_id_unique");

        bankAccountBuilder.Property(ba => ba.PlaidItemId)
            .IsRequired()
            .HasMaxLength(24);

        bankAccountBuilder.Property(ba => ba.BankName)
            .IsRequired()
            .HasMaxLength(255);

        bankAccountBuilder.Property(ba => ba.AccountType)
            .IsRequired()
            .HasMaxLength(50);

        bankAccountBuilder.Property(ba => ba.AccountNumberLast4)
            .IsRequired()
            .HasMaxLength(4);

        bankAccountBuilder.Property(ba => ba.OwnerName)
            .IsRequired()
            .HasMaxLength(255);

        bankAccountBuilder.Property(ba => ba.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("EUR");

        bankAccountBuilder.Property(ba => ba.CurrentBalance)
            .HasPrecision(15, 2);

        bankAccountBuilder.Property(ba => ba.SyncStatus)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("pending");

        bankAccountBuilder.Property(ba => ba.IsActive)
            .HasDefaultValue(true);

        bankAccountBuilder.Property(ba => ba.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        bankAccountBuilder.HasMany(ba => ba.Transactions)
            .WithOne(t => t.Account)
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        bankAccountBuilder.HasMany(ba => ba.SyncJobs)
            .WithOne(sj => sj.Account)
            .HasForeignKey(sj => sj.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        bankAccountBuilder.HasOne(ba => ba.EncryptedCredential)
            .WithOne(ec => ec.Account)
            .HasForeignKey<EncryptedCredential>(ec => ec.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Transaction entity
        var transactionBuilder = modelBuilder.Entity<Transaction>();

        transactionBuilder.HasKey(t => t.Id);

        transactionBuilder.HasIndex(t => t.AccountId).HasDatabaseName("idx_transaction_account_id");
        transactionBuilder.HasIndex(t => t.PostedDate).HasDatabaseName("idx_transaction_posted_date");
        transactionBuilder.HasIndex(t => t.CreatedAt).HasDatabaseName("idx_transaction_created_at");
        transactionBuilder.HasIndex(t => new { t.AccountId, t.UniqueHash })
            .IsUnique()
            .HasDatabaseName("idx_transaction_account_unique_hash_unique");

        transactionBuilder.Property(t => t.Amount)
            .IsRequired()
            .HasPrecision(15, 2);

        transactionBuilder.Property(t => t.Description)
            .IsRequired();

        transactionBuilder.Property(t => t.UniqueHash)
            .IsRequired()
            .HasMaxLength(64);

        transactionBuilder.Property(t => t.TransactionType)
            .HasMaxLength(50);

        transactionBuilder.Property(t => t.MerchantName)
            .HasMaxLength(255);

        transactionBuilder.Property(t => t.MerchantCategory)
            .HasMaxLength(100)
            .IsRequired(false);

        // Soft-delete support (T202/T309-A): IsActive=false hides deleted transactions from all user queries.
        // Use IgnoreQueryFilters() in audit/admin contexts only.
        transactionBuilder.Property(t => t.IsActive)
            .HasDefaultValue(true);

        transactionBuilder.Property(t => t.DeletedAt)
            .IsRequired(false);

        transactionBuilder.Property(t => t.ArchivedReason)
            .HasMaxLength(50)
            .IsRequired(false);

        transactionBuilder.HasQueryFilter(t => t.IsActive);

        transactionBuilder.HasIndex(t => new { t.AccountId, t.IsActive })
            .HasDatabaseName("idx_transaction_account_active");

        transactionBuilder.Property(t => t.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Configure SyncJob entity
        var syncJobBuilder = modelBuilder.Entity<SyncJob>();

        syncJobBuilder.HasKey(sj => sj.Id);

        syncJobBuilder.HasIndex(sj => sj.AccountId).HasDatabaseName("idx_sync_job_account_id");
        syncJobBuilder.HasIndex(sj => sj.Status).HasDatabaseName("idx_sync_job_status");
        syncJobBuilder.HasIndex(sj => sj.CreatedAt).HasDatabaseName("idx_sync_job_created_at");

        syncJobBuilder.Property(sj => sj.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("pending");

        syncJobBuilder.Property(sj => sj.ErrorMessage)
            .HasMaxLength(1000);

        syncJobBuilder.Property(sj => sj.ErrorCode)
            .HasMaxLength(50);

        syncJobBuilder.Property(sj => sj.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Configure EncryptedCredential entity
        var encryptedCredBuilder = modelBuilder.Entity<EncryptedCredential>();

        encryptedCredBuilder.HasKey(ec => ec.Id);

        encryptedCredBuilder.HasIndex(ec => ec.AccountId)
            .IsUnique()
            .HasDatabaseName("idx_encrypted_credential_account_id_unique");

        encryptedCredBuilder.Property(ec => ec.EncryptedData)
            .IsRequired();

        encryptedCredBuilder.Property(ec => ec.Iv)
            .IsRequired();

        encryptedCredBuilder.Property(ec => ec.AuthTag)
            .IsRequired();

        encryptedCredBuilder.Property(ec => ec.KeyVersion)
            .HasDefaultValue(1);

        encryptedCredBuilder.Property(ec => ec.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
