using AuthServer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthServer.Infrastructure.Persistence.Configurations;

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");
        builder.HasKey(current => new { current.UserId, current.RoleId });

        builder.Property(current => current.AssignedAtUtc)
            .IsRequired();

        builder.HasOne(current => current.User)
            .WithMany(user => user.UserRoles)
            .HasForeignKey(current => current.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(current => current.Role)
            .WithMany(role => role.UserRoles)
            .HasForeignKey(current => current.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(current => current.RoleId);
    }
}
