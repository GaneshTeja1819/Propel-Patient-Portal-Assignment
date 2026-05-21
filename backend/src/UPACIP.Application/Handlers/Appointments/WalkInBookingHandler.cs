using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using UPACIP.Application.Commands.Appointments;
using UPACIP.Application.Exceptions;
using UPACIP.Application.Interfaces;
using UPACIP.Domain.Entities;

namespace UPACIP.Application.Handlers.Appointments;

/// <summary>
/// Creates a walk-in appointment for an existing or anonymous patient.
/// Uses EF Core xmin row-version concurrency on the slot to prevent double-booking.
/// Audit entry is written with <c>actorRole="Staff"</c> and no patient attribution (AC-004).
/// </summary>
public sealed class WalkInBookingHandler
{
    private readonly IWalkInBookingStore _store;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ISlotCacheService _slotCacheService;

    public WalkInBookingHandler(
        IWalkInBookingStore store,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        ISlotCacheService slotCacheService)
    {
        _store = store;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _slotCacheService = slotCacheService;
    }

    public async Task<Guid> HandleAsync(WalkInBookingCommand command, CancellationToken cancellationToken = default)
    {
        // Exactly one of: existing patient ID or anonymous name must be present
        if (command.PatientId is null && string.IsNullOrWhiteSpace(command.AnonName))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["patient"] = ["Either a patient ID or an anonymous patient name must be provided."]
            });

        var slot = await _store.GetSlotAsync(command.SlotId, cancellationToken)
            ?? throw new ValidationException(new Dictionary<string, string[]>
            {
                ["slotId"] = ["Appointment slot not found."]
            });

        if (!slot.IsAvailable)
            throw new ConflictException("Appointment slot is no longer available.");

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            slot.IsAvailable = false;

            string? anonDetails = command.PatientId is null
                ? JsonSerializer.Serialize(new
                {
                    name = command.AnonName,
                    dob = command.AnonDob?.ToString("yyyy-MM-dd"),
                    phone = command.AnonPhone
                })
                : null;

            var appointment = new Appointment
            {
                PatientId = command.PatientId,
                ProviderId = slot.ProviderId,
                SlotId = command.SlotId,
                Status = "Scheduled",
                CreatedByStaffId = command.StaffActorId,
                AnonymousPatientDetails = anonDetails,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            await _store.AddAppointmentAsync(appointment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Invalidate cached slot availability so subsequent readers see it as booked
            await _slotCacheService.InvalidateSlotAsync(command.SlotId.ToString(), cancellationToken);

            await _auditLogService.LogAsync(
                actorId: command.StaffActorId,
                actorRole: "Staff",
                actionType: "WALKIN_APPOINTMENT_CREATED",
                targetEntity: "Appointment",
                targetId: appointment.Id,
                metadata: command.PatientId is null
                    ? "anonymous=true"
                    : $"patientId={command.PatientId}",
                cancellationToken: cancellationToken);

            return appointment.Id;
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ConflictException("Appointment slot is no longer available.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
