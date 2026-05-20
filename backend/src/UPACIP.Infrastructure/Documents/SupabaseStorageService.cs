using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using UPACIP.Application.Interfaces;

namespace UPACIP.Infrastructure.Documents;

/// <summary>
/// Uploads clinical PDF documents to Supabase Storage via the REST API
/// (<c>POST /storage/v1/object/{bucket}/{path}</c>).
///
/// Configuration — all loaded from environment variables at construction time
/// so the application fails fast on misconfiguration (OWASP A02):
/// <list type="bullet">
///   <item><c>SUPABASE_URL</c> — Base URL, e.g. <c>https://&lt;project&gt;.supabase.co</c>.</item>
///   <item><c>SUPABASE_SERVICE_KEY</c> — Service-role JWT; never exposed to clients.</item>
///   <item><c>SUPABASE_STORAGE_BUCKET</c> — Target bucket name (default: <c>clinical-documents</c>).</item>
/// </list>
///
/// Throws <see cref="StorageLimitExceededException"/> on HTTP 413 or 507 from
/// the storage backend so the controller can return HTTP 507 to the frontend.
/// </summary>
public sealed class SupabaseStorageService : IDocumentStorageService
{
    private readonly HttpClient _httpClient;
    private readonly string _bucket;
    private readonly ILogger<SupabaseStorageService> _logger;

    /// <summary>
    /// Validates required environment variables at construction time.
    /// Throws <see cref="InvalidOperationException"/> if any required variable is absent,
    /// surfacing the misconfiguration at application startup.
    /// </summary>
    public SupabaseStorageService(
        HttpClient httpClient,
        ILogger<SupabaseStorageService> logger)
    {
        var baseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL")
            ?? throw new InvalidOperationException(
                "SUPABASE_URL environment variable is not set. " +
                "Set it to your Supabase project URL (e.g. https://<project>.supabase.co).");

        var serviceKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_KEY")
            ?? throw new InvalidOperationException(
                "SUPABASE_SERVICE_KEY environment variable is not set. " +
                "Set it to the Supabase service-role JWT. " +
                "Never commit this value to source control (OWASP A02).");

        _bucket = Environment.GetEnvironmentVariable("SUPABASE_STORAGE_BUCKET")
            ?? "clinical-documents";

        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", serviceKey);

        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> UploadAsync(
        Guid patientId,
        string fileName,
        byte[] bytes,
        CancellationToken ct = default)
    {
        // Storage path keeps documents namespaced per patient so bucket policies
        // can enforce row-level access in a future iteration.
        var storagePath = $"patients/{patientId}/{fileName}";

        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        var url = $"storage/v1/object/{_bucket}/{storagePath}";

        _logger.LogDebug(
            "Uploading document for patient {PatientId} to {StoragePath}.",
            patientId,
            storagePath);

        var response = await _httpClient.PostAsync(url, content, ct);

        if ((int)response.StatusCode is 413 or 507)
        {
            _logger.LogWarning(
                "Supabase Storage returned HTTP {StatusCode} for patient {PatientId}. " +
                "Storage quota may be exhausted.",
                (int)response.StatusCode,
                patientId);
            throw new StorageLimitExceededException();
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Supabase Storage upload failed with HTTP {StatusCode} for patient {PatientId}. " +
                "Response: {Body}",
                (int)response.StatusCode,
                patientId,
                body);
            throw new InvalidOperationException(
                $"Storage upload failed with HTTP {(int)response.StatusCode}.");
        }

        return storagePath;
    }

    /// <inheritdoc />
    public async Task<byte[]> DownloadAsync(string storagePath, CancellationToken ct = default)
    {
        var url = $"storage/v1/object/{_bucket}/{storagePath}";

        _logger.LogDebug(
            "Downloading document from {StoragePath}.", storagePath);

        var response = await _httpClient.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Supabase Storage download failed with HTTP {StatusCode} for path {StoragePath}. "
                + "Response: {Body}",
                (int)response.StatusCode,
                storagePath,
                body);
            throw new InvalidOperationException(
                $"Storage download failed with HTTP {(int)response.StatusCode} for path '{storagePath}'.");
        }

        return await response.Content.ReadAsByteArrayAsync(ct);
    }
}
