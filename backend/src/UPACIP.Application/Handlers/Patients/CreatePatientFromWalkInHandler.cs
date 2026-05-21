using Npgsql;
using System.Globalization;
using UPACIP.Application.Commands.Patients;
using UPACIP.Application.Exceptions;
using UPACIP.Application.Interfaces;
using UPACIP.Domain.Entities;

namespace UPACIP.Application.Handlers.Patients;

/// <summary>
/// Creates a Patient <c>User</c> account and links it to an existing anonymous
/// walk-in appointment. Writes two audit entries, both with <c>actorRole="Staff"</c>
/// and no patient attribution (AC-003, AC-004).
/// </summary>
public sealed class CreatePatientFromWalkInHandler
{
    private const int PasswordWorkFactor = 12;

    private readonly IRegistrationStore _registrationStore;
    private readonly IWalkInBookingStore _walkInStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public CreatePatientFromWalkInHandler(
        IRegistrationStore registrationStore,
        IWalkInBookingStore walkInStore,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService)
    {
        _registrationStore = registrationStore;
        _walkInStore = walkInStore;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
    }

    public async Task<(Guid PatientId, Guid AppointmentId)> HandleAsync(
        CreatePatientFromWalkInCommand command,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmailOrThrow(command.Email);

        if (await _registrationStore.EmailExistsAsync(normalizedEmail, cancellationToken))
            throw new ConflictException("Email already registered.");

        var appointment = await _walkInStore.GetAppointmentAsync(command.AppointmentId, cancellationToken)
            ?? throw new ValidationException(new Dictionary<string, string[]>
            {
                ["appointmentId"] = ["Appointment not found."]
            });

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var user = new User
            {
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

            appointment.PatientId = user.Id;
            appointment.AnonymousPatientDetails = null;
            appointment.UpdatedAt = DateTimeOffset.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await _auditLogService.LogAsync(
                actorId: command.StaffActorId,
                actorRole: "Staff",
                actionType: "PATIENT_ACCOUNT_CREATED",
                targetEntity: "User",
                targetId: user.Id,
                metadata: $"createdForAppointmentId={command.AppointmentId}",
                cancellationToken: cancellationToken);

            await _auditLogService.LogAsync(
                actorId: command.StaffActorId,
                actorRole: "Staff",
                actionType: "WALKIN_APPOINTMENT_LINKED",
                targetEntity: "Appointment",
                targetId: command.AppointmentId,
                metadata: $"linkedPatientId={user.Id}",
                cancellationToken: cancellationToken);

            return (user.Id, command.AppointmentId);
        }
        catch (Exception ex) when (IsUniqueEmailViolation(ex))
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ConflictException("Email already registered.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
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
