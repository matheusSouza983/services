using AuthServer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthServer.Infrastructure.Persistence.Configurations;

public sealed class UserClaimConfiguration : IEntityTypeConfiguration<UserClaim>
{
    public void Configure(EntityTypeBuilder<UserClaim> builder)
    {
        builder.ToTable("UserClaims");
        builder.HasKey(current => current.Id);

        builder.Property(current => current.Type)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(current => current.Value)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(current => current.CreatedAtUtc)
            .IsRequired();

        builder.HasOne(current => current.User)
            .WithMany(user => user.Claims)
            .HasForeignKey(current => current.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(current => new { current.UserId, current.Type });
    }
}
