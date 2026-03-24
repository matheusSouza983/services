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

        builder.Property(current => current.Email)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(current => current.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(current => current.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(current => current.UserName)
            .IsUnique();

        builder.HasIndex(current => current.Email)
            .IsUnique();
    }
}