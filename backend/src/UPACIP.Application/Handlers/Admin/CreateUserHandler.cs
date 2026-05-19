using UPACIP.Application.Commands.Admin;
using UPACIP.Application.Exceptions;
using UPACIP.Application.Interfaces;
using UPACIP.Domain.Entities;

namespace UPACIP.Application.Handlers.Admin;

/// <summary>
/// Creates a new user account with the specified role.
/// Validates unique email, hashes password with BCrypt, and writes a USER_CREATED audit entry.
/// </summary>
public sealed class CreateUserHandler
{
    private static readonly HashSet<string> ValidRoles =
        new(StringComparer.Ordinal) { "Patient", "Staff", "Admin" };

    private const int PasswordWorkFactor = 12;

    private readonly IAdminUserStore _store;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public CreateUserHandler(
        IAdminUserStore store,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService)
    {
        _store = store;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
    }

    public async Task<Guid> HandleAsync(CreateUserCommand command, CancellationToken ct = default)
    {
        if (!ValidRoles.Contains(command.Role))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["role"] = [$"Role must be one of: {string.Join(", ", ValidRoles)}."]
            });

        var normalized = command.Email.Trim().ToLowerInvariant();

        if (await _store.EmailExistsAsync(normalized, excludeUserId: null, ct))
            throw new ConflictException("Email address is already in use.");

        var user = new User
        {
            Email = normalized,
            PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(command.Password, PasswordWorkFactor),
            Role = command.Role,
            FirstName = command.FirstName.Trim(),
            LastName = command.LastName.Trim(),
            IsActive = true,
            IsEmailVerified = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        await _store.AddAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(
            command.ActorId, "Admin", "USER_CREATED", "User", user.Id,
            cancellationToken: ct);

        return user.Id;
    }
}
