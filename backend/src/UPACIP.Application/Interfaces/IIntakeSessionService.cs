namespace UPACIP.Application.Interfaces;

/// <summary>
/// Manages the AI conversational intake session lifecycle (US_018, AC-001–AC-002).
///
/// Session state is held in Redis with a 5-minute sliding TTL and is never
/// persisted to the database until <c>ConfirmAsync</c> is called (task_003).
/// </summary>
public interface IIntakeSessionService
{
    /// <summary>
    /// Initialises a new intake session in Redis and returns the first question.
    /// </summary>
    Task<StartSessionResult> StartSessionAsync(Guid appointmentId, CancellationToken ct = default);

    /// <summary>
    /// Submits the patient's raw answer to Gemini, advances the session index,
    /// and returns either the next question or a summary signal.
    /// </summary>
    Task<AnswerResult> SubmitAnswerAsync(
        Guid appointmentId,
        string fieldKey,
        string rawAnswer,
        CancellationToken ct = default);

    /// <summary>
    /// Loads the current session state from Redis for mid-session resume.
    /// Returns <see langword="null"/> when no active session exists.
    /// </summary>
    Task<SessionResumeResult?> GetSessionAsync(Guid appointmentId, CancellationToken ct = default);

    /// <summary>
    /// Deletes the Redis session key for the specified appointment.
    /// Called by <c>ConfirmIntakeHandler</c> after a successful DB persist (task_003).
    /// Silently no-ops when Redis is unavailable.
    /// </summary>
    Task ClearSessionAsync(Guid appointmentId, CancellationToken ct = default);
}

// ── Result types (DTOs crossing Application ↔ API boundary) ────────────────

/// <summary>Result of <see cref="IIntakeSessionService.StartSessionAsync"/>.</summary>
public sealed record StartSessionResult(
    string FieldKey,
    string QuestionText,
    int QuestionNumber,
    int TotalQuestions,
    bool IsPhiField,
    string[]? QuickOptions);

/// <summary>Result of <see cref="IIntakeSessionService.SubmitAnswerAsync"/>.</summary>
public sealed record AnswerResult(
    string Phase,               // "conversation" | "summary"
    string? NextFieldKey,
    string? NextQuestionText,
    int? QuestionNumber,
    int? TotalQuestions,
    bool? IsPhiField,
    bool FallbackRequired,
    string[]? QuickOptions,
    IReadOnlyList<CapturedIntakeField>? CapturedFields);

/// <summary>Result of <see cref="IIntakeSessionService.GetSessionAsync"/>.</summary>
public sealed record SessionResumeResult(
    Guid AppointmentId,
    int CurrentFieldIndex,
    int TotalFields,
    string? CurrentQuestionText,
    bool? IsPhiField,
    IReadOnlyList<CapturedIntakeField> CapturedFields);

/// <summary>One captured field in the ongoing intake session.</summary>
public sealed record CapturedIntakeField(
    string FieldKey,
    string FieldLabel,
    string Value,
    bool ManualRequired);
