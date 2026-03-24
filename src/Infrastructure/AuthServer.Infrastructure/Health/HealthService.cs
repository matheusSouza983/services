using AuthServer.Application.Health;
using AuthServer.Infrastructure.Persistence;

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
            result.Status = result.DatabaseReachable ? "Healthy" : "Degraded";
        }
        catch
        {
            result.Status = "Degraded";
        }

        return result;
    }
}
