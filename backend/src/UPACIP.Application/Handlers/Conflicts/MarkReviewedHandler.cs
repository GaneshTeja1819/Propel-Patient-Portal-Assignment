using Microsoft.Extensions.Logging;
using UPACIP.Application.Commands.Conflicts;
using UPACIP.Application.Interfaces;

namespace UPACIP.Application.Handlers.Conflicts;

/// <summary>
/// Handles <c>PATCH /api/v1/conflicts/{id}/mark-reviewed</c> (US_028, AC-003).
/// Sets status to "ReviewedUnresolved" and writes a CONFLICT_REVIEWED audit entry.
/// </summary>
public sealed class MarkReviewedHandler
{
    private readonly IConflictRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IAuditLogService _audit;
    private readonly ILogger<MarkReviewedHandler> _logger;

    public MarkReviewedHandler(
        IConflictRepository repo,
        IUnitOfWork uow,
        IAuditLogService audit,
        ILogger<MarkReviewedHandler> logger)
    {
        _repo  = repo;
        _uow   = uow;
        _audit = audit;
        _logger = logger;
    }

    /// <summary>
    /// Executes the mark-reviewed flow.
    /// Throws <see cref="KeyNotFoundException"/> when the conflict does not exist.
    /// Throws <see cref="InvalidOperationException"/> when the conflict is already Resolved (cannot un-resolve).
    /// Returns without saving when the conflict is already ReviewedUnresolved (idempotent).
    /// </summary>
    public async Task HandleAsync(MarkReviewedCommand command, CancellationToken ct = default)
    {
        var conflict = await _repo.GetByIdAsync(command.ConflictId, ct)
            ?? throw new KeyNotFoundException($"Conflict {command.ConflictId} not found.");

        // Already in the target state — idempotent, nothing to do.
        if (conflict.Status == "ReviewedUnresolved")
            return;

        // Resolved conflicts cannot be reverted via mark-reviewed (AC-003 data integrity).
        if (conflict.Status == "Resolved")
            throw new InvalidOperationException("Cannot mark a resolved conflict as reviewed.");

        conflict.Status       = "ReviewedUnresolved";
        conflict.ResolvedById = command.ActorId;
        conflict.ResolvedAt   = DateTimeOffset.UtcNow;

        await _uow.SaveChangesAsync(ct);

        // FIX-003: mark entity dirty in the repository; second SaveChanges commits it.
        await _repo.TouchLastReviewedAtAsync(conflict.PatientId, ct);
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync(
            actorId:      command.ActorId,
            actorRole:    "Staff",
            actionType:   "CONFLICT_REVIEWED",
            targetEntity: "DataConflict",
            targetId:     command.ConflictId,
            cancellationToken: ct);

        _logger.LogInformation(
            "Conflict {ConflictId} marked reviewed by {ActorId}.",
            command.ConflictId, command.ActorId);
    }
}
