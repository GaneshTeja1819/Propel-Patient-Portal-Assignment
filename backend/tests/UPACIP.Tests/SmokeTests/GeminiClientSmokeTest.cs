using Microsoft.Extensions.Logging.Abstractions;
using UPACIP.Infrastructure.AI;

namespace UPACIP.Tests.SmokeTests;

/// <summary>
/// Integration smoke test confirming that <c>GeminiClient</c> can invoke
/// <c>gemini-1.5-pro</c> with structured output and deserialise the response
/// to a target C# type without regex parsing (AC-003).
///
/// Requires <c>GEMINI_API_KEY</c> environment variable. The test skips
/// gracefully when the variable is absent so that local builds without
/// API credentials do not fail.
/// </summary>
public sealed class GeminiClientSmokeTest
{
    [Fact]
    public async Task InvokeStructuredCoreAsync_WithValidApiKey_DeserializesToTargetType()
    {
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            // No API key present in this environment — skip without marking as failed.
            return;
        }

        var client = new GeminiClient(NullLogger<GeminiClient>.Instance);

        var (result, inputTokens, outputTokens) = await client.InvokeStructuredCoreAsync<AnswerResponse>(
            prompt: "Respond with a JSON object. Set the 'answer' field to the string 'ok'.",
            cancellationToken: default);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Answer),
            "Expected a non-empty 'answer' field in the structured response.");
        Assert.True(inputTokens > 0,
            "Expected a positive input token count from the Gemini API.");
        Assert.True(outputTokens >= 0,
            "Expected a non-negative output token count from the Gemini API.");
    }

    /// <summary>Minimal target type for the structured-output smoke test.</summary>
    private sealed record AnswerResponse(string Answer);
}
