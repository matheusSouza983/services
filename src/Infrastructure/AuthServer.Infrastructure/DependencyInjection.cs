using AuthServer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthServer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AuthServerConnection")
            ?? throw new InvalidOperationException("Connection string 'AuthServerConnection' was not found.");

        services.AddDbContext<AuthDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddSingleton<Security.IUserPasswordHasher, Security.UserPasswordHasher>();

        services.AddScoped<Application.Health.IHealthService, Health.HealthService>();

        return services;
    }
}