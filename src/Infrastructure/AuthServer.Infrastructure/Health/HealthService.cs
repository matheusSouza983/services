using AuthServer.Application.Health;
using AuthServer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Infrastructure.Health;

public class HealthService : IHealthService
{
    private readonly AuthDbContext _db;

    public HealthService(AuthDbContext db)
    {
        _db = db;
    }

    public async Task<HealthResult> GetHealthAsync()
    {
        var result = new HealthResult
        {
            Service = "AuthServer.API",
            Timestamp = DateTime.UtcNow
        };

        try
        {
            result.DatabaseReachable = await _db.Database.CanConnectAsync();
            if (result.DatabaseReachable)
            {
                result.Users = await _db.Users.LongCountAsync();
                result.AppliedMigrations = _db.Database.GetAppliedMigrations();
                result.Status = "Healthy";
            }
            else
            {
                result.Status = "Degraded";
            }
        }
        catch (Exception ex)
        {
            result.Status = "Degraded";
            result.DbError = ex.Message;
        }

        return result;
    }
}
