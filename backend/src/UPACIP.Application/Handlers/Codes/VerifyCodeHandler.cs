namespace UPACIP.Application.Handlers.Codes;

// -- Domain exceptions ---------------------------------------------------------
// Kept in Application so controllers in the API layer can reference them without
// depending directly on Infrastructure (Clean Architecture layer boundaries).

/// <summary>Raised when a resource already exists (HTTP 409).</summary>
public sealed class ConflictException(string message) : Exception(message);

/// <summary>Raised when business rules reject the operation (HTTP 422).</summary>
public sealed class UnprocessableEntityException(string message) : Exception(message);
