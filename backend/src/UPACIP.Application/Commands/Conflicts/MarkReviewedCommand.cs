namespace UPACIP.Application.Commands.Conflicts;

/// <summary>
/// Command for <c>PATCH /api/v1/conflicts/{id}/mark-reviewed</c> (US_028, AC-003).
/// </summary>
/// <param name="ConflictId">ID of the DataConflict to mark as reviewed.</param>
/// <param name="ActorId">Staff user ID from JWT <c>sub</c> claim.</param>
public sealed record MarkReviewedCommand(
    Guid ConflictId,
    Guid ActorId);
