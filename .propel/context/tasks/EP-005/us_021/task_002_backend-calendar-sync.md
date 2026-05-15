# Task - TASK_002

## Requirement Reference
- **User Story:** us_021
- **Story Location:** .propel/context/tasks/EP-005/us_021/us_021.md
- **Acceptance Criteria:**
  - AC-001: OAuth consent granted → calendar event created; CalendarSync record with encrypted tokens + syncStatus="Synced"
  - AC-002: Appointment rescheduled → existing calendar event updated; CalendarSync.lastSyncAt updated
  - AC-003: Appointment cancelled → calendar event deleted; CalendarSync.syncStatus = "Deleted"
  - AC-004: Calendar API failure after all retries → frontend notified; appointment unaffected
  - AC-005: Denied OAuth consent → no CalendarSync record; no error
- **Edge Cases:**
  - OAuth access token expires → token refresh attempted before API call; if refresh fails → sync logged as failed; patient may re-authorise
  - Patient grants consent for both Google and Outlook → two independent CalendarSync records; one failure does not block the other
  - Calendar API rate limit → retry with exponential back-off; if all retries fail → CalendarSync.syncStatus = "Failed"; amber Toast shown to frontend

---

## Design References [CONDITIONAL: UI Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | No |
| **Figma URL** | N/A |
| **Wireframe Status** | N/A |
| **Wireframe Type** | N/A |
| **Wireframe Path/URL** | N/A |
| **Screen Spec** | N/A |
| **UXR Requirements** | N/A |
| **Design Tokens** | N/A |

---

## AI References [CONDITIONAL: AI Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | No |
| **AIR Requirements** | N/A |
| **AI Pattern** | N/A |
| **Prompt Template Path** | N/A |
| **Guardrails Config** | N/A |
| **Model Provider** | N/A |

---

## Mobile References [CONDITIONAL: Mobile Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |
| **Platform Target** | N/A |
| **Min OS Version** | N/A |
| **Mobile Framework** | N/A |

---

## Applicable Technology Stack

| Layer | Technology | Version | Justification |
|-------|------------|---------|---------------|
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | TR-002, TR-009 — OAuth callback; calendar CRUD endpoints |
| Database | PostgreSQL via Supabase | 15 | TR-003 — CalendarSync entity with encrypted token columns |
| Security | AES-256-GCM (.NET 8 built-in) | .NET 8 | NFR-001 — OAuth access + refresh token encryption at rest |

---

## Task Overview
Implement the calendar sync backend: `GET /api/v1/calendar-sync/oauth-url` (returns OAuth authorisation URL), `POST /api/v1/calendar-sync/callback` (exchanges auth code for tokens, creates calendar event, persists CalendarSync record), and a `ICalendarSyncService` that handles event create/update/delete for both Google and Outlook providers. OAuth tokens are encrypted with `IPhiEncryptionService` before storage. The `CalendarSyncService` is invoked from `RescheduleAppointmentCommand` and `CancelAppointmentCommand` to update/delete calendar events. An exponential back-off Hangfire job handles retries for failed sync operations without blocking the primary appointment flow.

## Dependent Tasks
- `task_001_backend-phi-encryption.md` (US_006) — `IPhiEncryptionService` used for token encryption
- `task_002_backend-cancel-reschedule.md` (US_014) — reschedule/cancel handlers must call `ICalendarSyncService`
- `task_001_backend-hangfire.md` (US_003) — Hangfire for retry jobs

## Impacted Components
- `backend/src/UPACIP.Application/Services/CalendarSyncService.cs` — new service; Google + Outlook adapters
- `backend/src/UPACIP.API/Controllers/CalendarSyncController.cs` — new controller
- `backend/src/UPACIP.Domain/Entities/CalendarSync.cs` — verify entity exists (from US_005)
- `backend/src/UPACIP.Application/Commands/Appointments/RescheduleAppointmentCommand.cs` — call CalendarSync update
- `backend/src/UPACIP.Application/Commands/Appointments/CancelAppointmentCommand.cs` — call CalendarSync delete

