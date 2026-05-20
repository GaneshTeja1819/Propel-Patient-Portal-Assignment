using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using UPACIP.Application.Commands.Codes;
using UPACIP.Application.Handlers.Codes;
using UPACIP.Application.Interfaces;
using UPACIP.Infrastructure.Handlers.Codes;

namespace UPACIP.API.Controllers;

/// <summary>
/// Code verification endpoints (US_030, AC-001–AC-005).
/// Staff-only for POST /verify; authenticated for GET /validate.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/codes")]
public sealed class CodeVerificationController : ControllerBase
{
    private readonly VerifyCodeHandler _handler;
    private readonly IIcdCptReferenceService _referenceService;

    public CodeVerificationController(
        VerifyCodeHandler handler,
        IIcdCptReferenceService referenceService)
    {
        _handler          = handler;
        _referenceService = referenceService;
    }

    // ── POST /api/v1/codes/verify ─────────────────────────────────────────────

    /// <summary>
    /// Verifies a medical code suggestion as Accepted, Modified, or Rejected (AC-001–AC-004).
    /// Staff only (AC-006 OWASP A01). Returns 201 on success.
    /// </summary>
    [HttpPost("verify")]
    [Authorize(Policy = "StaffPolicy")]
    [ProducesResponseType(typeof(VerifyCodeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> VerifyCode(
        [FromBody] VerifyCodeRequest request,
        CancellationToken ct)
    {
        if (!HasValidSubClaim(out var actorId))
            return Unauthorized("Invalid or missing sub claim.");

        var command = new VerifyCodeCommand(
            SuggestionId: request.SuggestionId,
            Decision:     request.Decision,
            VerifiedCode: request.VerifiedCode,
            ActorStaffId: actorId);

        try
        {
            var result = await _handler.HandleAsync(command, ct);

            return CreatedAtAction(
                nameof(VerifyCode),
                new { id = result.VerifiedMedicalCodeId },
                new VerifyCodeResponse(
                    result.VerifiedMedicalCodeId,
                    result.CodingStatusUpdated));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (UnprocessableEntityException ex)
        {
            return UnprocessableEntity(new { error = ex.Message });
        }
    }

    // ── GET /api/v1/codes/validate ────────────────────────────────────────────

    /// <summary>
    /// Utility endpoint: validates whether a code is structurally correct for
    /// a given code system without persisting anything (AC-003 UI support).
    /// </summary>
    [HttpGet("validate")]
    [Authorize]
    [ProducesResponseType(typeof(CodeValidateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult ValidateCode(
        [FromQuery][Required] string code,
        [FromQuery][Required] string codeType)
    {
        var valid = _referenceService.IsValidCode(code, codeType);

        return Ok(new CodeValidateResponse(code, codeType, valid));
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private bool HasValidSubClaim(out Guid userId)
    {
        userId = Guid.Empty;
        var sub = User.FindFirstValue("sub");
        return !string.IsNullOrWhiteSpace(sub) && Guid.TryParse(sub, out userId);
    }
}

// ── Request / Response DTOs ───────────────────────────────────────────────────

/// <summary>Request body for <c>POST /codes/verify</c>.</summary>
/// <param name="SuggestionId">ID of the suggestion to verify.</param>
/// <param name="Decision">Must be <c>"Accepted"</c>, <c>"Modified"</c>, or <c>"Rejected"</c>.</param>
/// <param name="VerifiedCode">Required when <c>Decision == "Modified"</c>; otherwise null.</param>
public sealed record VerifyCodeRequest(
    [Required] Guid SuggestionId,
    [Required][RegularExpression("^(Accepted|Modified|Rejected)$",
        ErrorMessage = "Decision must be 'Accepted', 'Modified', or 'Rejected'.")] string Decision,
    string? VerifiedCode);

/// <summary>Response body for <c>POST /codes/verify</c>.</summary>
public sealed record VerifyCodeResponse(
    Guid VerifiedMedicalCodeId,
    bool CodingStatusUpdated);

/// <summary>Response body for <c>GET /codes/validate</c>.</summary>
public sealed record CodeValidateResponse(
    string Code,
    string CodeType,
    bool Valid);
