using AuthServer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthServer.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(current => current.Id);

        builder.Property(current => current.UserName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(current => current.NormalizedUserName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(current => current.Email)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(current => current.NormalizedEmail)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(current => current.EmailVerified)
            .IsRequired();

        builder.Property(current => current.PhoneNumber)
            .HasMaxLength(30);

        builder.Property(current => current.PhoneNumberVerified)
            .IsRequired();

        builder.Property(current => current.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(current => current.PasswordLastChangedUtc);

        builder.Property(current => current.IsLocked)
            .IsRequired();

        builder.Property(current => current.LockoutEndUtc);

        builder.Property(current => current.MfaEnabled)
            .IsRequired();

        builder.Property(current => current.TotpSecret)
            .HasMaxLength(200);

        builder.Property(current => current.AttributesJson)
            .HasColumnType("TEXT");

        builder.Property(current => current.LastLoginUtc);

        builder.Property(current => current.CreatedAtUtc)
            .IsRequired();

        builder.Property(current => current.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(current => current.NormalizedUserName)
            .IsUnique();

        builder.HasIndex(current => current.NormalizedEmail)
            .IsUnique();
    }
}