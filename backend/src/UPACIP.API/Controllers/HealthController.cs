using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace UPACIP.API.Controllers;

/// <summary>
/// Provides a lightweight liveness probe used by CI smoke tests and
/// IIS application pool health monitoring.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class HealthController : ControllerBase
{
    /// <summary>
    /// Returns the service liveness status.
    /// </summary>
    /// <returns>HTTP 200 with a JSON body containing status and UTC timestamp.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new HealthResponse("healthy", DateTime.UtcNow));
    }
}

/// <summary>Response body for GET /api/v1/health.</summary>
public sealed record HealthResponse(string Status, DateTime Timestamp);
