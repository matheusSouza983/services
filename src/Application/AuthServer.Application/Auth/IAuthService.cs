namespace AuthServer.Application.Auth;

public interface IAuthService
{
    Task<LoginResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<LoginResult?> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
}
