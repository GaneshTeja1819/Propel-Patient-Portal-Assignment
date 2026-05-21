# Task - TASK_002

## Requirement Reference
- **User Story:** us_015
- **Story Location:** .propel/context/tasks/EP-002/us_015/us_015.md
- **Acceptance Criteria:**
  - AC-001: Hangfire job → QuestPDF renders appointment confirmation PDF; attached to SMTP email; delivery status logged
  - AC-003: QuestPDF exception → retry once; second failure → job "Failed"; Appointment unchanged; `PDF_GENERATION_FAILED` audit written; no blocking error shown to patient
  - AC-004: SMTP unavailable → Hangfire exponential back-off, 3 retries (10 s, 60 s, 360 s); `Notification.status = "Failed"` after all retries; Appointment unaffected
  - AC-005: Triggered after reschedule → new PDF with updated date/time; email subject "Updated Appointment Confirmation"
- **Edge Cases:**
  - Duplicate job pickup (Hangfire visibility timeout) → idempotency key `appointment_{id}_pdf` prevents double-send
  - Bounced email (SMTP 5xx) → log failure; no further retry for that failure event
  - Special characters in patient name → QuestPDF escapes safely (UTF-8 output)

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
| Background Jobs | Hangfire (PostgreSQL storage) | 1.8.x | NFR-008, TR-004 — async PDF generation + SMTP dispatch |
| PDF | QuestPDF | Latest stable | TR-006 — appointment confirmation PDF rendering |
| Database | PostgreSQL via Supabase | 15 | TR-003 — Notification record; Appointment record |

---

## Task Overview
Implement the `GeneratePdfConfirmationJob` Hangfire background job. An idempotency key prevents duplicate processing. The job renders an appointment confirmation PDF via QuestPDF (patient name, appointment ID, date, time, provider), attaches it to an SMTP email, and dispatches it. A `Notification` record tracks the delivery lifecycle (Queued → Sent / Failed). QuestPDF failures allow one retry; on second failure the job is marked Failed and a `PDF_GENERATION_FAILED` audit entry is written without affecting the Appointment. SMTP failures use Hangfire's built-in exponential back-off (3 attempts). When triggered from a reschedule the email subject is "Updated Appointment Confirmation". A lightweight `GET /api/v1/appointments/{id}/confirmation-status` endpoint surfaces `Notification.status` for the frontend polling (US_015 task_001).

## Dependent Tasks
- `task_001_backend-hangfire.md` (US_003) — Hangfire server running
- `task_001_backend-migrations.md` (US_005) — `Notification` entity and table must exist
- `task_002_backend-booking-endpoint.md` (US_013) — job is first enqueued from booking handler
- `task_002_backend-cancel-reschedule.md` (US_014) — job re-enqueued from reschedule handler

## Impacted Components
- `backend/src/UPACIP.Infrastructure/BackgroundJobs/GeneratePdfConfirmationJob.cs` — new Hangfire job
- `backend/src/UPACIP.Infrastructure/Documents/AppointmentPdfTemplate.cs` — new QuestPDF template
- `backend/src/UPACIP.API/Controllers/AppointmentsController.cs` — add GET /confirmation-status action

## Implementation Plan
1. Create `AppointmentPdfTemplate.cs` using QuestPDF Fluent API: layout includes UPACIP header, appointment ID, patient full name, date/time (formatted locale), provider name, footer with generated timestamp; uses `Document.Create()` → `Page()` pattern; all strings are HTML-escaped via QuestPDF's built-in handling
2. Create `GeneratePdfConfirmationJob.cs`:
   - Accepts `GeneratePdfJobArgs { Guid AppointmentId, bool IsReschedule }` as serialisable record
   - Idempotency: query `Notification` where `referenceId = AppointmentId` and `notificationType = "AppointmentConfirmation"` and `status == "Sent"`; if found → return (already sent)
   - Create/upsert `Notification` with `status = "Queued"` if not exists
   - Render PDF: `AppointmentPdfTemplate.Generate(appointmentData)` — wrap in try/catch; on exception → `Notification.status = "Failed"`; write `PDF_GENERATION_FAILED` audit; retry attribute allows one retry from Hangfire; on second failure → return without throwing (let Hangfire mark job as Failed)
   - SMTP dispatch: inject `IEmailService`; subject = `IsReschedule ? "Updated Appointment Confirmation" : "Appointment Confirmation"`; attach PDF byte array; on SMTP failure → let Hangfire retry (back-off decorator); after all retries → `Notification.status = "Failed"`
   - On success → `Notification.status = "Sent"`; save
