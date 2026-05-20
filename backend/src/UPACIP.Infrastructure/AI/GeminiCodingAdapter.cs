using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using UPACIP.Application.Interfaces;

namespace UPACIP.Infrastructure.AI;

/// <summary>
/// Adapts the generic <see cref="IGeminiClient"/> to the AI medical code suggestion
/// workflow (US_029, AC-001, AIR-003).
///
/// <para>
/// Builds the structured code suggestion prompt from the embedded
/// <c>CodeSuggestionPrompt.json</c> resource, calls the model, and validates
/// the schema version of the response before returning.
/// </para>
///
/// <para>
/// <c>AI_INVOCATION</c> audit entries (AC-004, AIR-006) are written automatically by
/// <see cref="GeminiInvocationLogger"/> — the decorator that wraps <see cref="IGeminiClient"/>
/// in the DI container — so this adapter does not write audit entries directly.
/// </para>
/// </summary>
public sealed class GeminiCodingAdapter
{
    private const string LockedSchemaVersion = "1.0";

    private readonly IGeminiClient _geminiClient;
    private readonly string _promptTemplate;
    private readonly ILogger<GeminiCodingAdapter> _logger;

    public GeminiCodingAdapter(IGeminiClient geminiClient, ILogger<GeminiCodingAdapter> logger)
    {
        _geminiClient   = geminiClient;
        _logger         = logger;
        _promptTemplate = LoadPromptTemplate();
        PromptHash      = ComputePromptHash(_promptTemplate);
    }

    /// <summary>
    /// SHA-256 hex digest of the loaded prompt template (AIR-006 traceability).
    /// Populated once at construction; stored on each <see cref="MedicalCodeSuggestion"/> record.
    /// </summary>
    public string PromptHash { get; }

    /// <summary>
    /// Invokes Gemini with <paramref name="decryptedClinicalJson"/> and returns validated
    /// <see cref="CodeSuggestionResult"/> containing ranked ICD-10 and CPT candidates.
    /// </summary>
    /// <exception cref="SchemaVersionMismatchException">
    /// Thrown when the response <c>schemaVersion</c> does not match <see cref="LockedSchemaVersion"/>.
    /// Treated as non-retriable by <see cref="CodeSuggestionJob"/>.
    /// </exception>
    public async Task<CodeSuggestionResult> SuggestCodesAsync(
        string decryptedClinicalJson,
        CancellationToken ct = default)
    {
        // Fence clinical JSON to prevent prompt injection (OWASP A03).
        var fenced = $"<clinical_data>{decryptedClinicalJson}</clinical_data>";
        var prompt = _promptTemplate.Replace("{clinicalData}", fenced, StringComparison.Ordinal);

        _logger.LogInformation(
            "Invoking Gemini for code suggestion. PromptLength={Len}.", prompt.Length);

        var result = await _geminiClient.InvokeStructuredAsync<CodeSuggestionResult>(
            prompt, cancellationToken: ct);

        if (!string.Equals(result.SchemaVersion, LockedSchemaVersion, StringComparison.Ordinal))
            throw new SchemaVersionMismatchException(result.SchemaVersion, LockedSchemaVersion);

        return result;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string ComputePromptHash(string promptTemplate)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(promptTemplate));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string LoadPromptTemplate()
    {
        var assembly = typeof(GeminiCodingAdapter).Assembly;
        const string ResourceName = "UPACIP.Infrastructure.AI.CodeSuggestionPrompt.json";

        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{ResourceName}' not found in UPACIP.Infrastructure assembly. " +
                "Ensure the file Build Action is set to 'Embedded Resource'.");

        var resource = JsonSerializer.Deserialize<CodeSuggestionPromptResource>(
            stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("CodeSuggestionPrompt.json deserialized to null.");

        return resource.PromptTemplate;
    }

    private sealed record CodeSuggestionPromptResource(
        [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
        [property: JsonPropertyName("promptTemplate")] string PromptTemplate);
}

// ── Public result types ────────────────────────────────────────────────────

/// <summary>
/// Structured JSON response from Gemini for code suggestion (US_029, AC-002, AIR-003).
/// </summary>
public sealed record CodeSuggestionResult
{
    [JsonPropertyName("suggestions")]
    public CodeSuggestionItem[] Suggestions { get; init; } = [];

    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; init; } = "1.0";
}

/// <summary>
/// A single AI-suggested ICD-10 or CPT code candidate (AC-002).
/// </summary>
public sealed record CodeSuggestionItem
{
    [JsonPropertyName("codeType")]
    public string CodeType { get; init; } = string.Empty;           // "ICD10" | "CPT"

    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("rank")]
    public int Rank { get; init; }

    [JsonPropertyName("confidenceScore")]
    public double ConfidenceScore { get; init; }

    [JsonPropertyName("derivedFromField")]
    public string DerivedFromField { get; init; } = string.Empty;
}
