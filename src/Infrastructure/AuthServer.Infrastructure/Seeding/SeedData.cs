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
            var now = DateTimeOffset.UtcNow;
            var hasher = services.GetRequiredService<Security.IUserPasswordHasher>();

            var adminRole = await db.Roles.FirstOrDefaultAsync(current => current.Name == "admin");
            if (adminRole is null)
            {
                adminRole = new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "admin",
                    Description = "Full access role",
                    IsSystem = true,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                db.Roles.Add(adminRole);
            }

            var userRole = await db.Roles.FirstOrDefaultAsync(current => current.Name == "user");
            if (userRole is null)
            {
                userRole = new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "user",
                    Description = "Default user role",
                    IsSystem = true,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                db.Roles.Add(userRole);
            }

            var adminUser = await db.Users.FirstOrDefaultAsync(current => current.UserName == "admin");
            if (adminUser is null)
            {
                adminUser = new User
                {
                    Id = Guid.NewGuid(),
                    UserName = "admin",
                    Email = "admin@local",
                    EmailVerified = true,
                    IsActive = true,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    PasswordLastChangedUtc = now,
                    MfaEnabled = false
                };
                adminUser.PasswordHash = hasher.HashPassword(adminUser, "P@ssw0rd!");
                db.Users.Add(adminUser);
            }

            var testUser = await db.Users.FirstOrDefaultAsync(current => current.UserName == "test");
            if (testUser is null)
            {
                testUser = new User
                {
                    Id = Guid.NewGuid(),
                    UserName = "test",
                    Email = "test@local",
                    EmailVerified = true,
                    IsActive = true,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    PasswordLastChangedUtc = now,
                    MfaEnabled = false
                };
                testUser.PasswordHash = hasher.HashPassword(testUser, "Test1234");
                db.Users.Add(testUser);
            }

            if (!await db.UserRoles.AnyAsync(current => current.UserId == adminUser.Id && current.RoleId == adminRole.Id))
            {
                db.UserRoles.Add(new UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = adminRole.Id,
                    AssignedAtUtc = now
                });
            }

            if (!await db.UserRoles.AnyAsync(current => current.UserId == testUser.Id && current.RoleId == userRole.Id))
            {
                db.UserRoles.Add(new UserRole
                {
                    UserId = testUser.Id,
                    RoleId = userRole.Id,
                    AssignedAtUtc = now
                });
            }

            // Persist explicit role assignments first to avoid duplicate tracked keys in the safety-net pass.
            await db.SaveChangesAsync();

            // Safety net: ensure there is no user without at least one role.
            var userIdsWithoutRole = await db.Users
                .Where(current => !db.UserRoles.Any(ur => ur.UserId == current.Id))
                .Select(current => current.Id)
                .ToListAsync();

            foreach (var userId in userIdsWithoutRole)
            {
                db.UserRoles.Add(new UserRole
                {
                    UserId = userId,
                    RoleId = userRole.Id,
                    AssignedAtUtc = now
                });
            }

            await db.SaveChangesAsync();
            logger?.LogInformation("Users, roles and user-role assignments ensured successfully.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An error occurred while migrating or seeding the database.");
            throw;
        }
    }

    // Password hashing is provided by IUserPasswordHasher registered in DI.
}
