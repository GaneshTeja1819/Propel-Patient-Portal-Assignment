using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using UPACIP.Application.Commands.Appointments;
using AppValidationException = UPACIP.Application.Exceptions.ValidationException;
using UPACIP.Application.Handlers.Appointments;
using UPACIP.Infrastructure.BackgroundJobs;
using UPACIP.Infrastructure.Persistence;

namespace UPACIP.API.Controllers;

/// <summary>
/// Appointment booking endpoint (Patient-scoped).
///
/// Acceptance Criteria:
/// - AC-001: POST creates appointment with status "Booked"; returns HTTP 201 with appointmentId.
/// - AC-002: No-show risk score computed and stored; default 0 on exception; no UI error surfaced.
/// - AC-003: Insurance validated against InsuranceRecord; status returned in response.
/// - AC-004: HTTP 409 when slot already booked; no data changes on conflict.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/appointments")]
[Authorize(Policy = "PatientPolicy")]
public sealed class AppointmentsController : ControllerBase
{
    private readonly BookAppointmentHandler _bookAppointmentHandler;
    private readonly CancelAppointmentHandler _cancelAppointmentHandler;
    private readonly RescheduleAppointmentHandler _rescheduleAppointmentHandler;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(
        BookAppointmentHandler bookAppointmentHandler,
        CancelAppointmentHandler cancelAppointmentHandler,
        RescheduleAppointmentHandler rescheduleAppointmentHandler,
        AppDbContext dbContext,
        ILogger<AppointmentsController> logger)
    {
        _bookAppointmentHandler = bookAppointmentHandler;
        _cancelAppointmentHandler = cancelAppointmentHandler;
        _rescheduleAppointmentHandler = rescheduleAppointmentHandler;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Books an appointment slot for the authenticated patient.
    /// Returns HTTP 201 on success, HTTP 409 when the slot is already booked.
    /// </summary>
    /// <param name="request">Booking request with slotId and optional insurance fields.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> BookAppointment(
        [FromBody] BookAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SlotId == Guid.Empty)
            return BadRequest(new { message = "slotId is required and must be a valid GUID." });

        var patientId = ResolvePatientId();
        if (patientId == Guid.Empty)
            return Unauthorized();

        var command = new BookAppointmentCommand(
            PatientId:         patientId,
            SlotId:            request.SlotId,
            PreferredSlotId:   request.PreferredSlotId,
            InsuranceProvider: request.InsuranceProvider,
            InsuranceId:       request.InsuranceId);

        try
        {
            var result = await _bookAppointmentHandler.HandleAsync(command, cancellationToken);

            if (result.IsConflict)
            {
                return Conflict(new { message = "The requested slot is no longer available." });
            }

            return StatusCode(
                StatusCodes.Status201Created,
                new
                {
                    appointmentId             = result.AppointmentId,
                    insuranceValidationStatus = result.InsuranceValidationStatus,
                    slotId                    = result.SlotId,
                    preferredSlotRegistered   = result.PreferredSlotRegistered,
                });
        }
        catch (AppValidationException ex)
        {
            return UnprocessableEntity(new
            {
                message = "Validation failed.",
                errors = ex.Errors,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to book appointment for slot {SlotId}.", request.SlotId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "Failed to book appointment." });
        }
    }

    [HttpPatch("{id:guid}/cancel")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelAppointment(Guid id, CancellationToken cancellationToken)
    {
        var patientId = ResolvePatientId();
        if (patientId == Guid.Empty)
            return Unauthorized();

        try
        {
            var result = await _cancelAppointmentHandler.HandleAsync(
                new CancelAppointmentCommand(patientId, id),
                cancellationToken);

            if (result.Status == CancelAppointmentStatus.NotFound)
                return NotFound(new { message = "Appointment not found." });

            if (result.Status == CancelAppointmentStatus.PastAppointment)
                return UnprocessableEntity(new { message = "Past appointments cannot be cancelled." });

            return Ok(new
            {
                appointmentId = id,
                status = "Cancelled",
                slotId = result.SlotId,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel appointment {AppointmentId}.", id);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "Failed to cancel appointment." });
        }
    }

    [HttpPatch("{id:guid}/reschedule")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RescheduleAppointment(
        Guid id,
        [FromBody] RescheduleAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.NewSlotId == Guid.Empty)
            return BadRequest(new { message = "newSlotId is required and must be a valid GUID." });

        var patientId = ResolvePatientId();
        if (patientId == Guid.Empty)
            return Unauthorized();

        try
        {
            var result = await _rescheduleAppointmentHandler.HandleAsync(
                new RescheduleAppointmentCommand(patientId, id, request.NewSlotId),
                cancellationToken);

            if (result.Status == RescheduleAppointmentStatus.NotFound)
                return NotFound(new { message = "Appointment not found." });

            if (result.Status == RescheduleAppointmentStatus.Conflict)
                return Conflict(new { message = "Slot no longer available." });

            return Ok(new
            {
                appointmentId = id,
                oldSlotId = result.OldSlotId,
                newSlotId = result.NewSlotId,
                status = "Rescheduled",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reschedule appointment {AppointmentId}.", id);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "Failed to reschedule appointment." });
        }
    }

    [HttpGet("{id:guid}/confirmation-status")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetConfirmationStatus(Guid id, CancellationToken cancellationToken)
    {
        var patientId = ResolvePatientId();
        if (patientId == Guid.Empty)
            return Unauthorized();

        var idempotencyKey = GeneratePdfConfirmationJob.BuildIdempotencyKey(id);

        var status = await _dbContext.Notifications
            .AsNoTracking()
            .Where(n =>
                n.RecipientId == patientId &&
                n.Type == "AppointmentConfirmation" &&
                n.Title == idempotencyKey)
            .Select(n => n.Body)
            .FirstOrDefaultAsync(cancellationToken);

        var normalizedStatus = status switch
        {
            "Sent" => "Sent",
            "Failed" => "Failed",
            "Processing" => "Processing",
            _ => "Queued",
        };

        return Ok(new { status = normalizedStatus });
    }

    private Guid ResolvePatientId()
    {
        var claim = User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}

/// <summary>Request body for POST /api/v1/appointments.</summary>
public sealed record BookAppointmentRequest(
    Guid SlotId,
    Guid? PreferredSlotId,
    string? InsuranceProvider,
    string? InsuranceId);

public sealed record RescheduleAppointmentRequest(Guid NewSlotId);
