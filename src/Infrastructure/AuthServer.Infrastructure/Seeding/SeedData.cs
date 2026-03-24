using AuthServer.Domain.Entities;
using AuthServer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;

namespace AuthServer.Infrastructure.Seeding;

public static class SeedData
{
    public static async Task MigrateAndSeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetService<ILoggerFactory>()?.CreateLogger("SeedData");
        try
        {
            var db = services.GetRequiredService<AuthDbContext>();
            await db.Database.MigrateAsync();

            if (!await db.Users.AnyAsync())
            {
                var hasher = services.GetRequiredService<Security.IUserPasswordHasher>();

                var users = new List<User>
                {
                    new User
                    {
                        Id = Guid.NewGuid(),
                        UserName = "admin",
                        Email = "admin@local",
                        PasswordHash = hasher.HashPassword(null!, "P@ssw0rd!"),
                        IsActive = true,
                        CreatedAtUtc = DateTimeOffset.UtcNow
                    },
                    new User
                    {
                        Id = Guid.NewGuid(),
                        UserName = "test",
                        Email = "test@local",
                        PasswordHash = hasher.HashPassword(null!, "Test1234"),
                        IsActive = true,
                        CreatedAtUtc = DateTimeOffset.UtcNow
                    }
                };

                db.Users.AddRange(users);
                await db.SaveChangesAsync();
                logger?.LogInformation("Seeded initial users.");
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An error occurred while migrating or seeding the database.");
            throw;
        }
    }

    // Password hashing is provided by IUserPasswordHasher registered in DI.
}
