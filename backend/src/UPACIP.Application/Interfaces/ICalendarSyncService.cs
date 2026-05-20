namespace UPACIP.Application.Interfaces;

/// <summary>
/// Manages Google Calendar and Outlook calendar events linked to appointments.
/// All operations are non-blocking — failures do not roll back the appointment (US_021, AC-004).
/// </summary>
public interface ICalendarSyncService
{
    /// <summary>
    /// Exchanges the OAuth authorisation code for tokens, creates a calendar event,
    /// and persists a <c>CalendarSync</c> record with <c>SyncStatus = "Synced"</c>.
    /// </summary>
    Task CreateEventAsync(
        Guid userId,
        Guid appointmentId,
        string provider,
        string authCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the calendar event when an appointment is rescheduled.
    /// If no <c>CalendarSync</c> record exists the call is a no-op.
    /// </summary>
    Task UpdateEventAsync(Guid appointmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the calendar event when an appointment is cancelled.
    /// Sets <c>CalendarSync.SyncStatus = "Deleted"</c>.
    /// </summary>
    Task DeleteEventAsync(Guid appointmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the OAuth authorisation URL for the requested provider.
    /// </summary>
    string GetOAuthUrl(string provider, string redirectUri);
}
