using FinanceSentry.Modules.BrokerageSync.Domain;
using Microsoft.EntityFrameworkCore;

namespace FinanceSentry.Modules.BrokerageSync.Infrastructure.Persistence;

public sealed class BrokerageSyncDbContext : DbContext
{
    public BrokerageSyncDbContext(DbContextOptions<BrokerageSyncDbContext> options)
        : base(options)
    {
    }

    public DbSet<IBKRCredential> IBKRCredentials => Set<IBKRCredential>();
    public DbSet<BrokerageHolding> BrokerageHoldings => Set<BrokerageHolding>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("brokerage_sync");
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<IBKRCredential>(entity =>
        {
            entity.ToTable("IBKRCredentials");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.AccountId).HasMaxLength(20);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastSyncError).HasMaxLength(1000);
            entity.Property(e => e.EncryptedUsername).IsRequired().HasColumnType("bytea");
            entity.Property(e => e.UsernameIv).IsRequired().HasColumnType("bytea");
            entity.Property(e => e.UsernameAuthTag).IsRequired().HasColumnType("bytea");
            entity.Property(e => e.EncryptedPassword).IsRequired().HasColumnType("bytea");
            entity.Property(e => e.PasswordIv).IsRequired().HasColumnType("bytea");
            entity.Property(e => e.PasswordAuthTag).IsRequired().HasColumnType("bytea");
            entity.Property(e => e.KeyVersion).IsRequired().HasDefaultValue(1);
        });

        modelBuilder.Entity<BrokerageHolding>(entity =>
        {
            entity.ToTable("BrokerageHoldings");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Symbol, e.Provider }).IsUnique();
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(50);
            entity.Property(e => e.InstrumentType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Quantity).HasPrecision(30, 10);
            entity.Property(e => e.UsdValue).HasPrecision(20, 4);
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(20).HasDefaultValue("ibkr");
            entity.Property(e => e.AverageCostUsd).HasPrecision(20, 8);
            entity.Property(e => e.CostBasisUsd).HasPrecision(20, 4);
        });
    }
}
