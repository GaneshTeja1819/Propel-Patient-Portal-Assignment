namespace UPACIP.Application.Interfaces;

/// <summary>
/// Invokes the Google Gemini model with structured JSON output.
/// All calls are automatically decorated with AI_INVOCATION audit log entries
/// (AC-004). Callers receive the deserialised <typeparamref name="TResponse"/>
/// directly — no regex parsing.
/// </summary>
public interface IGeminiClient
{
    /// <summary>
    /// Sends <paramref name="prompt"/> to <c>gemini-1.5-pro</c> and deserialises
    /// the structured JSON response into <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TResponse">
    /// Target C# type. The SDK infers the JSON schema from this type and sets
    /// <c>response_mime_type = "application/json"</c> automatically.
    /// </typeparam>
    /// <param name="prompt">Natural-language instruction sent to the model.</param>
    /// <param name="jsonSchema">
    /// Optional caller-supplied JSON schema hint. Implementations may ignore this
    /// when the SDK derives the schema from <typeparamref name="TResponse"/>.
    /// </param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    /// <returns>Deserialised response; never <see langword="null"/>.</returns>
    Task<TResponse> InvokeStructuredAsync<TResponse>(
        string prompt,
        object? jsonSchema = null,
        CancellationToken cancellationToken = default)
        where TResponse : class;
}
