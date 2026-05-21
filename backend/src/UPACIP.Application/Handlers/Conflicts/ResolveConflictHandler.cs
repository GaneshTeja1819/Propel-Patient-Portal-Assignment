using Microsoft.Extensions.Logging;
using UPACIP.Application.Commands.Conflicts;
using UPACIP.Application.Interfaces;

namespace UPACIP.Application.Handlers.Conflicts;

/// <summary>
/// Handles <c>PATCH /api/v1/conflicts/{id}/resolve</c> (US_028, AC-002).
/// <list type="number">
///   <item>Load conflict; 404 if absent.</item>
///   <item>Reject if status != "Open" → throws <see cref="InvalidOperationException"/> (HTTP 409).</item>
///   <item>Update status, canonicalValue, resolvedById, resolvedAt.</item>
///   <item>SaveChanges — xmin row version guard; <see cref="DbUpdateConcurrencyException"/> → HTTP 409.</item>
///   <item>Touch patient's LastConflictReviewedAt.</item>
///   <item>Write immutable CONFLICT_RESOLVED audit entry.</item>
/// </list>
/// </summary>
public sealed class ResolveConflictHandler
{
    private readonly IConflictRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IAuditLogService _audit;
    private readonly ILogger<ResolveConflictHandler> _logger;

    public ResolveConflictHandler(
        IConflictRepository repo,
        IUnitOfWork uow,
        IAuditLogService audit,
        ILogger<ResolveConflictHandler> logger)
    {
        _repo  = repo;
        _uow   = uow;
        _audit = audit;
        _logger = logger;
    }

    /// <summary>
    /// Executes the resolve flow.
    /// Throws <see cref="KeyNotFoundException"/> when the conflict does not exist.
    /// Throws <see cref="InvalidOperationException"/> when the conflict is already resolved or reviewed.
    /// Throws <see cref="DbUpdateConcurrencyException"/> on concurrent resolution (caller maps → HTTP 409).
    /// </summary>
    public async Task HandleAsync(ResolveConflictCommand command, CancellationToken ct = default)
    {
        var conflict = await _repo.GetByIdAsync(command.ConflictId, ct)
            ?? throw new KeyNotFoundException($"Conflict {command.ConflictId} not found.");

        if (conflict.Status != "Open")
            throw new InvalidOperationException("Conflict already resolved or reviewed.");

        if (string.IsNullOrWhiteSpace(command.AuthoritativeValue))
            throw new ArgumentException("AuthoritativeValue must not be empty.", nameof(command));

        conflict.Status         = "Resolved";
        conflict.CanonicalValue = command.AuthoritativeValue.Trim();
        conflict.ResolvedById   = command.ActorId;
        conflict.ResolvedAt     = DateTimeOffset.UtcNow;

        // DbUpdateConcurrencyException from xmin guard propagates to caller (HTTP 409).
        await _uow.SaveChangesAsync(ct);

        // FIX-003: mark entity dirty; second SaveChanges commits LastConflictReviewedAt update.
        await _repo.TouchLastReviewedAtAsync(conflict.PatientId, ct);
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync(
            actorId:      command.ActorId,
            actorRole:    "Staff",
            actionType:   "CONFLICT_RESOLVED",
            targetEntity: "DataConflict",
            targetId:     command.ConflictId,
            metadata:     command.ResolutionNote,
            cancellationToken: ct);

        _logger.LogInformation(
            "Conflict {ConflictId} resolved by {ActorId}. Canonical: {Value}",
            command.ConflictId, command.ActorId, command.AuthoritativeValue);
    }
}
