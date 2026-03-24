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

        var normalized = identifier.ToLowerInvariant();

        var user = await _dbContext.Users
            .Include(current => current.UserRoles)
            .ThenInclude(current => current.Role)
            .FirstOrDefaultAsync(
                current => current.UserName.ToLower() == normalized || current.Email.ToLower() == normalized,
                cancellationToken);

        if (user is null || !user.IsActive || user.IsLocked)
        {
            return null;
        }

        var passwordOk = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (!passwordOk)
        {
            return null;
        }

        return await IssueTokensAsync(user, cancellationToken);
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

        if (existingToken.RevokedAtUtc is not null || existingToken.ExpiresAtUtc <= now)
        {
            return null;
        }

        var user = existingToken.User;
        if (!user.IsActive || user.IsLocked)
        {
            return null;
        }

        return await RotateAndIssueTokensAsync(user, existingToken, cancellationToken);
    }

    private async Task<LoginResult> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var refreshTokenRaw = GenerateRefreshToken();
        var now = DateTimeOffset.UtcNow;
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

        var roles = user.UserRoles.Select(current => current.Role.Name).Distinct().ToArray();
        var expiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes);
        var accessToken = GenerateAccessToken(user.Id, user.UserName, user.Email, roles, expiresAtUtc);

        return new LoginResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenRaw,
            ExpiresAtUtc = expiresAtUtc,
            RefreshTokenExpiresAtUtc = refreshExpiresAtUtc,
            UserId = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Roles = roles
        };
    }

    private async Task<LoginResult> RotateAndIssueTokensAsync(User user, RefreshToken currentToken, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
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

        var roles = user.UserRoles.Select(current => current.Role.Name).Distinct().ToArray();
        var accessExpiresAtUtc = now.AddMinutes(_jwtOptions.AccessTokenMinutes);
        var accessToken = GenerateAccessToken(user.Id, user.UserName, user.Email, roles, accessExpiresAtUtc);

        return new LoginResult
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshTokenRaw,
            ExpiresAtUtc = accessExpiresAtUtc,
            RefreshTokenExpiresAtUtc = newRefreshExpiresAtUtc,
            UserId = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Roles = roles
        };
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
