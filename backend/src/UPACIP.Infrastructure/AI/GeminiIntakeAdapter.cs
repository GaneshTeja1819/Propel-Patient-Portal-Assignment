using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using UPACIP.Application.Interfaces;

namespace UPACIP.Infrastructure.AI;

/// <summary>
/// Adapts the generic <see cref="IGeminiClient"/> to the structured intake-question
/// workflow (AC-001, AC-002, AIR-001).
///
/// For each intake field the adapter:
/// <list type="number">
///   <item>Constructs a prompt by interpolating the patient's raw answer into the
///         field's <c>promptTemplate</c> from <c>IntakeQuestions.json</c>.</item>
///   <item>Calls <see cref="IGeminiClient.InvokeStructuredAsync{TResponse}"/> targeting
///         <see cref="IntakeFieldResponse"/>.</item>
///   <item>Validates the returned <c>ParsedValue</c> against the field's JSON schema
///         (type check + enum check). Throws <see cref="IntakeSchemaValidationException"/>
///         on mismatch so the caller can retry and ultimately fall back to manual input.</item>
/// </list>
/// </summary>
internal sealed class GeminiIntakeAdapter
{
    private readonly IGeminiClient _geminiClient;
    private readonly ILogger<GeminiIntakeAdapter> _logger;

    public GeminiIntakeAdapter(IGeminiClient geminiClient, ILogger<GeminiIntakeAdapter> logger)
    {
        _geminiClient = geminiClient;
        _logger = logger;
    }

    /// <summary>
    /// Sends the patient's raw answer to Gemini and returns the validated parsed value
    /// as a JSON-serialisable object.
    /// </summary>
    /// <param name="fieldKey">Intake field key (used for logging).</param>
    /// <param name="rawAnswer">Free-text patient response.</param>
    /// <param name="question">The question definition from <c>IntakeQuestions.json</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Validated parsed value (string, string[], etc.).</returns>
    /// <exception cref="IntakeSchemaValidationException">
    /// Thrown when Gemini's output does not satisfy the field's JSON schema.
    /// Callers should retry once; on second failure, mark the field as manual-required.
    /// </exception>
    public async Task<object> CallFieldAsync(
        string fieldKey,
        string rawAnswer,
        IntakeQuestion question,
        CancellationToken ct = default)
    {
        // SEC-003: fence patient-supplied text so it cannot be interpreted as
        // model instructions (prompt injection defence).
        var fencedAnswer = $"<patient_answer>{rawAnswer}</patient_answer>";
        var prompt = question.PromptTemplate.Replace("{rawAnswer}", fencedAnswer, StringComparison.Ordinal)
            + "\n\nReturn a JSON object with a single key \"parsedValue\" whose value matches the required schema. "
            + "Do not include any other keys or explanatory text.";

        // BL-001: pass the wrapper schema so Gemini's structured-output mode
        // enforces { parsedValue: <field-value> } as the response envelope.
        var wrapperSchema = BuildWrapperSchema(question.Schema);

        _logger.LogInformation(
            "Calling Gemini for field {FieldKey} — promptLength={Len}.",
            fieldKey, prompt.Length);

        var response = await _geminiClient.InvokeStructuredAsync<IntakeFieldResponse>(
            prompt, jsonSchema: wrapperSchema, cancellationToken: ct);

        ValidateAgainstSchema(fieldKey, response.ParsedValue, question.Schema);

        return response.ParsedValue;
    }

    // ── Schema helpers ──────────────────────────────────────────────────

    /// <summary>
    /// Builds a JSON schema object that wraps the field's value type under
    /// the key <c>parsedValue</c>, matching <see cref="IntakeFieldResponse"/>.
    /// Passed to <see cref="IGeminiClient.InvokeStructuredAsync{TResponse}"/> so
    /// Gemini's structured-output mode enforces the expected shape.
    /// </summary>
    private static object BuildWrapperSchema(IntakeFieldSchema fieldSchema)
    {
        Dictionary<string, object?> valueSchema = fieldSchema.Type switch
        {
            "array" => new()
            {
                ["type"] = "array",
                ["items"] = new Dictionary<string, object> { ["type"] = "string" },
            },
            _ => BuildStringSchema(fieldSchema),
        };

        return new
        {
            type = "object",
            properties = new Dictionary<string, object?> { ["parsedValue"] = valueSchema },
            required = new[] { "parsedValue" },
        };
    }

