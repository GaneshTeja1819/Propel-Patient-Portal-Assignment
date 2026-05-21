using UPACIP.Application.Commands.Appointments;
using UPACIP.Application.Exceptions;
using UPACIP.Application.Interfaces;

namespace UPACIP.Application.Handlers.Queue;

/// <summary>
/// Updates the <c>displayOrder</c> of a queue entry. Always saves the new
/// order (override semantics). Returns a <c>slotConflict</c> flag if another
/// appointment in a nearby time window shares the same target order (AC-002).
/// </summary>
public sealed class ReorderQueueEntryHandler
{
    // Slots within 30 minutes of the reference time are considered conflict candidates.
    private const int ConflictWindowMinutes = 30;

    private readonly IQueueStore _store;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public ReorderQueueEntryHandler(
        IQueueStore store,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService)
    {
        _store = store;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
    }

    public async Task<ReorderResult> HandleAsync(
        ReorderQueueEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        var appointment = await _store.GetAppointmentWithSlotAsync(command.AppointmentId, cancellationToken)
            ?? throw new ValidationException(new Dictionary<string, string[]>
            {
                ["appointmentId"] = ["Appointment not found."]
            });

        int oldOrder = appointment.DisplayOrder ?? -1;

        var hasConflict = await _store.HasSlotConflictAsync(
            excludeAppointmentId: command.AppointmentId,
            referenceTime:        appointment.Slot.StartTime,
            targetOrder:          command.NewIndex,
            windowMinutes:        ConflictWindowMinutes,
            cancellationToken:    cancellationToken);

        appointment.DisplayOrder = command.NewIndex;
        appointment.UpdatedAt    = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            actorId:      command.StaffActorId,
            actorRole:    "Staff",
            actionType:   "QUEUE_REORDERED",
            targetEntity: "Appointment",
            targetId:     appointment.Id,
            metadata:     $"oldOrder={oldOrder};newOrder={command.NewIndex};slotConflict={hasConflict}",
            cancellationToken: cancellationToken);

        return new ReorderResult(hasConflict);
    }
}

/// <summary>Result returned to the caller indicating whether a slot conflict was detected.</summary>
public sealed record ReorderResult(bool SlotConflict);
