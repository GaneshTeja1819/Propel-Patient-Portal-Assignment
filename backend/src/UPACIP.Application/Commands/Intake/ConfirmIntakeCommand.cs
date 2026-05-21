namespace UPACIP.Application.Commands.Intake;

/// <summary>
/// Command for <c>POST /api/v1/intake/confirm</c> (US_018, AC-004).
///
/// Carries the caller identity and the fallback field payload in case the Redis
/// session has expired before the confirm request arrives.
/// </summary>
/// <param name="AppointmentId">Appointment being completed.</param>
/// <param name="UserId">Patient's user ID extracted from the JWT <c>sub</c> claim.</param>
/// <param name="FallbackFields">
/// Field values provided by the client (from the summary screen).
/// Used only when the Redis session cannot be found; otherwise the session's
/// captured fields take precedence.
/// For method="Manual" this is always the authoritative source.
/// </param>
/// <param name="Method">
/// Intake submission method: <c>"AI"</c> (default), <c>"AI-Partial"</c>, or <c>"Manual"</c>.
/// When <c>"Manual"</c>, the Redis session lookup is skipped entirely (AC-003).
/// </param>
public sealed record ConfirmIntakeCommand(
    Guid AppointmentId,
    Guid UserId,
    IReadOnlyList<ConfirmIntakeCapturedField> FallbackFields,
    string Method = "AI");

/// <summary>A single captured field in the confirm-intake fallback payload.</summary>
/// <param name="FieldKey">Machine-readable key matching IntakeQuestions.json.</param>
/// <param name="Value">Patient-supplied value (plain text; encrypted by EF Core converter on DB write).</param>
/// <param name="ManualRequired">True when Gemini failed to parse the answer; affects method determination.</param>
public sealed record ConfirmIntakeCapturedField(
    string FieldKey,
    string Value,
    bool ManualRequired);

/// <summary>Result returned by <c>ConfirmIntakeHandler</c>.</summary>
/// <param name="IntakeRecordId">ID of the newly created (or pre-existing) <see cref="UPACIP.Domain.Entities.IntakeRecord"/>.</param>
/// <param name="WasAlreadyCompleted">True when an idempotency hit was detected; signals HTTP 200 to the caller.</param>
public sealed record ConfirmIntakeResult(
    Guid IntakeRecordId,
    bool WasAlreadyCompleted);
