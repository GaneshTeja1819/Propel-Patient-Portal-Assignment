using UPACIP.Application.Interfaces;
using UPACIP.Application.Queries.Queue;

namespace UPACIP.Application.Handlers.Queue;

/// <summary>
/// Projects today's appointments into <see cref="QueueEntryDto"/> records
/// ordered by <c>displayOrder</c> (explicit order first), then slot start time.
/// </summary>
public sealed class GetTodaysQueueHandler
{
    private readonly IQueueStore _store;

    public GetTodaysQueueHandler(IQueueStore store) => _store = store;

    public async Task<IReadOnlyList<QueueEntryDto>> HandleAsync(
        GetTodaysQueueQuery query,
        CancellationToken cancellationToken = default)
    {
        var appointments = await _store.GetTodaysAppointmentsAsync(query.UtcToday, cancellationToken);

        return appointments
            .Select(a => new QueueEntryDto(
                Id:            a.Id,
                PatientName:   BuildPatientName(a),
                ScheduledTime: a.Slot.StartTime,
                Status:        a.Status,
                DisplayOrder:  a.DisplayOrder))
            .ToList();
    }

    private static string BuildPatientName(Domain.Entities.Appointment a)
    {
        if (a.Patient is not null)
            return $"{a.Patient.FirstName} {a.Patient.LastName}".Trim();

        if (!string.IsNullOrWhiteSpace(a.AnonymousPatientDetails))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(a.AnonymousPatientDetails);
                if (doc.RootElement.TryGetProperty("name", out var name))
                    return name.GetString() ?? "Anonymous";
            }
            catch { /* malformed JSON — fall through */ }
        }

        return "Anonymous";
    }
}

/// <summary>Projected queue entry returned to callers of GET /api/v1/queue/today.</summary>
public sealed record QueueEntryDto(
    Guid Id,
    string PatientName,
    DateTimeOffset ScheduledTime,
    string Status,
    int? DisplayOrder);
