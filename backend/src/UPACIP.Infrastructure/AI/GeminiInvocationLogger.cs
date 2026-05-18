using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using UPACIP.Application.Interfaces;

namespace UPACIP.Infrastructure.AI;

/// <summary>
/// Decorator around <see cref="GeminiClient"/> that implements
/// <see cref="IGeminiClient"/> and writes an <c>AI_INVOCATION</c> audit log
/// entry after every call — success or failure (AC-004).
///
/// Metadata captured per call:
/// <list type="bullet">
///   <item><c>modelVersion</c> — always <c>gemini-1.5-pro</c></item>
///   <item><c>promptHash</c> — SHA-256 of the prompt string (hex, lower-case)</item>
///   <item><c>inputTokenCount</c> / <c>outputTokenCount</c> — from SDK UsageMetadata</item>
///   <item><c>responseLatencyMs</c> — wall-clock duration of the SDK call</item>
///   <item><c>httpStatusCode</c> — 200 on success, 500 on unhandled exception</item>
///   <item><c>timestamp</c> — UTC moment of invocation</item>
/// </list>
///
/// Because this is a singleton and <see cref="IAuditLogService"/> is scoped,
/// a fresh DI scope is created for each audit write via
/// <see cref="IServiceScopeFactory"/> (standard ASP.NET Core pattern).
/// </summary>
internal sealed class GeminiInvocationLogger : IGeminiClient
{
    private const string ModelVersion = "gemini-1.5-pro";

    private readonly GeminiClient _inner;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GeminiInvocationLogger> _logger;

    internal GeminiInvocationLogger(
        GeminiClient inner,
        IServiceScopeFactory scopeFactory,
        ILogger<GeminiInvocationLogger> logger)
    {
        _inner = inner;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> InvokeStructuredAsync<TResponse>(
        string prompt,
        object? jsonSchema = null,
        CancellationToken cancellationToken = default)
        where TResponse : class
    {
        var promptHash = ComputePromptHash(prompt);
        var sw = Stopwatch.StartNew();
        int inputTokens = 0, outputTokens = 0, httpStatus = 200;

        try
        {
            var (result, it, ot) = await _inner.InvokeStructuredCoreAsync<TResponse>(prompt, cancellationToken);
            inputTokens  = it;
            outputTokens = ot;
            return result;
        }
        catch (Exception ex)
        {
            httpStatus = 500;
            _logger.LogError(ex,
                "Gemini invocation failed. PromptHash={PromptHash}.", promptHash);
            throw;
        }
        finally
        {
            sw.Stop();
            await WriteAuditEntryAsync(
                promptHash, inputTokens, outputTokens, httpStatus, sw.ElapsedMilliseconds);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task WriteAuditEntryAsync(
        string promptHash, int inputTokens, int outputTokens, int httpStatus, long latencyMs)
    {
        try
        {
            var metadata = JsonSerializer.Serialize(new
            {
                modelVersion    = ModelVersion,
                promptHash,
                inputTokenCount  = inputTokens,
                outputTokenCount = outputTokens,
                responseLatencyMs = latencyMs,
                httpStatusCode   = httpStatus,
                timestamp        = DateTime.UtcNow,
            });

            await using var scope = _scopeFactory.CreateAsyncScope();
            var auditService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();

            await auditService.LogAsync(
                actorId:      Guid.Empty,
                actorRole:    "system",
                actionType:   "AI_INVOCATION",
                targetEntity: "GeminiModel",
                targetId:     Guid.Empty,
                metadata:     metadata);
        }
        catch (Exception ex)
        {
            // Audit write failures must never abort the main AI operation.
            _logger.LogWarning(ex, "AI_INVOCATION audit log write failed.");
        }
    }

    private static string ComputePromptHash(string prompt)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(prompt));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