## Implementation Plan
1. Create `CalendarSyncController`:
   - `GET /api/v1/calendar-sync/oauth-url?provider=Google|Outlook` — build OAuth authorization URL from provider credentials (env vars: `GOOGLE_CLIENT_ID`, `OUTLOOK_CLIENT_ID`); return URL; `[Authorize(Policy = "PatientPolicy")]`
   - `POST /api/v1/calendar-sync/callback` — accept `{ code, state, provider }`; exchange code for tokens via OAuth token endpoint; encrypt `accessToken` and `refreshToken` with `IPhiEncryptionService`; call `CalendarSyncService.CreateEventAsync`; persist `CalendarSync` record with `syncStatus = "Synced"`; return HTTP 200
   - If `code` absent (denied): return HTTP 200 without creating CalendarSync record (AC-005)
2. Create `ICalendarSyncService` with `CreateEventAsync`, `UpdateEventAsync`, `DeleteEventAsync`:
   - Google adapter: calls Google Calendar API `POST/PUT/DELETE /calendars/primary/events`
   - Outlook adapter: calls Microsoft Graph API `POST/PATCH/DELETE /me/events`
   - Token refresh: before each call, check `accessToken` expiry; if expired → call token refresh endpoint; update `CalendarSync` record with new encrypted tokens; if refresh fails → throw `TokenRefreshFailedException`
3. On calendar API failure: do not throw to the caller; log failure; update `CalendarSync.syncStatus = "Failed"`; enqueue `CalendarSyncRetryJob` via Hangfire (3 attempts, 10 s / 60 s / 360 s back-off)
4. Wire `CalendarSyncService.UpdateEventAsync` in `RescheduleAppointmentCommand` handler (after transaction commit; failure does not roll back reschedule)
5. Wire `CalendarSyncService.DeleteEventAsync` in `CancelAppointmentCommand` handler (same: failure non-blocking)

## Current Project State
```
backend/
  src/
    UPACIP.Infrastructure/Security/PhiEncryptionService.cs  (from US_006)
    UPACIP.Application/Commands/Appointments/RescheduleAppointmentCommand.cs  (from US_014)
    UPACIP.Application/Commands/Appointments/CancelAppointmentCommand.cs  (from US_014)
    UPACIP.Domain/Entities/CalendarSync.cs  (from US_005)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/UPACIP.Application/Services/CalendarSyncService.cs | Google + Outlook calendar adapters; token refresh |
| CREATE | backend/src/UPACIP.API/Controllers/CalendarSyncController.cs | OAuth URL + callback endpoints |
| MODIFY | backend/src/UPACIP.Application/Commands/Appointments/RescheduleAppointmentCommand.cs | Call CalendarSync update (non-blocking) |
| MODIFY | backend/src/UPACIP.Application/Commands/Appointments/CancelAppointmentCommand.cs | Call CalendarSync delete (non-blocking) |

## External References
- [Google Calendar API — Events.insert](https://developers.google.com/calendar/api/v3/reference/events/insert)
- [Microsoft Graph API — Create event](https://learn.microsoft.com/en-us/graph/api/user-post-events)
- [OAuth 2.0 Token Refresh](https://datatracker.ietf.org/doc/html/rfc6749#section-6)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] OAuth callback with valid code → calendar event created in Google/Outlook; CalendarSync record with syncStatus="Synced"; tokens stored encrypted
- [ ] Reschedule → calendar event updated; CalendarSync.lastSyncAt updated; appointment unaffected if sync fails
- [ ] Cancel → calendar event deleted; CalendarSync.syncStatus = "Deleted"
- [ ] Deny OAuth consent → no CalendarSync record created; HTTP 200 returned

## Implementation Checklist
- [ ] `GET /api/v1/calendar-sync/oauth-url` returns provider OAuth URL; credentials from env vars (AC-001, OWASP A02)
- [ ] `POST /api/v1/calendar-sync/callback`: token exchange; encrypt tokens; create calendar event; persist CalendarSync (AC-001)
- [ ] Denied consent (no code) → return HTTP 200; no CalendarSync record (AC-005)
- [ ] Token refresh before each calendar API call; `TokenRefreshFailedException` → sync failed; patient may re-authorise (edge case)
- [ ] Calendar API failure → non-blocking; enqueue `CalendarSyncRetryJob`; `CalendarSync.syncStatus = "Failed"` (AC-004)
- [ ] Wire `UpdateEventAsync` in reschedule handler (non-blocking, post-transaction) (AC-002)
- [ ] Wire `DeleteEventAsync` in cancel handler (non-blocking) (AC-003)
- [ ] Both providers independent; failure of one does not affect the other (edge case)
