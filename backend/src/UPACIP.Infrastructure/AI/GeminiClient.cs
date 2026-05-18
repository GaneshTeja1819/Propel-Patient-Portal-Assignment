using GenerativeAI;
using GenerativeAI.Types;
using Microsoft.Extensions.Logging;

namespace UPACIP.Infrastructure.AI;

/// <summary>
/// Core Gemini SDK wrapper targeting <c>gemini-1.5-pro</c> with structured
/// JSON output mode. Internal — consumed only by <see cref="GeminiInvocationLogger"/>
/// which decorates it and exposes it as <c>IGeminiClient</c>.
///
/// Construction reads <c>GEMINI_API_KEY</c> from environment variables and throws
/// <see cref="InvalidOperationException"/> immediately if the key is absent so the
/// process exits at startup rather than at first request.
/// </summary>
internal sealed class GeminiClient
{
    private const string ModelId = "gemini-1.5-pro";

    private readonly GenerativeModel _model;
    private readonly ILogger<GeminiClient> _logger;

    internal GeminiClient(ILogger<GeminiClient> logger)
    {
        _logger = logger;

        var apiKey = System.Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? throw new InvalidOperationException(
                "GEMINI_API_KEY environment variable is not set. " +
                "The application cannot start without a Gemini API key.");

        // ResponseMimeType = "application/json" enables structured output mode
        // so the model always returns parseable JSON (AC-003).
        var config = new GenerationConfig { ResponseMimeType = "application/json" };
        _model = new GenerativeModel(apiKey, ModelId, config, null, null, null, null);
    }

    /// <summary>
    /// Invokes the model and returns the deserialised response together with
    /// token usage so the decorator can write the AI_INVOCATION audit entry.
    /// </summary>
    internal async Task<(TResponse Response, int InputTokens, int OutputTokens)>
        InvokeStructuredCoreAsync<TResponse>(string prompt, CancellationToken cancellationToken)
        where TResponse : class
    {
        var response = await _model.GenerateContentAsync(prompt, cancellationToken);

        // ToObject<T>() is a GenerateContentResponseExtensions extension method
        // that parses the JSON body into the target type — no regex (AC-003).
        var result = response.ToObject<TResponse>()
            ?? throw new InvalidOperationException(
                "Gemini returned null or failed to deserialise the structured response.");

        int inputTokens  = (int)(response.UsageMetadata?.PromptTokenCount     ?? 0);
        int outputTokens = (int)(response.UsageMetadata?.CandidatesTokenCount ?? 0);

        _logger.LogDebug(
            "Gemini invocation complete. Model={Model}, InputTokens={Input}, OutputTokens={Output}.",
            ModelId, inputTokens, outputTokens);

        return (result, inputTokens, outputTokens);
    }
}
