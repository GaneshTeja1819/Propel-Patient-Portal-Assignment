namespace UPACIP.Application.Exceptions;

/// <summary>
/// Represents field-level validation failures.
/// </summary>
public sealed class ValidationException : Exception
{
    public ValidationException(IDictionary<string, string[]> errors)
        : base("Validation failed.")
    {
        Errors = errors;
    }

    public IDictionary<string, string[]> Errors { get; }
}
