using AuthServer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Infrastructure.Persistence;

public sealed class AuthDbContext(
    DbContextOptions<AuthDbContext> options,
    Security.ITotpSecretProtector totpSecretProtector) : DbContext(options)
{
    private readonly Security.ITotpSecretProtector _totpSecretProtector = totpSecretProtector;

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserClaim> UserClaims => Set<UserClaim>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public override int SaveChanges()
    {
        NormalizeUsers();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        NormalizeUsers();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        NormalizeUsers();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        NormalizeUsers();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);

        modelBuilder.Entity<User>()
            .Property(current => current.TotpSecret)
            .HasConversion(
                current => current == null ? null : _totpSecretProtector.Protect(current),
                current => current == null ? null : _totpSecretProtector.Unprotect(current));
    }

    private void NormalizeUsers()
    {
        var userEntries = ChangeTracker.Entries<User>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in userEntries)
        {
            entry.Entity.NormalizedUserName = NormalizeIdentity(entry.Entity.UserName);
            entry.Entity.NormalizedEmail = NormalizeIdentity(entry.Entity.Email);
        }
    }

    private static string NormalizeIdentity(string value)
    {
        return value.Trim().ToUpperInvariant();
    }
}