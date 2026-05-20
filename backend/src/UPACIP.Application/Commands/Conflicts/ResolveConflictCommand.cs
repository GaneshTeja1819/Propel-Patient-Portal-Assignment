namespace UPACIP.Application.Commands.Conflicts;

/// <summary>
/// Command for <c>PATCH /api/v1/conflicts/{id}/resolve</c> (US_028, AC-002).
/// </summary>
/// <param name="ConflictId">ID of the DataConflict to resolve.</param>
/// <param name="ActorId">Staff user ID from JWT <c>sub</c> claim.</param>
/// <param name="AuthoritativeValue">The value chosen as canonical.</param>
/// <param name="SourceDocumentId">Optional — ID of the document supplying the value.</param>
/// <param name="ResolutionNote">Optional free-text note for the audit trail.</param>
public sealed record ResolveConflictCommand(
    Guid ConflictId,
    Guid ActorId,
    string AuthoritativeValue,
    string? SourceDocumentId,
    string? ResolutionNote);
