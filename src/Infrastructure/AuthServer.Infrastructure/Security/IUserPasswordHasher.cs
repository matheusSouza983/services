using AuthServer.Domain.Entities;

namespace AuthServer.Infrastructure.Security;

public interface IUserPasswordHasher
{
    string HashPassword(User user, string password);
    bool VerifyHashedPassword(User user, string hashedPassword, string providedPassword);
}
