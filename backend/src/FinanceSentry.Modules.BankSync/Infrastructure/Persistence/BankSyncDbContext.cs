namespace FinanceSentry.Modules.BankSync.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using FinanceSentry.Modules.BankSync.Domain;

public class BankSyncDbContext(DbContextOptions<BankSyncDbContext> options) : DbContext(options)
{
    public DbSet<BankAccount> BankAccounts { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<SyncJob> SyncJobs { get; set; } = null!;
    public DbSet<EncryptedCredential> EncryptedCredentials { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<MonobankCredential> MonobankCredentials { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var bab = modelBuilder.Entity<BankAccount>();
        bab.HasKey(ba => ba.Id);
        bab.HasIndex(ba => ba.UserId).HasDatabaseName("idx_bank_account_user_id");
        bab.HasIndex(ba => ba.SyncStatus).HasDatabaseName("idx_bank_account_sync_status");
        bab.HasIndex(ba => ba.ExternalAccountId).IsUnique().HasDatabaseName("idx_bank_account_external_account_id_unique");
        bab.Property(ba => ba.ExternalAccountId).IsRequired().HasMaxLength(64);
        bab.Property(ba => ba.Provider).IsRequired().HasMaxLength(20).HasDefaultValue("plaid");
        bab.Property(ba => ba.BankName).IsRequired().HasMaxLength(255);
        bab.Property(ba => ba.AccountType).IsRequired().HasMaxLength(50);
        bab.Property(ba => ba.AccountNumberLast4).IsRequired().HasMaxLength(4);
        bab.Property(ba => ba.OwnerName).IsRequired().HasMaxLength(255);
        bab.Property(ba => ba.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("EUR");
        bab.Property(ba => ba.CurrentBalance).HasPrecision(15, 2);
        bab.Property(ba => ba.SyncStatus).IsRequired().HasMaxLength(50).HasDefaultValue("pending");
        bab.Property(ba => ba.IsActive).HasDefaultValue(true);
        bab.Property(ba => ba.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        bab.HasMany(ba => ba.Transactions).WithOne(t => t.Account).HasForeignKey(t => t.AccountId).OnDelete(DeleteBehavior.Cascade);
        bab.HasMany(ba => ba.SyncJobs).WithOne(sj => sj.Account).HasForeignKey(sj => sj.AccountId).OnDelete(DeleteBehavior.Cascade);
        bab.HasOne(ba => ba.EncryptedCredential).WithOne(ec => ec.Account).HasForeignKey<EncryptedCredential>(ec => ec.AccountId).OnDelete(DeleteBehavior.Cascade);
        bab.HasOne(ba => ba.MonobankCredential).WithMany(mc => mc.BankAccounts).HasForeignKey(ba => ba.MonobankCredentialId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);

        var mcb = modelBuilder.Entity<MonobankCredential>();
        mcb.HasKey(mc => mc.Id);
        mcb.HasIndex(mc => mc.UserId).HasDatabaseName("idx_monobank_credential_user_id");
        mcb.HasIndex(mc => mc.UserId).IsUnique().HasDatabaseName("idx_monobank_credential_user_unique");
        mcb.Property(mc => mc.EncryptedToken).IsRequired();
        mcb.Property(mc => mc.Iv).IsRequired();
        mcb.Property(mc => mc.AuthTag).IsRequired();
        mcb.Property(mc => mc.KeyVersion).HasDefaultValue(1);
        mcb.Property(mc => mc.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

        var tb = modelBuilder.Entity<Transaction>();
        tb.HasKey(t => t.Id);
        tb.HasIndex(t => t.AccountId).HasDatabaseName("idx_transaction_account_id");
        tb.HasIndex(t => t.PostedDate).HasDatabaseName("idx_transaction_posted_date");
        tb.HasIndex(t => t.CreatedAt).HasDatabaseName("idx_transaction_created_at");
        tb.HasIndex(t => new { t.AccountId, t.UniqueHash }).IsUnique().HasDatabaseName("idx_transaction_account_unique_hash_unique");
        tb.Property(t => t.Amount).IsRequired().HasPrecision(15, 2);
        tb.Property(t => t.Description).IsRequired();
        tb.Property(t => t.UniqueHash).IsRequired().HasMaxLength(64);
        tb.Property(t => t.TransactionType).HasMaxLength(50);
        tb.Property(t => t.MerchantName).HasMaxLength(255);
        tb.Property(t => t.MerchantCategory).HasMaxLength(100).IsRequired(false);
        tb.Property(t => t.IsActive).HasDefaultValue(true);
        tb.Property(t => t.DeletedAt).IsRequired(false);
        tb.Property(t => t.ArchivedReason).HasMaxLength(50).IsRequired(false);
        tb.HasQueryFilter(t => t.IsActive);
        tb.HasIndex(t => new { t.AccountId, t.IsActive }).HasDatabaseName("idx_transaction_account_active");
        tb.Property(t => t.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

        var sjb = modelBuilder.Entity<SyncJob>();
        sjb.HasKey(sj => sj.Id);
        sjb.HasIndex(sj => sj.AccountId).HasDatabaseName("idx_sync_job_account_id");
        sjb.HasIndex(sj => sj.Status).HasDatabaseName("idx_sync_job_status");
        sjb.HasIndex(sj => sj.CreatedAt).HasDatabaseName("idx_sync_job_created_at");
        sjb.Property(sj => sj.Status).IsRequired().HasMaxLength(50).HasDefaultValue("pending");
        sjb.Property(sj => sj.UserId).IsRequired();
        sjb.Property(sj => sj.CorrelationId).HasMaxLength(100).IsRequired(false);
        sjb.Property(sj => sj.ErrorMessage).HasMaxLength(1000);
        sjb.Property(sj => sj.ErrorCode).HasMaxLength(50);
        sjb.Property(sj => sj.TransactionCountFetched).HasDefaultValue(0);
        sjb.Property(sj => sj.TransactionCountDeduped).HasDefaultValue(0);
        sjb.Property(sj => sj.RetryCount).HasDefaultValue(0);
        sjb.Property(sj => sj.WebhookTriggered).HasDefaultValue(false);
        sjb.HasIndex(sj => new { sj.AccountId, sj.Status }).HasDatabaseName("idx_sync_job_account_status");
        sjb.Property(sj => sj.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

        var ecb = modelBuilder.Entity<EncryptedCredential>();
        ecb.HasKey(ec => ec.Id);
        ecb.HasIndex(ec => ec.AccountId).IsUnique().HasDatabaseName("idx_encrypted_credential_account_id_unique");
        ecb.Property(ec => ec.EncryptedData).IsRequired();
        ecb.Property(ec => ec.Iv).IsRequired();
        ecb.Property(ec => ec.AuthTag).IsRequired();
        ecb.Property(ec => ec.KeyVersion).HasDefaultValue(1);
        ecb.Property(ec => ec.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

        var alb = modelBuilder.Entity<AuditLog>();
        alb.ToTable("audit_logs");
        alb.HasKey(al => al.AuditId);
        alb.HasIndex(al => new { al.UserId, al.PerformedAt }).HasDatabaseName("idx_audit_log_user_performed_at");
        alb.HasIndex(al => new { al.ResourceType, al.ResourceId }).HasDatabaseName("idx_audit_log_resource");
        alb.Property(al => al.Action).IsRequired().HasMaxLength(50);
        alb.Property(al => al.ResourceType).IsRequired().HasMaxLength(50);
        alb.Property(al => al.IpAddress).HasMaxLength(45).IsRequired(false);
        alb.Property(al => al.CorrelationId).HasMaxLength(64).IsRequired(false);
        alb.Property(al => al.PerformedAt).IsRequired();
    }
}
