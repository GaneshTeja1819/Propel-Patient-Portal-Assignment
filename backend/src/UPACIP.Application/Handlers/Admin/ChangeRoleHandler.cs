using System.Text.Json;
using UPACIP.Application.Exceptions;
using UPACIP.Application.Interfaces;

namespace UPACIP.Application.Handlers.Admin;

/// <summary>
/// Changes the role of a target user (AC-002, AC-005).
/// After persisting: invalidates all active Redis sessions for the target user
/// so the new role takes effect on next login, then writes a ROLE_CHANGED audit entry.
/// </summary>
public sealed class ChangeRoleHandler
{
    private static readonly HashSet<string> ValidRoles =
        new(StringComparer.Ordinal) { "Patient", "Staff", "Admin" };

    private readonly IAdminUserStore _store;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthService _authService;
    private readonly IAuditLogService _auditLogService;

    public ChangeRoleHandler(
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

    public async Task HandleAsync(
        Guid actorId,
        Guid targetUserId,
        string newRole,
        CancellationToken ct = default)
    {
        if (!ValidRoles.Contains(newRole))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["role"] = [$"Role must be one of: {string.Join(", ", ValidRoles)}."]
            });

        var user = await _store.GetByIdAsync(targetUserId, ct)
            ?? throw new NotFoundException($"User {targetUserId} not found.");

        var oldRole = user.Role;

        if (string.Equals(oldRole, newRole, StringComparison.Ordinal))
            return; // No-op; no audit entry for identity change.

        user.Role = newRole;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);

        // Force re-login so the new role claim is reflected immediately.
        await _authService.InvalidateAllSessionsForUserAsync(targetUserId, ct);

        var metadata = JsonSerializer.Serialize(new { oldRole, newRole });
        await _auditLogService.LogAsync(
            actorId, "Admin", "ROLE_CHANGED", "User", targetUserId, metadata, ct);
    }
}
