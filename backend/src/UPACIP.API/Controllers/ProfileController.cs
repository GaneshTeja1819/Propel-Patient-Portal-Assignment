using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UPACIP.Infrastructure.Handlers.Profile;
using UPACIP.Application.Queries.Profile;

namespace UPACIP.API.Controllers;

/// <summary>
/// Patient clinical profile endpoints (US_027, AC-001, AC-005, OWASP A01).
///
/// <list type="bullet">
///   <item>
///     <c>GET /api/v1/profile/me</c> — returns the calling patient's own profile.
///     Requires <c>PatientPolicy</c>.
///   </item>
///   <item>
///     <c>GET /api/v1/profile/{patientId}</c> — returns a specific patient's profile.
///     Requires <c>StaffPolicy</c> (OWASP A01 — staff-only access).
///   </item>
/// </list>
///
/// Both endpoints support LIMIT/OFFSET pagination via <c>page</c> and <c>pageSize</c>
/// query parameters (AC-005). PHI is decrypted by EF Core before the response is serialised.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/profile")]
public sealed class ProfileController : ControllerBase
{
    private readonly GetPatientProfileHandler _handler;

    public ProfileController(GetPatientProfileHandler handler) => _handler = handler;

    // ── GET /api/v1/profile/me ───────────────────────────────────────────

    /// <summary>
    /// Returns the calling patient's clinical profile.
    /// The patient ID is resolved from the <c>sub</c> JWT claim — no user-supplied
    /// ID parameter is accepted for this endpoint (OWASP A01 — Broken Access Control).
    /// </summary>
    /// <param name="page">1-based page number (default 1).</param>
    /// <param name="pageSize">Items per page (default 20, max 100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>HTTP 200 with profile DTO; HTTP 401 when unauthenticated; HTTP 403 when wrong role.</returns>
    [HttpGet("me")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = "PatientPolicy")]
    [ProducesResponseType(typeof(PatientProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyProfile(
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct     = default)
    {
        var patientId = ResolvePatientId();
        if (patientId == Guid.Empty)
            return Unauthorized();

        var query  = new GetPatientProfileQuery(patientId, page, pageSize);
        var result = await _handler.HandleAsync(query, ct);

        return Ok(result);
    }

    // ── GET /api/v1/profile/{patientId} ──────────────────────────────────

    /// <summary>
    /// Returns a specific patient's clinical profile. Staff-only endpoint (OWASP A01).
    /// </summary>
    /// <param name="patientId">Patient identifier.</param>
    /// <param name="page">1-based page number (default 1).</param>
    /// <param name="pageSize">Items per page (default 20, max 100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>HTTP 200 with profile DTO; HTTP 401 when unauthenticated; HTTP 403 when wrong role; HTTP 404 when patient not found.</returns>
    [HttpGet("{patientId:guid}")]
    [MapToApiVersion("1.0")]
    [Authorize(Policy = "StaffPolicy")]
    [ProducesResponseType(typeof(PatientProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPatientProfile(
        [FromRoute] Guid patientId,
        [FromQuery] int  page     = 1,
        [FromQuery] int  pageSize = 20,
        CancellationToken ct      = default)
    {
        if (patientId == Guid.Empty)
            return BadRequest();

        var query  = new GetPatientProfileQuery(patientId, page, pageSize);
        var result = await _handler.HandleAsync(query, ct);

        // Return 404 when the patient record does not exist
        if (string.IsNullOrEmpty(result.DisplayName) && !result.HasDocuments)
        {
            var exists = result.PatientId != Guid.Empty;
            if (!exists) return NotFound();
        }

        return Ok(result);
    }

    // ── Private helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Extracts and validates the patient <c>sub</c> claim from the JWT (OWASP A01).
    /// Returns <see cref="Guid.Empty"/> when the claim is absent or malformed.
    /// </summary>
    private Guid ResolvePatientId()
    {
        var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
