# Task - TASK_002

## Requirement Reference
- **User Story:** us_017
- **Story Location:** .propel/context/tasks/EP-003/us_017/us_017.md
- **Acceptance Criteria:**
  - AC-003: Email dispatched confirming swap with new date/time; Notification record created with status "Sent" or "Failed"
  - AC-004: If email fails, failure is logged in audit log; swap remains in effect
- **Edge Cases:**
  - Email delivery fails after successful swap → swap unchanged; Notification record status = "Failed"; audit log records delivery failure
  - Patient email address missing or invalid → log error; no SMS fallback for swap notification; WaitlistEntry remains deleted (swap already committed)

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
| Background Jobs | Hangfire (PostgreSQL storage) | 1.8.x | NFR-008 — notification dispatched as chained Hangfire job after successful swap |
| Database | PostgreSQL via Supabase | 15 | TR-003 — Notification record lifecycle (Queued → Sent / Failed) |

---

## Task Overview
Implement `SlotSwapNotificationJob`, a lightweight Hangfire job chained from `SlotSwapJob` upon a successful swap. The job loads the patient's updated appointment, builds the email body (new date, time, appointment ID), dispatches via `IEmailService` (SMTP credentials from env vars), and writes a `Notification` record (`notificationType = "SlotSwap"`, `status = "Sent" | "Failed"`). SMTP failures use Hangfire's built-in retry (3 attempts, back-off 10 s / 60 s / 360 s). The swap is not rolled back on email failure.

## Dependent Tasks
- `task_001_backend-slot-swap-job.md` (US_017) — `SlotSwapJob` enqueues this job on successful swap
- `task_002_backend-pdf-hangfire-job.md` (US_015) — `IEmailService` must be registered; `Notification` entity available

## Impacted Components
- `backend/src/UPACIP.Infrastructure/BackgroundJobs/SlotSwapNotificationJob.cs` — new Hangfire job

## Implementation Plan
1. Create `SlotSwapNotificationJob.cs` accepting `Guid appointmentId, Guid newSlotId`
2. Load updated `Appointment` with `AppointmentSlot` and `User` (patient) from DB
3. Build email: subject "Your appointment has been moved to [new date/time]"; body includes appointment ID, new date, time, clinic name
4. Call `IEmailService.SendAsync(patient.Email, subject, body)` — credentials from env vars; wrap in try/catch
5. Create `Notification` record: `notificationType = "SlotSwap"`, `referenceId = appointmentId`; `status = "Queued"` before dispatch → update to `"Sent"` on success; on exception → `"Failed"`; write `SLOT_SWAP_NOTIFICATION_FAILED` audit entry on failure
6. SMTP retry: `[AutomaticRetry(Attempts = 3)]` on job class with Hangfire back-off configuration

## Current Project State
```
backend/
  src/
    UPACIP.Infrastructure/BackgroundJobs/SlotSwapJob.cs      (from task_001)
    UPACIP.Infrastructure/BackgroundJobs/GeneratePdfConfirmationJob.cs  (IEmailService pattern)
    UPACIP.Domain/Entities/Notification.cs
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/UPACIP.Infrastructure/BackgroundJobs/SlotSwapNotificationJob.cs | Email notification after successful slot swap |

## External References
- [Hangfire AutomaticRetry](https://docs.hangfire.io/en/latest/background-methods/performing-recurrent-tasks.html)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [x] Successful swap → email received with new date/time; `Notification.status = "Sent"`
- [x] Force SMTP failure → Notification `status = "Failed"` after retries; `SLOT_SWAP_NOTIFICATION_FAILED` audit; swap appointment unchanged

## Implementation Checklist
- [x] Create `SlotSwapNotificationJob` with appointment data load and email dispatch via `IEmailService` (AC-003)
- [x] Create `Notification` record: Queued → Sent / Failed lifecycle (AC-003)
- [x] On SMTP failure → `Notification.status = "Failed"`; write audit entry; swap unaffected (AC-004)
- [x] `[AutomaticRetry(Attempts = 3)]` on job; back-off 10 s / 60 s / 360 s (AC-004)
- [x] Credentials from env vars only — no hardcoded SMTP secrets (OWASP A02)
