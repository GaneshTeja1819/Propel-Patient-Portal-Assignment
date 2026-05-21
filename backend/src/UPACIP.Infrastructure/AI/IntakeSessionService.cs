using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using UPACIP.Application.Interfaces;

namespace UPACIP.Infrastructure.AI;

/// <summary>
/// Manages the AI conversational intake session state in Redis (AC-001, AC-002, AIR-001).
///
/// State key  : <c>intake-session-{appointmentId}</c>
/// TTL        : 5 minutes, sliding — reset on every successful answer (NFR-002).
/// Persistence: fields are accumulated in Redis only; the session is never written
///              to the database until <c>POST /api/v1/intake/confirm</c> (task_003).
///
/// Gemini retry policy: one retry per field on <see cref="IntakeSchemaValidationException"/>.
/// On second failure the field is marked <c>ManualRequired = true</c> and
/// <c>IntakeMethod</c> becomes <c>"AI-Partial"</c> (AC-002 edge case).
/// </summary>
internal sealed class IntakeSessionService : IIntakeSessionService
{
    private const string KeyPrefix  = "intake-session-";
    private static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(5);

    private readonly IConnectionMultiplexer? _redis;
    private readonly GeminiIntakeAdapter      _adapter;
    private readonly IntakeQuestion[]         _questions;
    private readonly IEncryptionService       _encryptionService;
    private readonly ILogger<IntakeSessionService> _logger;

    /// <summary>Keys of intake fields that are classified as PHI (from IntakeQuestions.json).</summary>
    private readonly HashSet<string> _phiFieldKeys;

    /// <summary>In-memory fallback used when Redis is not configured (dev/test only).</summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _memoryStore = new();

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public IntakeSessionService(
        IConnectionMultiplexer? redis,
        GeminiIntakeAdapter adapter,
        IReadOnlyList<IntakeQuestion> questions,
        IEncryptionService encryptionService,
        ILogger<IntakeSessionService> logger)
    {
        // BL-002: guard against empty question list so StartSessionAsync does not throw
        // IndexOutOfRangeException at runtime.
        if (questions.Count == 0)
            throw new InvalidOperationException(
                "IntakeQuestions.json contained no questions. " +
                "Verify the embedded resource is present and non-empty.");

        _redis             = redis;
        _adapter           = adapter;
        _questions         = questions.ToArray();
        _encryptionService = encryptionService;
        _logger            = logger;
        _phiFieldKeys      = questions.Where(q => q.IsPhiField).Select(q => q.FieldKey).ToHashSet();
    }