3. Add `[AutomaticRetry(Attempts = 1)]` attribute on the job class for QuestPDF retries; configure Hangfire retry delays in `Program.cs` for SMTP failures (10 s, 60 s, 360 s)
4. Add `GET /api/v1/appointments/{id:guid}/confirmation-status` to `AppointmentsController`: query `Notification.status` for `appointmentId`; return `{ status: "Queued" | "Processing" | "Sent" | "Failed" }`; require `[Authorize(Policy = "PatientPolicy")]`
5. Wire `IEmailService` reading SMTP credentials from environment variables (`SMTP_HOST`, `SMTP_PORT`, `SMTP_USER`, `SMTP_PASS`); do not hardcode credentials

## Current Project State
```
backend/
  src/
    UPACIP.Infrastructure/BackgroundJobs/WaitlistNotificationJob.cs  (from US_014)
    UPACIP.API/Controllers/AppointmentsController.cs  (POST /appointments, PATCH cancel/reschedule)
    UPACIP.Domain/Entities/Notification.cs  (from US_005)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/UPACIP.Infrastructure/BackgroundJobs/GeneratePdfConfirmationJob.cs | Hangfire job with idempotency, QuestPDF, SMTP |
| CREATE | backend/src/UPACIP.Infrastructure/Documents/AppointmentPdfTemplate.cs | QuestPDF Fluent document template |
| MODIFY | backend/src/UPACIP.API/Controllers/AppointmentsController.cs | Add GET /confirmation-status endpoint |

## External References
- [QuestPDF Fluent API Docs](https://www.questpdf.com/getting-started.html)
- [Hangfire AutomaticRetry attribute](https://docs.hangfire.io/en/latest/background-methods/performing-recurrent-tasks.html)
- [ASP.NET Core Environment Variable Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [x] Book appointment → Hangfire dashboard shows `GeneratePdfConfirmationJob` enqueued → executed; `Notification.status = "Sent"`; email received with PDF attachment
- [x] Force QuestPDF to throw → job retried once → if second failure → `status = "Failed"`; audit `PDF_GENERATION_FAILED` written; Appointment unchanged
- [x] Force SMTP failure → Hangfire retries at 10/60/360 s; after 3 failures → `Notification.status = "Failed"`; Appointment unchanged
- [x] Enqueue job twice with same `AppointmentId` → idempotency key prevents double-send; only one email delivered
- [x] Reschedule triggers job with `IsReschedule = true` → email subject starts "Updated Appointment Confirmation"

## Implementation Checklist
- [x] Build `AppointmentPdfTemplate` with QuestPDF Fluent API; fields: appointmentId, patient name, date/time, provider; UTF-8 safe (AC-001, edge case)
- [x] Implement `GeneratePdfConfirmationJob` with idempotency guard (duplicate pickup edge case) (AC-001)
- [x] Wrap QuestPDF render in try/catch; audit `PDF_GENERATION_FAILED` on failure; Appointment unchanged; allow a single retry before terminal failure handling (AC-003)
- [x] SMTP dispatch via `IEmailService`; credentials from env vars only (Security: no hardcoded secrets) (AC-001, AC-004)
- [x] Configure Hangfire retry delays 10 s / 60 s / 360 s for SMTP failures; `Notification.status = "Failed"` after all retries; Appointment unchanged (AC-004)
- [x] `IsReschedule = true` → email subject "Updated Appointment Confirmation" (AC-005)
- [x] Add `GET /confirmation-status` endpoint; `[Authorize(Policy = "PatientPolicy")]`; return Notification.status (US_015 frontend polling)
