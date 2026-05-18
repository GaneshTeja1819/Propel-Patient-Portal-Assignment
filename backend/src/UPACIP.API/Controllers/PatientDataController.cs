using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UPACIP.Application.Interfaces;

namespace UPACIP.API.Controllers;

/// <summary>
/// HIPAA patient data management endpoints.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/patients")]
public sealed class PatientDataController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public PatientDataController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Initiates a HIPAA-compliant deletion of all PHI associated with the
    /// specified patient (AC-006). Returns HTTP 202 Accepted immediately;
    /// the actual erasure is processed asynchronously.
    /// </summary>
    /// <param name="id">Patient user ID.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>HTTP 202 with no body.</returns>
    [HttpDelete("{id:guid}/data")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeletePatientData(
        Guid id,
        CancellationToken cancellationToken)
    {
        var actorId = ResolveActorId();
        var actorRole = User.FindFirstValue("role") ?? "anonymous";

        await _auditLogService.LogAsync(
            actorId,
            actorRole,
            actionType: "DELETE_PATIENT_DATA",
            targetEntity: "Patient",
            targetId: id,
            cancellationToken: cancellationToken);

        // TODO: enqueue background erasure job (task_003_patient-data-erasure)
        return Accepted();
    }

    private Guid ResolveActorId()
    {
        var claim = User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