    // ── StartSessionAsync ───────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<StartSessionResult> StartSessionAsync(
        Guid appointmentId,
        CancellationToken ct = default)
    {
        var state = new IntakeSessionState
        {
            AppointmentId     = appointmentId,
            CurrentFieldIndex = 0,
            IntakeMethod      = "AI",
        };

        await SaveStateAsync(appointmentId, state);

        var first = _questions[0];
        return new StartSessionResult(
            FieldKey:       first.FieldKey,
            QuestionText:   first.QuestionText,
            QuestionNumber: 1,
            TotalQuestions: _questions.Length,
            IsPhiField:     first.IsPhiField,
            QuickOptions:   first.QuickOptions);
    }

    // ── SubmitAnswerAsync ────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<AnswerResult> SubmitAnswerAsync(
        Guid appointmentId,
        string fieldKey,
        string rawAnswer,
        CancellationToken ct = default)
    {
        var state = await LoadStateAsync(appointmentId);
        if (state is null)
        {
            // Session expired or never started — auto-restart.
            _logger.LogWarning(
                "No active intake session for appointment {AppointmentId}. Auto-restarting.",
                appointmentId);
            await StartSessionAsync(appointmentId, ct);
            state = await LoadStateAsync(appointmentId)
                    ?? throw new InvalidOperationException("Failed to initialise intake session.");
        }

        var questionIdx = state.CurrentFieldIndex;
        if (questionIdx >= _questions.Length)
        {
            // All fields already captured — return summary immediately.
            return BuildSummaryResult(state);
        }

        var question = _questions[questionIdx];

        // Validate the caller supplied the correct fieldKey for this index.
        if (!string.Equals(question.FieldKey, fieldKey, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "FieldKey mismatch: expected {Expected}, received {Received}. " +
                "Proceeding with expected field.",
                question.FieldKey, fieldKey);
            fieldKey = question.FieldKey;
        }

        // ── Call Gemini (AC-002: retry once on schema validation failure) ──
        object? parsedValue      = null;
        bool    fallbackRequired = false;

        try
        {
            parsedValue = await _adapter.CallFieldAsync(fieldKey, rawAnswer, question, ct);
        }
        catch (IntakeSchemaValidationException firstEx)
        {
            _logger.LogWarning(firstEx,
                "First Gemini validation failure for field {FieldKey}. Retrying once.",
                fieldKey);

            try
            {
                parsedValue = await _adapter.CallFieldAsync(fieldKey, rawAnswer, question, ct);
            }
            catch (IntakeSchemaValidationException secondEx)
            {
                _logger.LogWarning(secondEx,
                    "Second Gemini validation failure for field {FieldKey}. Marking as manual.",
                    fieldKey);

                fallbackRequired        = true;
                state.IntakeMethod      = "AI-Partial";
                state.ManualRequiredFields.Add(fieldKey);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Rate limit, network error, or unexpected Gemini failure → treat as fallback.
            _logger.LogError(ex,
                "Gemini call failed for field {FieldKey}. Marking as manual.",
                fieldKey);

            fallbackRequired        = true;
            state.IntakeMethod      = "AI-Partial";
            state.ManualRequiredFields.Add(fieldKey);
        }

        // ── Persist the captured value ─────────────────────────────────────
        var serialisedValue = parsedValue is not null
            ? JsonSerializer.Serialize(parsedValue, JsonOpts)
            : rawAnswer; // store raw on fallback so manual form can pre-fill

        state.CapturedFields[fieldKey] = serialisedValue;
        state.CurrentFieldIndex++;

        await SaveStateAsync(appointmentId, state);

        // ── Advance to next field or return summary ────────────────────────
        if (state.CurrentFieldIndex >= _questions.Length)
            return BuildSummaryResult(state);

        var next = _questions[state.CurrentFieldIndex];
        return new AnswerResult(
            Phase:            "conversation",
            NextFieldKey:     next.FieldKey,
            NextQuestionText: next.QuestionText,
            QuestionNumber:   state.CurrentFieldIndex + 1,
            TotalQuestions:   _questions.Length,
            IsPhiField:       next.IsPhiField,
            FallbackRequired: fallbackRequired,
            QuickOptions:     next.QuickOptions,
            CapturedFields:   null);
    }

    // ── GetSessionAsync ──────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<SessionResumeResult?> GetSessionAsync(
        Guid appointmentId,
        CancellationToken ct = default)
    {
        var state = await LoadStateAsync(appointmentId);
        if (state is null) return null;

        IntakeQuestion? current = state.CurrentFieldIndex < _questions.Length
            ? _questions[state.CurrentFieldIndex]
            : null;

        var captured = state.CapturedFields
            .Select(kvp =>
            {
                var q = _questions.FirstOrDefault(q => q.FieldKey == kvp.Key);
                return new CapturedIntakeField(
                    FieldKey:       kvp.Key,
                    FieldLabel:     q?.FieldLabel ?? kvp.Key,
                    Value:          kvp.Value,
                    ManualRequired: state.ManualRequiredFields.Contains(kvp.Key));
            })
            .ToList();

        return new SessionResumeResult(
            AppointmentId:      appointmentId,
            CurrentFieldIndex:  state.CurrentFieldIndex,
            TotalFields:        _questions.Length,
            CurrentQuestionText: current?.QuestionText,
            IsPhiField:         current?.IsPhiField,
            CapturedFields:     captured);
    }

    // ── ClearSessionAsync ────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task ClearSessionAsync(Guid appointmentId, CancellationToken ct = default)
    {
        if (_redis is null)
        {
            _memoryStore.TryRemove(KeyPrefix + appointmentId, out _);
            return;
        }

        await _redis.GetDatabase().KeyDeleteAsync(KeyPrefix + appointmentId);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private AnswerResult BuildSummaryResult(IntakeSessionState state)
    {
        var captured = state.CapturedFields
            .Select(kvp =>
            {
                var q = _questions.FirstOrDefault(q => q.FieldKey == kvp.Key);
                return new CapturedIntakeField(
                    FieldKey:       kvp.Key,
                    FieldLabel:     q?.FieldLabel ?? kvp.Key,
                    Value:          kvp.Value,
                    ManualRequired: state.ManualRequiredFields.Contains(kvp.Key));
            })
            .ToList();

        return new AnswerResult(
            Phase:            "summary",
            NextFieldKey:     null,
            NextQuestionText: null,
            QuestionNumber:   null,
            TotalQuestions:   null,
            IsPhiField:       null,
            FallbackRequired: false,
            QuickOptions:     null,
            CapturedFields:   captured);
    }

    private async Task SaveStateAsync(Guid appointmentId, IntakeSessionState state)
    {
        if (_redis is null)
        {
            _logger.LogWarning(
                "Redis unavailable — using in-memory session store for {AppointmentId} (dev only).",
                appointmentId);
            var fallbackJson = JsonSerializer.Serialize(state, JsonOpts);
            _memoryStore[KeyPrefix + appointmentId] = fallbackJson;
            return;
        }

        // SEC-001: encrypt PHI field values before writing to Redis so that
        // PHI is never stored in plaintext at rest.
        var encryptedFields = state.CapturedFields
            .ToDictionary(
                kvp => kvp.Key,
                kvp => _phiFieldKeys.Contains(kvp.Key)
                    ? _encryptionService.Encrypt(kvp.Value)
                    : kvp.Value);

        var stateToWrite = new IntakeSessionState
        {
            AppointmentId     = state.AppointmentId,
            CurrentFieldIndex = state.CurrentFieldIndex,
            IntakeMethod      = state.IntakeMethod,
            CapturedFields    = encryptedFields,
            ManualRequiredFields = state.ManualRequiredFields,
        };

        var json = JsonSerializer.Serialize(stateToWrite, JsonOpts);
        await _redis.GetDatabase()
            .StringSetAsync(KeyPrefix + appointmentId, json, SessionTtl);
    }

    private async Task<IntakeSessionState?> LoadStateAsync(Guid appointmentId)
    {
        if (_redis is null)
        {
            _memoryStore.TryGetValue(KeyPrefix + appointmentId, out var fallback);
            return fallback is null ? null : JsonSerializer.Deserialize<IntakeSessionState>(fallback, JsonOpts);
        }

        try
        {
            var raw = await _redis.GetDatabase()
                .StringGetAsync(KeyPrefix + appointmentId);

            if (!raw.HasValue) return null;

            // Refresh the sliding TTL on every read (NFR-002).
            await _redis.GetDatabase()
                .KeyExpireAsync(KeyPrefix + appointmentId, SessionTtl);

            var state = JsonSerializer.Deserialize<IntakeSessionState>(raw.ToString(), JsonOpts);

            if (state is not null)
            {
                // SEC-001: decrypt PHI field values after loading from Redis.
                state.CapturedFields = state.CapturedFields
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => _phiFieldKeys.Contains(kvp.Key)
                            ? _encryptionService.Decrypt(kvp.Value)
                            : kvp.Value);
            }

            return state;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex,
                "Redis read failed for intake session {AppointmentId}.",
                appointmentId);
            return null;
        }
    }
}

// ── Internal session state model (Redis only) ────────────────────────────────

/// <summary>
/// Serialised to Redis as <c>intake-session-{appointmentId}</c>.
/// Never written to the database directly (task_003 handles persistence).
/// </summary>
internal sealed class IntakeSessionState
{
    public Guid   AppointmentId     { get; set; }
    public int    CurrentFieldIndex { get; set; }
    public string IntakeMethod      { get; set; } = "AI";

    /// <summary>
    /// Accumulated field values keyed by <c>fieldKey</c>.
    /// Values are JSON-serialised strings (could be scalars or arrays).
    /// </summary>
    public Dictionary<string, string> CapturedFields { get; set; } = new();

    /// <summary>Fields that Gemini failed to parse after max retries (AC-002).</summary>
    public List<string> ManualRequiredFields { get; set; } = [];
}
