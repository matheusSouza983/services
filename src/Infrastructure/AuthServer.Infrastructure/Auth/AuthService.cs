using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using AuthServer.Application.Auth;
using AuthServer.Domain.Entities;
using AuthServer.Infrastructure.Persistence;
using AuthServer.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthServer.Infrastructure.Auth;

public sealed class AuthService : IAuthService
{
    private readonly AuthDbContext _dbContext;
    private readonly IUserPasswordHasher _passwordHasher;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        AuthDbContext dbContext,
        IUserPasswordHasher passwordHasher,
        IOptions<JwtOptions> jwtOptions)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<LoginResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var identifier = request.UserNameOrEmail.Trim();
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var normalized = NormalizeIdentity(identifier);

        var user = await _dbContext.Users
            .Include(current => current.UserRoles)
            .ThenInclude(current => current.Role)
            .FirstOrDefaultAsync(
                current => current.NormalizedUserName == normalized || current.NormalizedEmail == normalized,
                cancellationToken);

        if (user is null || !CanAuthenticate(user, now))
        {
            return null;
        }

        var passwordOk = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (!passwordOk)
        {
            return null;
        }

        return await IssueTokensAsync(user, now, cancellationToken);
    }

    public async Task<LoginResult?> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var tokenHash = ComputeTokenHash(request.RefreshToken);

        var existingToken = await _dbContext.RefreshTokens
            .Include(current => current.User)
            .ThenInclude(current => current.UserRoles)
            .ThenInclude(current => current.Role)
            .FirstOrDefaultAsync(current => current.TokenHash == tokenHash, cancellationToken);

        if (existingToken is null)
        {
            return null;
        }

        if (existingToken.RevokedAtUtc is not null)
        {
            // Reuse detection: if a revoked refresh token is presented, revoke its whole family.
            await RevokeTokenFamilyAsync(existingToken, cancellationToken);
            return null;
        }

        if (existingToken.ExpiresAtUtc <= now)
        {
            return null;
        }

        var user = existingToken.User;
        if (!CanAuthenticate(user, now))
        {
            return null;
        }

        return await RotateAndIssueTokensAsync(user, existingToken, cancellationToken);
    }

    public async Task RevokeAsync(RevokeTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return;
        }

        var tokenHash = ComputeTokenHash(request.RefreshToken);
        var currentToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(current => current.TokenHash == tokenHash, cancellationToken);

        if (currentToken is null)
        {
            return;
        }

        await RevokeTokenFamilyAsync(currentToken, cancellationToken);
    }

    private async Task RevokeTokenFamilyAsync(RefreshToken rootToken, CancellationToken cancellationToken)
    {
        var revokedAny = false;
        var revokedAtUtc = DateTimeOffset.UtcNow;
        var currentToken = rootToken;

        while (currentToken is not null)
        {
            if (currentToken.RevokedAtUtc is null)
            {
                currentToken.RevokedAtUtc = revokedAtUtc;
                revokedAny = true;
            }

            if (!currentToken.ReplacedByTokenId.HasValue)
            {
                break;
            }

            currentToken = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(
                    current => current.Id == currentToken.ReplacedByTokenId.Value && current.UserId == rootToken.UserId,
                    cancellationToken);
        }

        if (revokedAny)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<LoginResult> IssueTokensAsync(User user, DateTimeOffset now, CancellationToken cancellationToken)
    {
        ClearExpiredLockout(user, now);

        var refreshTokenRaw = GenerateRefreshToken();
        var refreshExpiresAtUtc = now.AddDays(_jwtOptions.RefreshTokenDays);
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = ComputeTokenHash(refreshTokenRaw),
            ExpiresAtUtc = refreshExpiresAtUtc,
            CreatedAtUtc = now
        };

        _dbContext.RefreshTokens.Add(refreshToken);

        user.LastLoginUtc = now;
        user.UpdatedAtUtc = now;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var expiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes);
        return CreateLoginResult(user, refreshTokenRaw, refreshExpiresAtUtc, expiresAtUtc);
    }

    private async Task<LoginResult> RotateAndIssueTokensAsync(User user, RefreshToken currentToken, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        ClearExpiredLockout(user, now);

        var newRefreshTokenRaw = GenerateRefreshToken();
        var newRefreshExpiresAtUtc = now.AddDays(_jwtOptions.RefreshTokenDays);

        var replacementToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = ComputeTokenHash(newRefreshTokenRaw),
            ExpiresAtUtc = newRefreshExpiresAtUtc,
            CreatedAtUtc = now
        };

        currentToken.RevokedAtUtc = now;
        currentToken.ReplacedByTokenId = replacementToken.Id;

        _dbContext.RefreshTokens.Add(replacementToken);

        user.LastLoginUtc = now;
        user.UpdatedAtUtc = now;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var accessExpiresAtUtc = now.AddMinutes(_jwtOptions.AccessTokenMinutes);
        return CreateLoginResult(user, newRefreshTokenRaw, newRefreshExpiresAtUtc, accessExpiresAtUtc);
    }

    private LoginResult CreateLoginResult(User user, string refreshToken, DateTimeOffset refreshExpiresAtUtc, DateTimeOffset accessExpiresAtUtc)
    {
        var roles = user.UserRoles.Select(current => current.Role.Name).Distinct().ToArray();
        var accessToken = GenerateAccessToken(user.Id, user.UserName, user.Email, roles, accessExpiresAtUtc);

        return new LoginResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAtUtc = accessExpiresAtUtc,
            RefreshTokenExpiresAtUtc = refreshExpiresAtUtc,
            UserId = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Roles = roles
        };
    }

    private static bool CanAuthenticate(User? user, DateTimeOffset now)
    {
        if (user is null || !user.IsActive)
        {
            return false;
        }

        if (!user.IsLocked)
        {
            return true;
        }

        return user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value <= now;
    }

    private static string NormalizeIdentity(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private static void ClearExpiredLockout(User user, DateTimeOffset now)
    {
        if (user.IsLocked && user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value <= now)
        {
            user.IsLocked = false;
            user.LockoutEndUtc = null;
        }
    }

    private string GenerateAccessToken(Guid userId, string userName, string email, IReadOnlyCollection<string> roles, DateTimeOffset expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(_jwtOptions.SigningKey))
        {
            throw new InvalidOperationException("JWT signing key is missing.");
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, userName),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(roleName => new Claim(ClaimTypes.Role, roleName)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static string ComputeTokenHash(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
