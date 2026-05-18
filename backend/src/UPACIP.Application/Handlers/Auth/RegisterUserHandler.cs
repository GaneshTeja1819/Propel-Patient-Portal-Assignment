using Npgsql;
using System.Globalization;
using UPACIP.Application.Commands.Auth;
using UPACIP.Application.Exceptions;
using UPACIP.Application.Interfaces;
using UPACIP.Domain.Entities;

namespace UPACIP.Application.Handlers.Auth;

/// <summary>
/// Handles patient self-registration with transactional write semantics.
/// </summary>
public sealed class RegisterUserHandler
{
    private const int PasswordWorkFactor = 12;

    private readonly IRegistrationStore _registrationStore;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterUserHandler(
        IRegistrationStore registrationStore,
        IUnitOfWork unitOfWork)
    {
        _registrationStore = registrationStore;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> HandleAsync(RegisterUserCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmailOrThrow(command.Email);
        ValidateNameOrThrow("firstName", command.FirstName);
        ValidateNameOrThrow("lastName", command.LastName);
        ValidatePasswordOrThrow(command.Password);

        if (await _registrationStore.EmailExistsAsync(normalizedEmail, cancellationToken))
            throw new ConflictException("Email address already in use");

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(command.Password, workFactor: PasswordWorkFactor),
                Role = "Patient",
                FirstName = command.FirstName.Trim(),
                LastName = command.LastName.Trim(),
                IsEmailVerified = false,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            await _registrationStore.AddUserAsync(user, cancellationToken);

            await _registrationStore.AddAuditLogAsync(new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorId = user.Id,
                ActorEmail = normalizedEmail,
                Action = "USER_REGISTERED",
                EntityType = "User",
                EntityId = user.Id,
                CreatedAt = DateTimeOffset.UtcNow,
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return user.Id;
        }
        catch (Exception ex) when (IsUniqueEmailViolation(ex))
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ConflictException("Email address already in use");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static void ValidateNameOrThrow(string field, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [field] = [field == "firstName" ? "First name is required." : "Last name is required."]
            });
    }

    private static void ValidatePasswordOrThrow(string password)
    {
        var rule = GetFirstFailingPasswordRule(password);
        if (rule is null)
            return;

        throw new ValidationException(new Dictionary<string, string[]>
        {
            ["password"] = [rule]
        });
    }

    private static string? GetFirstFailingPasswordRule(string password)
    {
        if (password.Length < 8)
            return "Must be at least 8 characters long.";
        if (!password.Any(char.IsUpper))
            return "Must contain at least one uppercase letter.";
        if (!password.Any(char.IsLower))
            return "Must contain at least one lowercase letter.";
        if (!password.Any(char.IsDigit))
            return "Must contain at least one digit.";

        return null;
    }

    private static string NormalizeEmailOrThrow(string rawEmail)
    {
        if (string.IsNullOrWhiteSpace(rawEmail))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["email"] = ["Email address is required."]
            });

        var email = rawEmail.Trim();
        var atIndex = email.LastIndexOf('@');

        if (atIndex <= 0 || atIndex >= email.Length - 1)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["email"] = ["Please enter a valid email address."]
            });

        var localPart = email[..atIndex];
        var domainPart = email[(atIndex + 1)..];

        if (localPart.Any(ch => ch > 127))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["email"] = ["Email address must be ASCII-compatible."]
            });

        string asciiDomain;
        try
        {
            asciiDomain = new IdnMapping().GetAscii(domainPart);
        }
        catch (ArgumentException)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["email"] = ["Email address normalisation failed."]
            });
        }

        return $"{localPart}@{asciiDomain}".ToLowerInvariant();
    }

    private static bool IsUniqueEmailViolation(Exception ex)
    {
        var current = ex;
        while (current is not null)
        {
            if (current is PostgresException pgEx && pgEx.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                return string.Equals(pgEx.ConstraintName, "IX_users_Email", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(pgEx.ConstraintName, "users_email_key", StringComparison.OrdinalIgnoreCase)
                    || (pgEx.Detail?.Contains("(Email)") ?? false);
            }

            current = current.InnerException!;
        }

        return false;
    }
}
