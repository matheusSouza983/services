namespace AuthServer.API.Authorization;

public static class AuthorizationPolicies
{
    public const string AuthenticatedOnly = nameof(AuthenticatedOnly);
    public const string AdminOnly = nameof(AdminOnly);
    public const string UserOrAdmin = nameof(UserOrAdmin);

    public const string AdminRole = "admin";
    public const string UserRole = "user";
}
