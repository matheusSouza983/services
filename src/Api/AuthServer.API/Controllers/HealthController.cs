using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AuthServer.API.Authorization;
using AuthServer.Application.ApiResponses;
using AuthServer.Application.Health;

namespace AuthServer.API.Controllers;

[ApiController]
[Route("health")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class HealthController : ControllerBase
{
    private readonly IHealthService _health;

    public HealthController(IHealthService health)
    {
        _health = health;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<HealthResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get()
    {
        var result = await _health.GetHealthAsync();
        return Ok(result);
    }
}