    private static Dictionary<string, object?> BuildStringSchema(IntakeFieldSchema fieldSchema)
    {
        var dict = new Dictionary<string, object?> { ["type"] = "string" };
        if (fieldSchema.MinLength.HasValue) dict["minLength"] = fieldSchema.MinLength.Value;
        if (fieldSchema.MaxLength.HasValue) dict["maxLength"] = fieldSchema.MaxLength.Value;
        if (fieldSchema.Enum?.Length > 0)   dict["enum"]      = fieldSchema.Enum;
        return dict;
    }

    // ── Schema validation ───────────────────────────────────────────────

    private static void ValidateAgainstSchema(
        string fieldKey,
        JsonElement parsedValue,
        IntakeFieldSchema schema)
    {
        switch (schema.Type)
        {
            case "string":
                if (parsedValue.ValueKind != JsonValueKind.String)
                    throw new IntakeSchemaValidationException(
                        fieldKey, $"Expected string, got {parsedValue.ValueKind}.");

                var str = parsedValue.GetString() ?? string.Empty;

                if (schema.MinLength.HasValue && str.Length < schema.MinLength.Value)
                    throw new IntakeSchemaValidationException(
                        fieldKey, $"String too short: {str.Length} < {schema.MinLength}.");

                if (schema.MaxLength.HasValue && str.Length > schema.MaxLength.Value)
                    throw new IntakeSchemaValidationException(
                        fieldKey, $"String too long: {str.Length} > {schema.MaxLength}.");

                if (schema.Enum?.Length > 0 && !schema.Enum.Contains(str, StringComparer.OrdinalIgnoreCase))
                    throw new IntakeSchemaValidationException(
                        fieldKey, $"Value '{str}' is not in allowed enum: [{string.Join(", ", schema.Enum)}].");
                break;

            case "array":
                if (parsedValue.ValueKind != JsonValueKind.Array)
                    throw new IntakeSchemaValidationException(
                        fieldKey, $"Expected array, got {parsedValue.ValueKind}.");
                break;

            default:
                // Unknown schema type — pass through; log warning rather than block.
                break;
        }
    }
}

// ── Supporting types ────────────────────────────────────────────────────────

/// <summary>Gemini structured response for a single intake field.</summary>
internal sealed class IntakeFieldResponse
{
    /// <summary>
    /// The parsed value as returned by Gemini — could be a string, array, etc.
    /// JsonElement allows us to forward the raw JSON to schema validation without
    /// an intermediate deserialisation step.
    /// </summary>
    [JsonPropertyName("parsedValue")]
    public JsonElement ParsedValue { get; init; }
}

/// <summary>
/// One entry in <c>IntakeQuestions.json</c>.
/// Loaded once at startup by <see cref="IntakeSessionService"/>.
/// </summary>
public sealed class IntakeQuestion
{
    [JsonPropertyName("fieldKey")]
    public string FieldKey { get; init; } = string.Empty;

    [JsonPropertyName("fieldLabel")]
    public string FieldLabel { get; init; } = string.Empty;

    [JsonPropertyName("isPhiField")]
    public bool IsPhiField { get; init; }

    [JsonPropertyName("promptTemplate")]
    public string PromptTemplate { get; init; } = string.Empty;

    [JsonPropertyName("questionText")]
    public string QuestionText { get; init; } = string.Empty;

    [JsonPropertyName("schema")]
    public IntakeFieldSchema Schema { get; init; } = new();

    [JsonPropertyName("quickOptions")]
    public string[]? QuickOptions { get; init; }
}

/// <summary>JSON schema constraints for an intake field.</summary>
public sealed class IntakeFieldSchema
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "string";

    [JsonPropertyName("minLength")]
    public int? MinLength { get; init; }

    [JsonPropertyName("maxLength")]
    public int? MaxLength { get; init; }

    [JsonPropertyName("enum")]
    public string[]? Enum { get; init; }
}

/// <summary>
/// Thrown when Gemini's structured output does not satisfy the field's JSON schema.
/// Callers (IntakeSessionService) retry once; on second failure mark the field as
/// manual-required (AC-002 edge case).
/// </summary>
public sealed class IntakeSchemaValidationException : Exception
{
    public string FieldKey { get; }

    public IntakeSchemaValidationException(string fieldKey, string reason)
        : base($"Schema validation failed for field '{fieldKey}': {reason}")
    {
        FieldKey = fieldKey;
    }
}
