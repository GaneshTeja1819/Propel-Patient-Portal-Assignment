using Microsoft.Extensions.Logging;
using UPACIP.Application.Commands.Appointments;
using UPACIP.Application.Interfaces;

namespace UPACIP.Application.Handlers.Appointments;

public enum RescheduleAppointmentStatus
{
    Success,
    NotFound,
    Conflict
}

public sealed class RescheduleAppointmentResult
{
    public RescheduleAppointmentStatus Status { get; init; }
    public Guid? OldSlotId { get; init; }
    public Guid? NewSlotId { get; init; }
}

/// <summary>
/// Handles atomic reschedule updates for appointment slots.
/// </summary>
public sealed class RescheduleAppointmentHandler
{
    private readonly IAppointmentManagementStore _managementStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISlotCacheService _slotCacheService;
    private readonly IAuditLogService _auditLogService;
    private readonly ISlotSwapJobEnqueuer _slotSwapJobEnqueuer;
    private readonly IPdfConfirmationJobEnqueuer _pdfJobEnqueuer;
    private readonly ILogger<RescheduleAppointmentHandler> _logger;

    public RescheduleAppointmentHandler(
        IAppointmentManagementStore managementStore,
        IUnitOfWork unitOfWork,
        ISlotCacheService slotCacheService,
        IAuditLogService auditLogService,
        ISlotSwapJobEnqueuer slotSwapJobEnqueuer,
        IPdfConfirmationJobEnqueuer pdfJobEnqueuer,
        ILogger<RescheduleAppointmentHandler> logger)
    {
        _managementStore = managementStore;
        _unitOfWork = unitOfWork;
        _slotCacheService = slotCacheService;
        _auditLogService = auditLogService;
        _slotSwapJobEnqueuer = slotSwapJobEnqueuer;
        _pdfJobEnqueuer = pdfJobEnqueuer;
        _logger = logger;
    }

    public async Task<RescheduleAppointmentResult> HandleAsync(
        RescheduleAppointmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var appointment = await _managementStore.GetAppointmentWithSlotAsync(
            command.AppointmentId,
            command.PatientId,
            cancellationToken);

        if (appointment is null)
        {
            return new RescheduleAppointmentResult { Status = RescheduleAppointmentStatus.NotFound };
        }

        var newSlot = await _managementStore.GetSlotByIdAsync(command.NewSlotId, cancellationToken);
        if (newSlot is null || !newSlot.IsAvailable)
        {
            return new RescheduleAppointmentResult { Status = RescheduleAppointmentStatus.Conflict };
        }

        var oldSlotId = appointment.SlotId;

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            appointment.Slot.IsAvailable = true;
            newSlot.IsAvailable = false;
            appointment.SlotId = newSlot.Id;
            appointment.ProviderId = newSlot.ProviderId;
            appointment.UpdatedAt = DateTimeOffset.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex) when (IsConcurrencyException(ex))
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogInformation(ex, "Reschedule conflict for appointment {AppointmentId}.", appointment.Id);
            return new RescheduleAppointmentResult { Status = RescheduleAppointmentStatus.Conflict };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        await _slotCacheService.InvalidateSlotAsync(oldSlotId.ToString(), cancellationToken);
        await _slotCacheService.InvalidateSlotAsync(newSlot.Id.ToString(), cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                actorId: command.PatientId,
                actorRole: "Patient",
                actionType: "APPOINTMENT_RESCHEDULED",
                targetEntity: "Appointment",
                targetId: appointment.Id,
                metadata: $"{{\"oldSlotId\":\"{oldSlotId}\",\"newSlotId\":\"{newSlot.Id}\"}}",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audit log failed for rescheduled appointment {AppointmentId}.", appointment.Id);
        }

        _pdfJobEnqueuer.Enqueue(appointment.Id, isReschedule: true);
        _slotSwapJobEnqueuer.Enqueue(oldSlotId);

        return new RescheduleAppointmentResult
        {
            Status = RescheduleAppointmentStatus.Success,
            OldSlotId = oldSlotId,
            NewSlotId = newSlot.Id,
        };
    }

    private static bool IsConcurrencyException(Exception exception)
    {
        var current = exception;
        while (current is not null)
        {
            if (string.Equals(current.GetType().Name, "DbUpdateConcurrencyException", StringComparison.Ordinal))
                return true;

            current = current.InnerException!;
        }

        return false;
    }
}
