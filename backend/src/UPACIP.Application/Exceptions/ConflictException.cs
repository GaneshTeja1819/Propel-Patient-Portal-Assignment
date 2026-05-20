namespace UPACIP.Application.Exceptions;

/// <summary>
/// Represents a conflict with current server state (e.g., duplicate email).
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }
}
