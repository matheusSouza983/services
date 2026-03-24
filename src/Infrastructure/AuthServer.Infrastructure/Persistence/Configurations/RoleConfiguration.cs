using AuthServer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthServer.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(current => current.Id);

        builder.Property(current => current.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(current => current.Description)
            .HasMaxLength(500);

        builder.Property(current => current.CreatedAtUtc)
            .IsRequired();

        builder.Property(current => current.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(current => current.Name)
            .IsUnique();
    }
}
