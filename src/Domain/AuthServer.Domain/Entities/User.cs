namespace AuthServer.Domain.Entities;

public sealed class User
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool EmailVerified { get; set; }

    public string? PhoneNumber { get; set; }

    public bool PhoneNumberVerified { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    public DateTimeOffset? PasswordLastChangedUtc { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsLocked { get; set; }

    public DateTimeOffset? LockoutEndUtc { get; set; }

    public bool MfaEnabled { get; set; }

    public string? TotpSecret { get; set; }

    public string? AttributesJson { get; set; }

    public DateTimeOffset? LastLoginUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public ICollection<UserClaim> Claims { get; set; } = new List<UserClaim>();

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}