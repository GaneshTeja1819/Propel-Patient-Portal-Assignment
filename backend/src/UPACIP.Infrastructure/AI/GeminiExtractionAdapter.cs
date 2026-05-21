using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using UPACIP.Application.Interfaces;

namespace UPACIP.Infrastructure.AI;

/// <summary>
/// Adapts the generic <see cref="IGeminiClient"/> to the AI clinical data extraction
/// workflow (AC-001, AIR-002).
///
/// <para>
/// Builds the structured extraction prompt from the embedded
/// <c>ClinicalExtractionPrompt.json</c> resource, calls the model, and validates
/// the schema version of the response before returning.
/// </para>
///
/// <para>
/// <c>AI_INVOCATION</c> audit entries (AC-003, AIR-006) are written automatically by
/// <see cref="GeminiInvocationLogger"/> — the decorator that wraps <see cref="IGeminiClient"/>
/// in the DI container — so this adapter does not need to write audit entries directly.
/// </para>
/// </summary>
internal sealed class GeminiExtractionAdapter
{
    private const string LockedSchemaVersion = "1.0";

    private readonly IGeminiClient _geminiClient;
    private readonly string _promptTemplate;
    private readonly ILogger<GeminiExtractionAdapter> _logger;

    public GeminiExtractionAdapter(
        IGeminiClient geminiClient,
        ILogger<GeminiExtractionAdapter> logger)
    {
        _geminiClient = geminiClient;
        _logger = logger;
        _promptTemplate = LoadPromptTemplate();
    }

    /// <summary>
    /// Sends <paramref name="documentText"/> to Gemini with the versioned extraction prompt
    /// and returns the validated <see cref="ClinicalExtractionResult"/>.
    /// </summary>
    /// <exception cref="SchemaVersionMismatchException">
    /// Thrown when the response <c>schemaVersion</c> does not match
    /// <see cref="LockedSchemaVersion"/>. Treated as a non-retriable failure by
    /// <see cref="ClinicalDataExtractionJob"/> (edge case: schema evolution).
    /// </exception>
    public async Task<ClinicalExtractionResult> CallExtractionAsync(
        string documentText,
        CancellationToken ct = default)
    {
        // Fence document text to prevent prompt injection (OWASP A03).
        var fencedText = $"<document_text>{documentText}</document_text>";
        var prompt = _promptTemplate.Replace("{documentText}", fencedText, StringComparison.Ordinal);

        _logger.LogInformation(
            "Invoking Gemini for clinical extraction. PromptLength={Len}.", prompt.Length);

        var result = await _geminiClient.InvokeStructuredAsync<ClinicalExtractionResult>(
            prompt, cancellationToken: ct);

        if (!string.Equals(result.SchemaVersion, LockedSchemaVersion, StringComparison.Ordinal))
        {
            throw new SchemaVersionMismatchException(result.SchemaVersion, LockedSchemaVersion);
        }

        return result;
    }

    // ── Embedded resource loader ──────────────────────────────────────────

    private static string LoadPromptTemplate()
    {
        var assembly = typeof(GeminiExtractionAdapter).Assembly;
        const string ResourceName = "UPACIP.Infrastructure.AI.ClinicalExtractionPrompt.json";

        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{ResourceName}' not found in UPACIP.Infrastructure assembly. " +
                "Ensure the file Build Action is set to 'Embedded Resource'.");

        var resource = JsonSerializer.Deserialize<ClinicalExtractionPromptResource>(
            stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("ClinicalExtractionPrompt.json deserialized to null.");

        return resource.PromptTemplate;
    }

    // ── Embedded resource DTO ─────────────────────────────────────────────

    private sealed record ClinicalExtractionPromptResource(
        [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
        [property: JsonPropertyName("promptTemplate")] string PromptTemplate);
}

// ── Public result types ────────────────────────────────────────────────────

/// <summary>
/// Structured JSON response from Gemini for clinical data extraction (AIR-002).
/// Serialised to plaintext JSON and stored in
/// <c>ExtractedClinicalData.EncryptedExtractedJson</c>; EF Core encrypts the column
/// with AES-256-GCM on <c>SaveChanges</c> (AC-002).
/// </summary>
public sealed record ClinicalExtractionResult
{
    [JsonPropertyName("vitals")]
    public VitalsData? Vitals { get; init; }

    [JsonPropertyName("medications")]
    public MedicationItem[] Medications { get; init; } = [];

    [JsonPropertyName("diagnoses")]
    public DiagnosisItem[] Diagnoses { get; init; } = [];

    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; init; } = "1.0";

    public sealed record VitalsData
    {
        [JsonPropertyName("bloodPressure")] public string?  BloodPressure { get; init; }
        [JsonPropertyName("heartRate")]     public double?  HeartRate     { get; init; }
        [JsonPropertyName("weight")]        public double?  Weight        { get; init; }
        [JsonPropertyName("temperature")]   public double?  Temperature   { get; init; }
    }

    public sealed record MedicationItem
    {
        [JsonPropertyName("name")]      public string? Name      { get; init; }
        [JsonPropertyName("dosage")]    public string? Dosage    { get; init; }
        [JsonPropertyName("frequency")] public string? Frequency { get; init; }
    }

    public sealed record DiagnosisItem
    {
        [JsonPropertyName("icdCode")]     public string? IcdCode     { get; init; }
        [JsonPropertyName("description")] public string? Description { get; init; }
    }
}

/// <summary>
/// Thrown when Gemini's extraction response carries a schema version that does not match
/// the version locked in <c>ClinicalExtractionPrompt.json</c> (edge case: schema evolution).
/// The <see cref="ClinicalDataExtractionJob"/> catches this as a non-retriable failure and
/// sets <c>ExtractionStatus = "Failed"</c> without re-throwing so Hangfire does not retry.
/// </summary>
public sealed class SchemaVersionMismatchException : Exception
{
    public string ReceivedVersion { get; }
    public string ExpectedVersion { get; }

    public SchemaVersionMismatchException(string? received, string expected)
        : base($"Schema version mismatch: expected '{expected}', received '{received ?? "null"}'.")
    {
        ReceivedVersion = received ?? string.Empty;
        ExpectedVersion = expected;
    }
}
