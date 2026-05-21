using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UPACIP.Application.Commands.Intake;
using UPACIP.Application.Handlers.Intake;
using UPACIP.Application.Interfaces;

namespace UPACIP.API.Controllers;

/// <summary>
/// AI conversational intake session endpoints (US_018, AC-001–AC-002).
///
/// All endpoints require the <c>PatientPolicy</c> role (OWASP A01).
/// Session state is managed in Redis by <see cref="IIntakeSessionService"/>;
/// no data is persisted to the database until <c>POST /confirm</c> (task_003).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = "PatientPolicy")]
[Route("api/v{version:apiVersion}/intake")]
public sealed class IntakeController : ControllerBase
{
    private readonly IIntakeSessionService  _intakeService;
    private readonly ConfirmIntakeHandler    _confirmHandler;

    public IntakeController(
        IIntakeSessionService intakeService,
        ConfirmIntakeHandler confirmHandler)
    {
        _intakeService  = intakeService;
        _confirmHandler = confirmHandler;
    }

    // ── POST /api/v1/intake/start ────────────────────────────────────────

    /// <summary>
    /// Initialises a new AI intake session for the specified appointment.
    /// Returns the first question text and field metadata.
    /// </summary>
    /// <param name="request">Appointment identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>HTTP 200 with the first question; HTTP 400 on invalid input.</returns>
    [HttpPost("start")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(StartIntakeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartSession(
        [FromBody] StartIntakeRequest request,
        CancellationToken ct)
    {
        if (!ValidateAppointmentOwnership(request.AppointmentId))
            return Forbid();

        var result = await _intakeService.StartSessionAsync(request.AppointmentId, ct);

        return Ok(new StartIntakeResponse(
            FieldKey:       result.FieldKey,
            QuestionText:   result.QuestionText,
            QuestionNumber: result.QuestionNumber,
            TotalQuestions: result.TotalQuestions,
            IsPhiField:     result.IsPhiField,
            QuickOptions:   result.QuickOptions));
    }

    // ── POST /api/v1/intake/answer ───────────────────────────────────────

    /// <summary>
    /// Submits the patient's free-text answer for the current intake field.
    /// Returns either the next question (phase = "conversation") or a summary
    /// signal (phase = "summary") when all fields are captured.
    /// </summary>
    /// <param name="request">Appointment ID, field key, and raw patient answer.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>HTTP 200 with next question or summary; HTTP 400 on invalid input.</returns>
    [HttpPost("answer")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(AnswerIntakeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitAnswer(
        [FromBody] SubmitAnswerRequest request,
        CancellationToken ct)
    {
        if (!ValidateAppointmentOwnership(request.AppointmentId))
            return Forbid();

        if (string.IsNullOrWhiteSpace(request.RawAnswer))
            return BadRequest(new { message = "Answer cannot be empty." });

        // Enforce max answer length to prevent DoS (OWASP A04).
        const int MaxAnswerLength = 2000;
        var trimmedAnswer = request.RawAnswer.Trim();
        if (trimmedAnswer.Length > MaxAnswerLength)
            trimmedAnswer = trimmedAnswer[..MaxAnswerLength];

        var result = await _intakeService.SubmitAnswerAsync(
            request.AppointmentId, request.FieldKey, trimmedAnswer, ct);

        return Ok(new AnswerIntakeResponse(
            Phase:           result.Phase,
            NextFieldKey:    result.NextFieldKey,
            NextQuestionText: result.NextQuestionText,
            QuestionNumber:  result.QuestionNumber,
            TotalQuestions:  result.TotalQuestions,
            IsPhiField:      result.IsPhiField,
            FallbackRequired: result.FallbackRequired,
            QuickOptions:    result.QuickOptions,
            CapturedFields:  result.CapturedFields?
                .Select(f => new CapturedFieldDto(f.FieldKey, f.FieldLabel, f.Value, f.ManualRequired))
                .ToList()));
    }

    // ── GET /api/v1/intake/session/{appointmentId} ───────────────────────

    /// <summary>
    /// Returns the current partial session state for mid-session resume.
    /// Returns HTTP 404 when no active session exists.
    /// </summary>
    /// <param name="appointmentId">The appointment whose session is being resumed.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>HTTP 200 with partial state; HTTP 404 when session not found.</returns>
    [HttpGet("session/{appointmentId:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(SessionResumeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSession(
        Guid appointmentId,
        CancellationToken ct)
    {
        if (!ValidateAppointmentOwnership(appointmentId))
            return Forbid();

        var result = await _intakeService.GetSessionAsync(appointmentId, ct);
        if (result is null)
            return NotFound(new { message = "No active intake session found for this appointment." });

        return Ok(new SessionResumeResponse(
            AppointmentId:      result.AppointmentId,
            CurrentFieldIndex:  result.CurrentFieldIndex,
            TotalFields:        result.TotalFields,
            CurrentQuestionText: result.CurrentQuestionText,
            IsPhiField:         result.IsPhiField,
            CapturedFields:     result.CapturedFields
                .Select(f => new CapturedFieldDto(f.FieldKey, f.FieldLabel, f.Value, f.ManualRequired))
                .ToList()));
    }

    // ── POST /api/v1/intake/confirm ───────────────────────────────────────

    /// <summary>
    /// Confirms the AI intake session: persists the <c>IntakeRecord</c> with
    /// AES-256-GCM-encrypted form data and writes an <c>INTAKE_COMPLETED</c>
    /// audit entry (US_018, AC-004).
    /// </summary>
    /// <remarks>
    /// Returns HTTP 201 on first persist, HTTP 200 when the record already exists
    /// (idempotency). Includes the fallback <paramref name="request"/>'s
    /// <c>CapturedFields</c> in case the Redis session has expired.
    /// </remarks>
    [HttpPost("confirm")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ConfirmIntakeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ConfirmIntakeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ConfirmIntake(
        [FromBody] ConfirmIntakeRequest request,
        CancellationToken ct)
    {
        if (!HasValidSubClaim(request.AppointmentId, out var userId))
            return Forbid();

        try
        {
            var command = new ConfirmIntakeCommand(
                AppointmentId:  request.AppointmentId,
                UserId:         userId,
                FallbackFields: request.CapturedFields
                    ?.Select(f => new ConfirmIntakeCapturedField(f.FieldKey, f.Value, f.ManualRequired))
                    .ToList() ?? [],
                Method:         request.Method ?? "AI");

            var result = await _confirmHandler.HandleAsync(command, ct);
            var response = new ConfirmIntakeResponse(result.IntakeRecordId);

            return result.WasAlreadyCompleted
                ? Ok(response)
                : Created(string.Empty, response);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("does not exist"))
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Confirms that a valid JWT <c>sub</c> claim is present and parses it as a
    /// <see cref="Guid"/>. The <see cref="AuthorizeAttribute"/> guarantees
    /// authentication; this method validates sub format only.
    ///
    /// Full ownership verification (appointment.PatientId == userId) is performed
    /// inside <c>ConfirmIntakeHandler</c> after loading the appointment entity
    /// (OWASP A01 / CR-001 fix).
    /// </summary>
    private bool HasValidSubClaim(Guid appointmentId, out Guid userId)
    {
        userId = Guid.Empty;
        if (appointmentId == Guid.Empty) return false;
        var sub = User.FindFirstValue("sub");
        return !string.IsNullOrWhiteSpace(sub) && Guid.TryParse(sub, out userId);
    }

    /// <summary>
    /// Legacy helper used by <c>StartSession</c>, <c>SubmitAnswer</c>, and
    /// <c>GetSession</c> — confirms the JWT <c>sub</c> claim is present.
    /// Full ownership verification for those endpoints is deferred until those
    /// handlers gain appointment-lookup support.
    /// </summary>
    private bool ValidateAppointmentOwnership(Guid appointmentId)
    {
        var sub = User.FindFirstValue("sub");
        return !string.IsNullOrWhiteSpace(sub) && appointmentId != Guid.Empty;
    }
}

// ── Request / Response DTOs ──────────────────────────────────────────────────

/// <summary>Request body for <c>POST /intake/start</c>.</summary>
public sealed record StartIntakeRequest(Guid AppointmentId);

/// <summary>Response body for <c>POST /intake/start</c>.</summary>
public sealed record StartIntakeResponse(
    string FieldKey,
    string QuestionText,
    int QuestionNumber,
    int TotalQuestions,
    bool IsPhiField,
    string[]? QuickOptions);

/// <summary>Request body for <c>POST /intake/answer</c>.</summary>
public sealed record SubmitAnswerRequest(
    Guid AppointmentId,
    string FieldKey,
    string RawAnswer);

/// <summary>Response body for <c>POST /intake/answer</c>.</summary>
public sealed record AnswerIntakeResponse(
    string Phase,
    string? NextFieldKey,
    string? NextQuestionText,
    int? QuestionNumber,
    int? TotalQuestions,
    bool? IsPhiField,
    bool FallbackRequired,
    string[]? QuickOptions,
    IReadOnlyList<CapturedFieldDto>? CapturedFields);

/// <summary>A single captured field in a response body.</summary>
public sealed record CapturedFieldDto(
    string FieldKey,
    string FieldLabel,
    string Value,
    bool ManualRequired);

/// <summary>Response body for <c>GET /intake/session/{id}</c>.</summary>
public sealed record SessionResumeResponse(
    Guid AppointmentId,
    int CurrentFieldIndex,
    int TotalFields,
    string? CurrentQuestionText,
    bool? IsPhiField,
    IReadOnlyList<CapturedFieldDto> CapturedFields);

/// <summary>Request body for <c>POST /intake/confirm</c>.</summary>
/// <param name="Method">Optional intake method override. Defaults to <c>"AI"</c>.
/// Pass <c>"Manual"</c> from the manual form submission path (AC-003).</param>
public sealed record ConfirmIntakeRequest(
    Guid AppointmentId,
    IReadOnlyList<ConfirmIntakeFieldDto>? CapturedFields,
    string? Method);

/// <summary>A single captured field in the confirm-intake request (fallback payload).</summary>
public sealed record ConfirmIntakeFieldDto(
    string FieldKey,
    string Value,
    bool ManualRequired);

/// <summary>Response body for <c>POST /intake/confirm</c>.</summary>
public sealed record ConfirmIntakeResponse(Guid IntakeRecordId);
