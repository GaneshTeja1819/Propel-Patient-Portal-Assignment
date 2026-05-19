using UPACIP.Application.Commands.Admin;
using UPACIP.Application.Exceptions;
using UPACIP.Application.Interfaces;

namespace UPACIP.Application.Handlers.Admin;

/// <summary>
/// Updates an existing user's profile fields (first/last name, email, phone).
/// Enforces unique-email constraint and writes a USER_UPDATED audit entry.
/// </summary>
public sealed class UpdateUserHandler
{
    private readonly IAdminUserStore _store;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public UpdateUserHandler(
        IAdminUserStore store,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService)
    {
        _store = store;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
    }

    public async Task HandleAsync(UpdateUserCommand command, CancellationToken ct = default)
    {
        var user = await _store.GetByIdAsync(command.TargetUserId, ct)
            ?? throw new NotFoundException($"User {command.TargetUserId} not found.");

        if (!string.IsNullOrWhiteSpace(command.Email))
        {
            var normalized = command.Email.Trim().ToLowerInvariant();
            if (!string.Equals(user.Email, normalized, StringComparison.Ordinal))
            {
                if (await _store.EmailExistsAsync(normalized, command.TargetUserId, ct))
                    throw new ConflictException("Email address is already in use.");

                user.Email = normalized;
            }
        }

        if (!string.IsNullOrWhiteSpace(command.FirstName))
            user.FirstName = command.FirstName.Trim();

        if (!string.IsNullOrWhiteSpace(command.LastName))
            user.LastName = command.LastName.Trim();

        // PhoneNumber: null = leave unchanged; empty string = clear field.
        if (command.PhoneNumber is not null)
            user.PhoneNumber = string.IsNullOrWhiteSpace(command.PhoneNumber)
                ? null
                : command.PhoneNumber.Trim();

        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(
            command.ActorId, "Admin", "USER_UPDATED", "User", command.TargetUserId,
            cancellationToken: ct);
    }
}
