namespace AuthServer.Domain.Entities;

public sealed class UserRole
{
    public Guid UserId { get; set; }

    public Guid RoleId { get; set; }

    public DateTimeOffset AssignedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Guid? AssignedByUserId { get; set; }

    public User User { get; set; } = default!;

    public Role Role { get; set; } = default!;
}
