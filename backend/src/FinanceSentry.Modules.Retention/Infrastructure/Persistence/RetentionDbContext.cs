namespace FinanceSentry.Modules.Retention.Infrastructure.Persistence;

using FinanceSentry.Modules.Retention.Domain;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Self-contained store for retention/backup run records (feature 024, schema <c>retention</c>).
/// Holds no user data and references no other module's tables — the purge engine reaches other
/// schemas only by name string via raw batched SQL, so there is no cross-module coupling here.
/// </summary>
public class RetentionDbContext(DbContextOptions<RetentionDbContext> options) : DbContext(options)
{
    public const string Schema = "retention";

    public DbSet<RetentionRun> RetentionRuns => Set<RetentionRun>();

    public DbSet<BackupRun> BackupRuns => Set<BackupRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<RetentionRun>(e =>
        {
            e.ToTable("retention_runs");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.RunType, x.StartedAt });
            e.Property(x => x.RunType).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.Outcome).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.TableResults).HasColumnType("jsonb");
            e.Property(x => x.Error).HasMaxLength(4000);
        });

        modelBuilder.Entity<BackupRun>(e =>
        {
            e.ToTable("backup_runs");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.CreatedAt);
            // Powers SC-002: "the last backup that provably restores".
            e.HasIndex(x => x.VerifiedAt)
                .HasFilter("\"VerificationStatus\" = 'Verified'");
            e.Property(x => x.Kind).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.VerificationStatus).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.ArtifactKey).HasMaxLength(256);
            e.Property(x => x.Sha256).HasMaxLength(64);
            e.Property(x => x.Error).HasMaxLength(4000);
        });
    }
}
