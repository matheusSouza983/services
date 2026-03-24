using AuthServer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthServer.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(current => current.Id);

        builder.Property(current => current.TokenHash)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(current => current.ExpiresAtUtc)
            .IsRequired();

        builder.Property(current => current.CreatedAtUtc)
            .IsRequired();

        builder.Property(current => current.RevokedAtUtc);

        builder.Property(current => current.ReplacedByTokenId);

        builder.HasOne(current => current.User)
            .WithMany(user => user.RefreshTokens)
            .HasForeignKey(current => current.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(current => current.TokenHash)
            .IsUnique();

        builder.HasIndex(current => new { current.UserId, current.RevokedAtUtc, current.ExpiresAtUtc });
    }
}
