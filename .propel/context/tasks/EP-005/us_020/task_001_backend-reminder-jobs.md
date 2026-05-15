# Task - TASK_001

## Requirement Reference
- **User Story:** us_020
- **Story Location:** .propel/context/tasks/EP-005/us_020/us_020.md
- **Acceptance Criteria:**
  - AC-001: Reminders dispatched at configured pre-appointment intervals (e.g. 24 h and 2 h); email + SMS; Notification records created Queued → Sent
  - AC-002: Email sent via configured SMTP gateway (env var); delivery status in Notification record
  - AC-003: SMS sent via configured SMS gateway (env var); phone number absent → SMS step skipped without error
  - AC-004: Delivery failures retried up to 3 times with exponential back-off (10 s, 60 s, 360 s); Notification status = "Failed" after all retries; appointment unaffected
  - AC-005: Cancelled appointments do not receive reminders; record skipped without error
- **Edge Cases:**
  - Phone number in unsupported international format → SMS gateway error; logged; email still sent
  - Reminder fires within 1 min of appointment start → reminder skipped (too late to be useful)
  - Appointment rescheduled after reminders queued → old Hangfire reminder jobs cancelled; new jobs queued for new appointment date/time

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
| Background Jobs | Hangfire (PostgreSQL storage) | 1.8.x | NFR-008, TR-004, TR-010 — scheduled reminder jobs at configured intervals |
| Database | PostgreSQL via Supabase | 15 | TR-003 — Notification record; Appointment status check |

---

## Task Overview
Implement the appointment reminder system using Hangfire scheduled jobs. When an appointment is confirmed (in `BookAppointmentHandler`) two Hangfire scheduled jobs are enqueued: `AppointmentReminderJob` at `appointmentTime - 24h` and `appointmentTime - 2h` (intervals read from `appsettings.json`). The job checks the appointment is not cancelled before dispatching. It sends an email via `IEmailService` and, if a phone number is registered, an SMS via `ISmsService`. Each channel has its own `Notification` record. SMTP/SMS failures use Hangfire exponential back-off (3 attempts). When an appointment is rescheduled, `CancelAppointmentHandler`/`RescheduleAppointmentHandler` cancel the old scheduled jobs and enqueue new ones.

## Dependent Tasks
- `task_001_backend-hangfire.md` (US_003) — Hangfire server running; job storage configured
- `task_002_backend-booking-endpoint.md` (US_013) — `BookAppointmentHandler` must exist; reminder jobs enqueued here
- `task_002_backend-cancel-reschedule.md` (US_014) — reschedule path must cancel old reminder jobs + enqueue new

## Impacted Components
- `backend/src/UPACIP.Infrastructure/BackgroundJobs/AppointmentReminderJob.cs` — new Hangfire job
- `backend/src/UPACIP.Infrastructure/Services/SmsService.cs` — new SMS gateway adapter
- `backend/src/UPACIP.Application/Handlers/Appointments/BookAppointmentHandler.cs` — enqueue reminder jobs after successful booking
- `backend/src/UPACIP.Application/Commands/Appointments/RescheduleAppointmentCommand.cs` — cancel old jobs; enqueue new

## Implementation Plan
1. Define `ReminderIntervals` in `appsettings.json`: `{ "hours": [24, 2] }` — configurable without code change
2. Create `AppointmentReminderJob.cs` accepting `Guid appointmentId, string channel`:
   - Load `Appointment`; if `status == "Cancelled"` → return (AC-005)
   - Check `appointment.startDateTime - UtcNow < 1 min` → skip (edge case)
   - If `channel == "Email"`: call `IEmailService.SendAsync` with reminder template; `Notification.type = "AppointmentReminder_Email"`
   - If `channel == "SMS"`: check `patient.phoneNumber` present; if null → return; else call `ISmsService.SendAsync`; on unsupported format error → log warning; do not throw (AC-003 edge case)
   - Create `Notification` record `status = "Queued"`; update to `"Sent"` on success; `"Failed"` after all Hangfire retries
3. Create `SmsService.cs`: wraps configurable free-tier SMS gateway (Twilio Free Tier / TextBelt); reads credentials from env vars (`SMS_GATEWAY_SID`, `SMS_GATEWAY_TOKEN`); no hardcoded credentials
4. In `BookAppointmentHandler.Handle`: after successful booking, for each interval in `ReminderIntervals`: enqueue `AppointmentReminderJob` via `BackgroundJob.Schedule(...)` at `appointmentTime - interval hours`; store returned Hangfire job IDs in `Appointment.reminderJobIds` (JSON column) for later cancellation
5. In `RescheduleAppointmentCommand.Handle`: load `appointment.reminderJobIds`; call `BackgroundJob.Delete(jobId)` for each; re-enqueue new scheduled jobs at new appointment time

## Current Project State
```
backend/
  src/
    UPACIP.Infrastructure/BackgroundJobs/GeneratePdfConfirmationJob.cs  (IEmailService pattern)
    UPACIP.Application/Handlers/Appointments/BookAppointmentHandler.cs  (from US_013)
    UPACIP.Application/Commands/Appointments/RescheduleAppointmentCommand.cs  (from US_014)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/UPACIP.Infrastructure/BackgroundJobs/AppointmentReminderJob.cs | Scheduled email + SMS reminder job |
| CREATE | backend/src/UPACIP.Infrastructure/Services/SmsService.cs | SMS gateway adapter (env-var credentials) |
| MODIFY | backend/src/UPACIP.Application/Handlers/Appointments/BookAppointmentHandler.cs | Enqueue two scheduled reminder jobs post-booking |
| MODIFY | backend/src/UPACIP.Application/Commands/Appointments/RescheduleAppointmentCommand.cs | Cancel old reminder jobs; enqueue new for rescheduled time |
| MODIFY | backend/src/UPACIP.Domain/Entities/Appointment.cs | Add reminderJobIds JSON column |

## External References
- [Hangfire — Scheduling Methods](https://docs.hangfire.io/en/latest/background-methods/scheduling-methods.html)
- [Hangfire — BackgroundJob.Delete](https://docs.hangfire.io/en/latest/background-methods/cancelling-background-jobs.html)
- [Twilio Free Trial SMS](https://www.twilio.com/docs/usage/tutorials/how-to-use-your-free-trial-account)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Book appointment → two Hangfire scheduled jobs appear in dashboard (24 h and 2 h before); at trigger time email received; Notification `status = "Sent"`
- [ ] Appointment cancelled before reminder fires → job evaluates `status == Cancelled`; no reminder sent
- [ ] Patient has no phone number → SMS step skipped; email reminder still sent; no error
- [ ] Reschedule → old scheduled jobs cancelled in Hangfire; new jobs for updated time

## Implementation Checklist
- [ ] Create `AppointmentReminderJob`: cancelled appointment guard; 1-minute-before skip; email dispatch (AC-001, AC-002, AC-005)
- [ ] SMS dispatch via `ISmsService`; phone absent → skip without error; unsupported format → log warning (AC-003)
- [ ] `Notification` record per channel: Queued → Sent / Failed; `[AutomaticRetry(Attempts = 3)]` with back-off (AC-004)
- [ ] Create `SmsService` with env-var credentials; no hardcoded secrets (OWASP A02) (AC-003)
- [ ] Enqueue two scheduled `AppointmentReminderJob`s from `BookAppointmentHandler`; store job IDs (AC-001)
- [ ] On reschedule: cancel old reminder jobs + re-enqueue for new appointment time (edge case)
- [ ] Reminder intervals configurable via `appsettings.json` (AC-001)
