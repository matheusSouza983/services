using Microsoft.AspNetCore.Mvc;
using AuthServer.Application.Health;

namespace AuthServer.API.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    private readonly IHealthService _health;

    public HealthController(IHealthService health)
    {
        _health = health;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var result = await _health.GetHealthAsync();
        return Ok(result);
    }
}