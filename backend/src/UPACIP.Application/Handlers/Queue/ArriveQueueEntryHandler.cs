using Microsoft.EntityFrameworkCore;
using UPACIP.Application.Commands.Appointments;
using UPACIP.Application.Exceptions;
using UPACIP.Application.Interfaces;

namespace UPACIP.Application.Handlers.Queue;

/// <summary>
/// Marks a queue appointment as "Arrived". Uses EF Core xmin row-version
/// optimistic concurrency to prevent double-marking (AC-004, edge case).
/// Returns HTTP 409 via <see cref="ConflictException"/> if already "Arrived".
/// </summary>
public sealed class ArriveQueueEntryHandler
{
    private readonly IQueueStore _store;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public ArriveQueueEntryHandler(
        IQueueStore store,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService)
    {
        _store = store;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
    }

    public async Task HandleAsync(ArriveQueueEntryCommand command, CancellationToken cancellationToken = default)
    {
        var appointment = await _store.GetAppointmentWithSlotAsync(command.AppointmentId, cancellationToken)
            ?? throw new ValidationException(new Dictionary<string, string[]>
            {
                ["appointmentId"] = ["Appointment not found."]
            });

        if (appointment.Status == "Arrived")
            throw new ConflictException("Patient already marked as arrived.");

        try
        {
            appointment.Status    = "Arrived";
            appointment.ArrivedAt = DateTimeOffset.UtcNow;
            appointment.UpdatedAt = DateTimeOffset.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another Staff member saved concurrently — treat as already arrived (edge case)
            throw new ConflictException("Patient already marked as arrived.");
        }

        await _auditLogService.LogAsync(
            actorId:      command.StaffActorId,
            actorRole:    "Staff",
            actionType:   "PATIENT_ARRIVED",
            targetEntity: "Appointment",
            targetId:     appointment.Id,
            metadata:     $"arrivedAt={appointment.ArrivedAt:O}",
            cancellationToken: cancellationToken);
    }
}
