using UPACIP.Application.Commands.Appointments;
using UPACIP.Application.Exceptions;
using UPACIP.Application.Interfaces;

namespace UPACIP.Application.Handlers.Queue;

/// <summary>
/// Soft-deletes a queue entry by setting <c>status = "RemovedFromQueue"</c>.
/// Requires a non-empty reason string. Always accepts even if the appointment
/// is already "Arrived" — the override is recorded in the audit (AC-003, edge case).
/// </summary>
public sealed class RemoveQueueEntryHandler
{
    private readonly IQueueStore _store;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public RemoveQueueEntryHandler(
        IQueueStore store,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService)
    {
        _store = store;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
    }

    public async Task HandleAsync(RemoveQueueEntryCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["reason"] = ["A removal reason is required."]
            });

        var appointment = await _store.GetAppointmentWithSlotAsync(command.AppointmentId, cancellationToken)
            ?? throw new ValidationException(new Dictionary<string, string[]>
            {
                ["appointmentId"] = ["Appointment not found."]
            });

        string priorStatus = appointment.Status;

        appointment.Status    = "RemovedFromQueue";
        appointment.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            actorId:      command.StaffActorId,
            actorRole:    "Staff",
            actionType:   "QUEUE_ENTRY_REMOVED",
            targetEntity: "Appointment",
            targetId:     appointment.Id,
            metadata:     $"reason={command.Reason};priorStatus={priorStatus}",
            cancellationToken: cancellationToken);
    }
}
