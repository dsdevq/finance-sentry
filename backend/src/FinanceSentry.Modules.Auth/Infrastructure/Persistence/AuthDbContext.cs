using FinanceSentry.Modules.Auth.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinanceSentry.Modules.Auth.Infrastructure.Persistence;

public class AuthDbContext(DbContextOptions<AuthDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OAuthState> OAuthStates => Set<OAuthState>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.HasIndex(t => t.TokenHash).IsUnique();
            entity.HasIndex(t => t.UserId);
            entity.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
            entity.Property(t => t.UserId).IsRequired();
        });

        builder.Entity<OAuthState>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.State).HasMaxLength(64).IsRequired();
            entity.HasIndex(s => s.State).IsUnique();
            entity.HasIndex(s => s.ExpiresAt);
            entity.Property(s => s.IsUsed).HasDefaultValue(false);
            entity.Property(s => s.CreatedAt).IsRequired();
        });
    }
}
