namespace AuthServer.Application.Health;

public interface IHealthService
{
    Task<HealthResult> GetHealthAsync();
}

public sealed class HealthResult
{
    public string Service { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool DatabaseReachable { get; set; }
    public string Status { get; set; } = "Unknown";
}
