using System.Text.Json;
using Microsoft.Extensions.Logging;
using UPACIP.Application.Commands.Intake;
using UPACIP.Application.Interfaces;
using UPACIP.Domain.Entities;

namespace UPACIP.Application.Handlers.Intake;

/// <summary>
/// Handles the confirm-intake flow (US_018, AC-004):
/// <list type="number">
///   <item>Verifies the caller is the patient for the appointment (OWASP A01 / CR-001 fix).</item>
///   <item>Idempotency: returns the existing record ID without re-inserting (AC-004 edge case).</item>
///   <item>Loads the Redis session; falls back to the request payload on expiry (AC-004 edge case).</item>
///   <item>Persists an <see cref="IntakeRecord"/> — form data is AES-256-GCM-encrypted at the
///         database layer by EF Core's <c>EncryptedStringConverter</c> (AC-004, NFR-001, DR-001).</item>
///   <item>Clears the Redis session key on successful persistence.</item>
///   <item>Writes an <c>INTAKE_COMPLETED</c> audit entry (AC-004).</item>
/// </list>
/// </summary>
public sealed class ConfirmIntakeHandler
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly IIntakeRecordRepository  _repo;
    private readonly IUnitOfWork             _uow;
    private readonly IIntakeSessionService   _intakeService;
    private readonly IAuditLogService        _auditService;
    private readonly ILogger<ConfirmIntakeHandler> _logger;

    public ConfirmIntakeHandler(
        IIntakeRecordRepository repo,
        IUnitOfWork uow,
        IIntakeSessionService intakeService,
        IAuditLogService auditService,
        ILogger<ConfirmIntakeHandler> logger)
    {
        _repo          = repo;
        _uow           = uow;
        _intakeService = intakeService;
        _auditService  = auditService;
        _logger        = logger;
    }

    /// <summary>
    /// Executes the confirm-intake orchestration.
    /// Throws <see cref="UnauthorizedAccessException"/> when the caller does not own the appointment.
    /// Throws <see cref="InvalidOperationException"/> when the appointment does not exist.
    /// </summary>
    public async Task<ConfirmIntakeResult> HandleAsync(
        ConfirmIntakeCommand command,
        CancellationToken ct = default)
    {
        // ── 1. Ownership check (OWASP A01 / CR-001 fix) ─────────────────
        // Load the appointment's PatientId from the database — this is the
        // authoritative ownership check that replaces the stub in task_002.
        var appointmentPatientId = await _repo.GetAppointmentPatientIdAsync(command.AppointmentId, ct);
        if (appointmentPatientId is null)
            throw new InvalidOperationException(
                $"Appointment {command.AppointmentId} does not exist.");

        if (appointmentPatientId.Value != command.UserId)
            throw new UnauthorizedAccessException(
                "The authenticated user is not the patient for this appointment.");

        // ── 2. Idempotency (AC-004 edge case) ───────────────────────────
        var existing = await _repo.FindByAppointmentIdAsync(command.AppointmentId, ct);
        if (existing is not null)
            return new ConfirmIntakeResult(existing.Id, WasAlreadyCompleted: true);

        // ── 3. Resolve captured fields ───────────────────────────────────
        IReadOnlyList<ConfirmIntakeCapturedField> fields;
        string method;

        if (command.Method == "Manual")
        {
            // Manual path (AC-003): no Redis session; form payload is the
            // authoritative source. Skip GetSessionAsync entirely.
            fields = command.FallbackFields;
            method = "Manual";
        }
        else
        {
            // AI path: prefer Redis session (authoritative). If Redis has
            // expired, use the fallback payload sent in the request body.
            var session = await _intakeService.GetSessionAsync(command.AppointmentId, ct);
            if (session is not null)
            {
                fields = session.CapturedFields
                    .Select(f => new ConfirmIntakeCapturedField(f.FieldKey, f.Value, f.ManualRequired))
                    .ToList();
            }
            else
            {
                _logger.LogWarning(
                    "Redis session expired for appointment {AppointmentId}. " +
                    "Persisting fallback fields from request body.",
                    command.AppointmentId);
                fields = command.FallbackFields;
            }

            method = fields.Any(f => f.ManualRequired) ? "AI-Partial" : "AI";
        }

        // ── 4. Serialise form data ───────────────────────────────────────
        // Do NOT pre-encrypt — AppDbContext's EncryptedStringConverter applies
        // AES-256-GCM transparently on SaveChanges (AC-004, NFR-001, DR-001).
        var formDataJson = JsonSerializer.Serialize(
            fields.ToDictionary(f => f.FieldKey, f => f.Value),
            JsonOpts);

        // ── 5. Persist IntakeRecord ──────────────────────────────────────
        var record = new IntakeRecord
        {
            PatientId         = appointmentPatientId.Value,
            AppointmentId     = command.AppointmentId,
            EncryptedFormData = formDataJson,
            FormType          = method,
            SubmittedAt       = DateTimeOffset.UtcNow,
        };

        await _repo.AddAsync(record, ct);
        await _uow.SaveChangesAsync(ct);

        // ── 6. Clear Redis session (AI / AI-Partial path only) ──────────────
        // Manual submissions have no Redis session to clear (AC-003).
        if (command.Method != "Manual")
            await _intakeService.ClearSessionAsync(command.AppointmentId, ct);

        // ── 7. Audit log (non-blocking — failure must not roll back persist) ──
        try
        {
            await _auditService.LogAsync(
                actorId:           command.UserId,
                actorRole:         "Patient",
                actionType:        "INTAKE_COMPLETED",
                targetEntity:      "IntakeRecord",
                targetId:          record.Id,
                metadata:          $"{{\"appointmentId\":\"{command.AppointmentId}\",\"method\":\"{method}\"}}",
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Audit log write failed for INTAKE_COMPLETED on appointment {AppointmentId}. " +
                "IntakeRecord {IntakeRecordId} was persisted successfully.",
                command.AppointmentId,
                record.Id);
        }

        return new ConfirmIntakeResult(record.Id, WasAlreadyCompleted: false);
    }
}
