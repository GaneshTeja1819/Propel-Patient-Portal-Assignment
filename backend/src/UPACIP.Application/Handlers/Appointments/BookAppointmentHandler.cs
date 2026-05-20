using Microsoft.Extensions.Logging;
using UPACIP.Application.Commands.Appointments;
using UPACIP.Application.Exceptions;
using UPACIP.Application.Interfaces;
using UPACIP.Domain.Entities;

namespace UPACIP.Application.Handlers.Appointments;

/// <summary>
/// Result returned by <see cref="BookAppointmentHandler"/>.
/// </summary>
public sealed class BookAppointmentResult
{
    public bool IsConflict { get; init; }
    public Guid? AppointmentId { get; init; }
    public string InsuranceValidationStatus { get; init; } = string.Empty;
    public Guid? SlotId { get; init; }
    public bool PreferredSlotRegistered { get; init; }
}

/// <summary>
/// Orchestrates the full appointment booking flow (CQRS command handler).
/// AC-001: appointment created with status Booked; slot marked unavailable; 201 returned.
/// AC-002: no-show risk score computed; default 0 on exception.
/// AC-003: insurance validated; default NotProvided when blank.
/// AC-004: HTTP 409 on slot conflict; TryAcquireSlotAsync returns false.
/// </summary>
public sealed class BookAppointmentHandler
{
    private readonly IAppointmentBookingStore _bookingStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INoShowRiskScorer _riskScorer;
    private readonly ISlotCacheService _slotCacheService;
    private readonly IAuditLogService _auditLogService;
    private readonly IPdfConfirmationJobEnqueuer _pdfJobEnqueuer;
    private readonly ILogger<BookAppointmentHandler> _logger;

    public BookAppointmentHandler(
        IAppointmentBookingStore bookingStore,
        IUnitOfWork unitOfWork,
        INoShowRiskScorer riskScorer,
        ISlotCacheService slotCacheService,
        IAuditLogService auditLogService,
        IPdfConfirmationJobEnqueuer pdfJobEnqueuer,
        ILogger<BookAppointmentHandler> logger)
    {
        _bookingStore     = bookingStore;
        _unitOfWork       = unitOfWork;
        _riskScorer       = riskScorer;
        _slotCacheService = slotCacheService;
        _auditLogService  = auditLogService;
        _pdfJobEnqueuer   = pdfJobEnqueuer;
        _logger           = logger;
    }

    public async Task<BookAppointmentResult> HandleAsync(
        BookAppointmentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.PreferredSlotId.HasValue && command.PreferredSlotId.Value == command.SlotId)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["preferredSlotId"] = ["Preferred slot must differ from booked slot"],
            });
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        var acquired = await _bookingStore.TryAcquireSlotAsync(command.SlotId, cancellationToken);
        if (!acquired)
        {
            _logger.LogInformation("Slot {SlotId} could not be acquired.", command.SlotId);
            return new BookAppointmentResult { IsConflict = true };
        }

        var providerId = await _bookingStore.GetSlotProviderIdAsync(command.SlotId, cancellationToken);
        var startTime  = await _bookingStore.GetSlotStartTimeAsync(command.SlotId, cancellationToken);

        var insuranceStatus = await ResolveInsuranceStatusAsync(
            command.InsuranceProvider, command.InsuranceId, cancellationToken);

        var noShowScore = await ComputeNoShowScoreAsync(
            command.PatientId, startTime?.UtcDateTime ?? DateTime.UtcNow, cancellationToken);

        var appointment = new Appointment
        {
            PatientId                 = command.PatientId,
            ProviderId                = providerId ?? Guid.Empty,
            SlotId                    = command.SlotId,
            Status                    = "Booked",
            NoShowRiskScore           = noShowScore,
            InsuranceValidationStatus = insuranceStatus,
            InsuranceProvider         = command.InsuranceProvider,
            InsuranceId               = command.InsuranceId,
            CreatedAt                 = DateTimeOffset.UtcNow,
            UpdatedAt                 = DateTimeOffset.UtcNow,
        };

        var appointmentId = await _bookingStore.SaveAppointmentAsync(appointment, cancellationToken);

        var preferredSlotRegistered = false;
        if (command.PreferredSlotId.HasValue)
        {
            var preferredSlotId = command.PreferredSlotId.Value;
            var preferredProviderId = await _bookingStore.GetSlotProviderIdAsync(preferredSlotId, cancellationToken);
            var preferredRequestedDate = await _bookingStore.GetSlotStartTimeAsync(preferredSlotId, cancellationToken)
                ?? DateTimeOffset.UtcNow;

            await _bookingStore.UpsertWaitlistEntryAsync(
                command.PatientId,
                appointmentId,
                preferredSlotId,
                preferredProviderId,
                preferredRequestedDate,
                cancellationToken);

            preferredSlotRegistered = true;
        }

        await transaction.CommitAsync(cancellationToken);

        await _slotCacheService.InvalidateSlotAsync(command.SlotId.ToString(), cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                actorId:      command.PatientId,
                actorRole:    "Patient",
                actionType:   "APPOINTMENT_CREATED",
                targetEntity: "Appointment",
                targetId:     appointmentId,
                metadata:     $"{{\"slotId\":\"{command.SlotId}\",\"insuranceStatus\":\"{insuranceStatus}\"}}",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audit log failed for {AppointmentId}; booking preserved.", appointmentId);
        }

        _pdfJobEnqueuer.Enqueue(appointmentId, isReschedule: false);

        return new BookAppointmentResult
        {
            IsConflict                = false,
            AppointmentId             = appointmentId,
            InsuranceValidationStatus = insuranceStatus,
            SlotId                    = command.SlotId,
            PreferredSlotRegistered   = preferredSlotRegistered,
        };
    }

    private async Task<string> ResolveInsuranceStatusAsync(
        string? provider, string? insuranceId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(insuranceId))
            return "NotProvided";

        var match = await _bookingStore.ValidateInsuranceAsync(provider, insuranceId, cancellationToken);
        return match is true ? "Validated" : "NotRecognised";
    }

    private async Task<int> ComputeNoShowScoreAsync(
        Guid patientId, DateTime slotUtc, CancellationToken cancellationToken)
    {
        try
        {
            var priorNoShows = await _bookingStore.CountPatientNoShowsAsync(patientId, cancellationToken);
            var leadTimeDays = Math.Max(0, (int)(slotUtc.Date - DateTime.UtcNow.Date).TotalDays);
            return _riskScorer.Score(priorNoShows, leadTimeDays);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Risk scoring failed for {PatientId}; defaulting to 0.", patientId);
            return 0;
        }
    }
}
