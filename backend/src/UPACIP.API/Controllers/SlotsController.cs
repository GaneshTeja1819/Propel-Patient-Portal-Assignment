using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UPACIP.Application.Handlers.Slots;
using UPACIP.Application.Queries.Slots;

namespace UPACIP.API.Controllers;

/// <summary>
/// Appointment slot availability endpoint with Redis caching (5-second TTL).
///
/// Acceptance Criteria (AC-001, AC-002):
/// - AC-001: Slot data served from Redis cache within 100ms on cache hit;
///           falls through to PostgreSQL if Redis unavailable
/// - AC-002: Slot state changes reflected in cache within one 5-second TTL cycle
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/slots")]
[Authorize(Policy = "PatientPolicy")]
public sealed class SlotsController : ControllerBase
{
    private readonly GetSlotsHandler _getSlotsHandler;

    public SlotsController(GetSlotsHandler getSlotsHandler)
    {
        _getSlotsHandler = getSlotsHandler;
    }

    /// <summary>
    /// Retrieves available appointment slots for the specified date.
    /// 
    /// Slot data is cached in Redis with a 5-second TTL. On cache miss or
    /// Redis unavailability, data is fetched from PostgreSQL and cached.
    /// 
    /// The response includes a <c>cached</c> flag indicating whether the data
    /// came from Redis (true) or PostgreSQL fallback (false).
    /// </summary>
    /// <param name="date">
    /// Date for which to fetch slots, in ISO 8601 format (yyyy-MM-dd).
    /// Future dates are supported; past dates return an empty list.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// HTTP 200 with a <see cref="GetSlotsResult"/> containing available slots
    /// and a cached flag.
    /// </returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSlots(
        [FromQuery] DateTime date,
        CancellationToken cancellationToken)
    {
        if (date == default)
            return BadRequest(new { message = "date query parameter is required in yyyy-MM-dd format" });

        try
        {
            var query = new GetSlotsQuery(date);
            var result = await _getSlotsHandler.HandleAsync(query, cancellationToken);

            return Ok(new
            {
                data = result.Slots,
                cached = result.Cached,
                timestamp = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "Failed to retrieve slots", error = ex.Message });
        }
    }
}
