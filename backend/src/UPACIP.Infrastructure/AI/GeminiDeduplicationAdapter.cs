using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using UPACIP.Application.Interfaces;

namespace UPACIP.Infrastructure.AI;

/// <summary>
/// Adapts the generic <see cref="IGeminiClient"/> to the AI clinical data de-duplication
/// workflow (US_027, AC-003, AIR-004).
///
/// <para>
/// Builds the structured de-duplication prompt from the embedded
/// <c>DeduplicationPrompt.json</c> resource, fences input data to prevent prompt
/// injection (OWASP A03), calls the model, and validates the response schema version
/// before returning.
/// </para>
///
/// <para>
/// <c>AI_INVOCATION</c> audit entries are written automatically by
/// <see cref="GeminiInvocationLogger"/> — the decorator wrapping <see cref="IGeminiClient"/>
/// in the DI container — so this adapter does not need additional audit writes.
/// </para>
/// </summary>
internal sealed class GeminiDeduplicationAdapter
{
    private const string LockedSchemaVersion = "1.0";

    private readonly IGeminiClient _geminiClient;
    private readonly string _promptTemplate;
    private readonly ILogger<GeminiDeduplicationAdapter> _logger;

    public GeminiDeduplicationAdapter(
        IGeminiClient geminiClient,
        ILogger<GeminiDeduplicationAdapter> logger)
    {
        _geminiClient  = geminiClient;
        _logger        = logger;
        _promptTemplate = LoadPromptTemplate();
    }

    /// <summary>
    /// Sends the flattened <paramref name="clinicalItems"/> to Gemini with the versioned
    /// de-duplication prompt and returns the validated <see cref="DeduplicationResult"/>.
    /// </summary>
    /// <exception cref="SchemaVersionMismatchException">
    /// Thrown when the response <c>schemaVersion</c> does not match
    /// <see cref="LockedSchemaVersion"/>. Treated as non-retriable by
    /// <see cref="DeduplicationJob"/> (edge case: schema evolution).
    /// </exception>
    public async Task<DeduplicationResult> CallDeduplicationAsync(
        IReadOnlyList<ClinicalInputItem> clinicalItems,
        CancellationToken ct = default)
    {
        var dataJson = JsonSerializer.Serialize(clinicalItems, JsonOpts);

        // Fence clinical data to prevent prompt injection (OWASP A03, AIR-004).
        var fencedData = $"<clinical_data>{dataJson}</clinical_data>";
        var prompt     = _promptTemplate.Replace("{clinicalDataJson}", fencedData, StringComparison.Ordinal);

        _logger.LogInformation(
            "Invoking Gemini for de-duplication. ItemCount={Count}, PromptLength={Len}.",
            clinicalItems.Count, prompt.Length);

        var result = await _geminiClient.InvokeStructuredAsync<DeduplicationResult>(
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
        var assembly     = typeof(GeminiDeduplicationAdapter).Assembly;
        const string ResourceName = "UPACIP.Infrastructure.AI.DeduplicationPrompt.json";

        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{ResourceName}' not found in UPACIP.Infrastructure assembly. " +
                "Ensure the file Build Action is set to 'Embedded Resource'.");

        var resource = JsonSerializer.Deserialize<DeduplicationPromptResource>(
            stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("DeduplicationPrompt.json deserialized to null.");

        return resource.PromptTemplate;
    }

    // ── Internal options ──────────────────────────────────────────────────

    private static readonly JsonSerializerOptions JsonOpts =
        new(JsonSerializerDefaults.Web);

    // ── Embedded resource DTO ─────────────────────────────────────────────

    private sealed record DeduplicationPromptResource(
        [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
        [property: JsonPropertyName("promptTemplate")] string PromptTemplate);
}

// ── Input / output types ───────────────────────────────────────────────────

/// <summary>A single extracted clinical fact item sent as input to the dedup prompt.</summary>
public sealed record ClinicalInputItem
{
    [JsonPropertyName("documentId")]  public string  DocumentId  { get; init; } = string.Empty;
    [JsonPropertyName("sectionType")] public string  SectionType { get; init; } = string.Empty;
    [JsonPropertyName("label")]       public string  Label       { get; init; } = string.Empty;
    [JsonPropertyName("value")]       public string  Value       { get; init; } = string.Empty;
}

/// <summary>Structured JSON response from Gemini for de-duplication (AIR-004).</summary>
public sealed record DeduplicationResult
{
    [JsonPropertyName("schemaVersion")] public string SchemaVersion { get; init; } = "1.0";
    [JsonPropertyName("sections")]      public DeduplicationSections Sections { get; init; } = new();
}

public sealed record DeduplicationSections
{
    [JsonPropertyName("vitals")]       public CanonicalEntry[] Vitals       { get; init; } = [];
    [JsonPropertyName("medications")]  public CanonicalEntry[] Medications  { get; init; } = [];
    [JsonPropertyName("diagnoses")]    public CanonicalEntry[] Diagnoses    { get; init; } = [];
    [JsonPropertyName("visitHistory")] public CanonicalEntry[] VisitHistory { get; init; } = [];
}

public sealed record CanonicalEntry
{
    [JsonPropertyName("label")]             public string   Label             { get; init; } = string.Empty;
    [JsonPropertyName("canonicalValue")]    public string   CanonicalValue    { get; init; } = string.Empty;
    [JsonPropertyName("sourceDocumentIds")] public string[] SourceDocumentIds { get; init; } = [];
    [JsonPropertyName("confidence")]        public double   Confidence        { get; init; }
    [JsonPropertyName("isPhiField")]        public bool     IsPhiField        { get; init; } = true;
}
