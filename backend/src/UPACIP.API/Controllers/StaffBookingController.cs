using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UPACIP.Application.Commands.Appointments;
using UPACIP.Application.Commands.Patients;
using UPACIP.Application.Exceptions;
using UPACIP.Application.Handlers.Appointments;
using UPACIP.Application.Handlers.Patients;
using UPACIP.Application.Interfaces;
using AppValidationException = UPACIP.Application.Exceptions.ValidationException;

namespace UPACIP.API.Controllers;

/// <summary>
/// Staff-only walk-in booking endpoints.
/// All routes require <c>StaffPolicy</c> (role = "Staff") — patients cannot call
/// these endpoints (OWASP A01 Broken Access Control).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = "StaffPolicy")]
[Route("api/v{version:apiVersion}")]
public sealed class StaffBookingController : ControllerBase
{
    private readonly IWalkInBookingStore _walkInStore;
    private readonly WalkInBookingHandler _walkInBookingHandler;
    private readonly CreatePatientFromWalkInHandler _createPatientHandler;

    public StaffBookingController(
        IWalkInBookingStore walkInStore,
        WalkInBookingHandler walkInBookingHandler,
        CreatePatientFromWalkInHandler createPatientHandler)
    {
        _walkInStore = walkInStore;
        _walkInBookingHandler = walkInBookingHandler;
        _createPatientHandler = createPatientHandler;
    }

    /// <summary>
    /// Type-ahead patient search by name (case-insensitive).
    /// Returns up to 20 matching Patient records.
    /// </summary>
    /// <param name="q">Search term (partial first/last name).</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpGet("patients/search")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchPatients(
        [FromQuery] string q,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "Search query 'q' is required." });

        var results = await _walkInStore.SearchPatientsAsync(q, cancellationToken);

        return Ok(results.Select(r => new
        {
            id = r.Id,
            fullName = r.FullName,
            email = r.Email
        }));
    }

    /// <summary>
    /// Creates a walk-in appointment for an existing or anonymous patient.
    /// Sets <c>createdByStaffId</c> from the Staff JWT sub claim.
    /// Returns HTTP 201 with the new appointment ID.
    /// </summary>
    [HttpPost("appointments/walk-in")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateWalkInAppointment(
        [FromBody] WalkInBookingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var staffId = ResolveStaffId();
            var appointmentId = await _walkInBookingHandler.HandleAsync(new WalkInBookingCommand(
                request.PatientId,
                request.AnonName,
                request.AnonDob,
                request.AnonPhone,
                request.SlotId,
                staffId),
                cancellationToken);

            return CreatedAtAction(nameof(CreateWalkInAppointment), new { appointmentId },
                new { appointmentId });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (AppValidationException ex)
        {
            return UnprocessableEntity(new
            {
                message = "Validation failed.",
                errors = ex.Errors
            });
        }
    }

    /// <summary>
    /// Creates a Patient User account and links it to an existing anonymous appointment.
    /// Returns HTTP 201 with the new patient ID and the appointment ID.
    /// Returns HTTP 409 if the email is already registered.
    /// </summary>
    [HttpPost("patients/create-from-walkin")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreatePatientFromWalkIn(
        [FromBody] CreatePatientFromWalkInRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var staffId = ResolveStaffId();
            var (patientId, appointmentId) = await _createPatientHandler.HandleAsync(
                new CreatePatientFromWalkInCommand(
                    request.AppointmentId,
                    request.Email,
                    request.Password,
                    request.FirstName,
                    request.LastName,
                    staffId),
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, new { patientId, appointmentId });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (AppValidationException ex)
        {
            return UnprocessableEntity(new
            {
                message = "Validation failed.",
                errors = ex.Errors
            });
        }
    }

    private Guid ResolveStaffId()
    {
        var claim = User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}

// ── Request DTOs (controller-scoped; not domain objects) ─────────────────────

/// <summary>Request body for <c>POST /appointments/walk-in</c>.</summary>
public sealed record WalkInBookingRequest(
    Guid? PatientId,
    string? AnonName,
    DateOnly? AnonDob,
    string? AnonPhone,
    Guid SlotId);

/// <summary>Request body for <c>POST /patients/create-from-walkin</c>.</summary>
public sealed record CreatePatientFromWalkInRequest(
    Guid AppointmentId,
    string Email,
    string Password,
    string FirstName,
    string LastName);
