using UPACIP.Application.Exceptions;
using UPACIP.Application.Interfaces;

namespace UPACIP.Application.Handlers.Admin;

/// <summary>
/// Deactivates or reactivates a user account (AC-001, AC-003).
/// Deactivation: guards against self-deactivation (HTTP 422), sets IsActive = false,
/// invalidates all Redis sessions, and writes a USER_DEACTIVATED audit entry.
/// Reactivation: sets IsActive = true and writes a USER_REACTIVATED audit entry.
/// </summary>
public sealed class DeactivateUserHandler
{
    private readonly IAdminUserStore _store;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthService _authService;
    private readonly IAuditLogService _auditLogService;

    public DeactivateUserHandler(
        IAdminUserStore store,
        IUnitOfWork unitOfWork,
        IAuthService authService,
        IAuditLogService auditLogService)
    {
        _store = store;
        _unitOfWork = unitOfWork;
        _authService = authService;
        _auditLogService = auditLogService;
    }

    public async Task DeactivateAsync(Guid actorId, Guid targetUserId, CancellationToken ct = default)
    {
        if (targetUserId == actorId)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["userId"] = ["You cannot deactivate your own account."]
            });

        var user = await _store.GetByIdAsync(targetUserId, ct)
            ?? throw new NotFoundException($"User {targetUserId} not found.");

        if (!user.IsActive)
            return; // Idempotent; already inactive — no duplicate audit entry.

        user.IsActive = false;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);

        await _authService.InvalidateAllSessionsForUserAsync(targetUserId, ct);

        await _auditLogService.LogAsync(
            actorId, "Admin", "USER_DEACTIVATED", "User", targetUserId,
            cancellationToken: ct);
    }

    public async Task ReactivateAsync(Guid actorId, Guid targetUserId, CancellationToken ct = default)
    {
        var user = await _store.GetByIdAsync(targetUserId, ct)
            ?? throw new NotFoundException($"User {targetUserId} not found.");

        if (user.IsActive)
            return; // Idempotent; already active — no duplicate audit entry.

        user.IsActive = true;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(
            actorId, "Admin", "USER_REACTIVATED", "User", targetUserId,
            cancellationToken: ct);
    }
}
