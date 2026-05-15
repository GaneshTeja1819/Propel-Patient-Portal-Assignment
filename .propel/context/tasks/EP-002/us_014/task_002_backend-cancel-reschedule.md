# Task - TASK_002

## Requirement Reference
- **User Story:** us_014
- **Story Location:** .propel/context/tasks/EP-002/us_014/us_014.md
- **Acceptance Criteria:**
  - AC-001: Cancel → status="Cancelled"; slot released to available pool; waitlisted patient notification Hangfire job enqueued; audit entry written
  - AC-002: Cancel prompt dismissal handled on frontend only; backend only accepts confirmed cancel calls
  - AC-003: Past appointments → PATCH /cancel returns HTTP 422 (cannot cancel past appointment)
  - AC-004: Reschedule → old slot released and new slot reserved atomically; updated PDF email enqueued; audit entry written
  - AC-005: Reschedule conflict (HTTP 409) → neither slot modified; rollback; conflict message returned
- **Edge Cases:**
  - Cancel then immediate rebook same slot → sequential requests handled by booking concurrency check (US_013)
  - No waitlisted patients → `WaitlistNotificationJob` no-ops without error
  - Network failure mid-reschedule → EF Core transaction rolls back atomically

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
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | TR-002 — PATCH cancel/reschedule endpoints |
| Database | PostgreSQL via Supabase | 15 | TR-003 — Appointment, AppointmentSlot atomic transaction |
| Background Jobs | Hangfire (PostgreSQL storage) | 1.8.x | NFR-008 — waitlist notification; reschedule PDF job |

---

## Task Overview
Add `PATCH /api/v1/appointments/{id}/cancel` and `PATCH /api/v1/appointments/{id}/reschedule` endpoints to `AppointmentsController`. Cancel validates the appointment is future-dated, sets status to "Cancelled", releases the slot (`isBooked = false`), enqueues `WaitlistNotificationJob`, and writes `APPOINTMENT_CANCELLED` audit. Reschedule runs inside a single EF Core transaction: release old slot, reserve new slot (with rowVersion concurrency check), update `slotId`, enqueue the PDF confirmation job (US_015), and write `APPOINTMENT_RESCHEDULED` audit. A concurrent slot conflict during reschedule raises `DbUpdateConcurrencyException`, the transaction rolls back, and HTTP 409 is returned.

## Dependent Tasks
- `task_002_backend-booking-endpoint.md` (US_013) — `AppointmentsController` scaffold; `IAuditLogService` wired
- `task_001_backend-hangfire.md` (US_003) — Hangfire server; job storage configured

## Impacted Components
- `backend/src/UPACIP.Application/Commands/Appointments/CancelAppointmentCommand.cs` — new command
- `backend/src/UPACIP.Application/Commands/Appointments/RescheduleAppointmentCommand.cs` — new command
- `backend/src/UPACIP.Infrastructure/BackgroundJobs/WaitlistNotificationJob.cs` — new Hangfire job
- `backend/src/UPACIP.API/Controllers/AppointmentsController.cs` — add cancel/reschedule actions

## Implementation Plan
1. Implement `CancelAppointmentHandler.Handle`:
   - Load `Appointment` with `AppointmentSlot`; verify `appointment.startDateTime > UtcNow`; if past → return HTTP 422
   - Set `appointment.status = "Cancelled"`, `slot.isBooked = false`; save
   - Enqueue `BackgroundJob.Enqueue<WaitlistNotificationJob>(j => j.ExecuteAsync(slotId))`
   - Write `APPOINTMENT_CANCELLED` audit entry
2. Implement `WaitlistNotificationJob.ExecuteAsync(slotId)`:
   - Query `WaitlistEntry` for `slotId`; if none → return (no-op)
   - For each entry: send notification via `IEmailService` (SMTP); log delivery status
3. Implement `RescheduleAppointmentHandler.Handle`:
   - Open EF Core transaction via `await context.Database.BeginTransactionAsync()`
   - Release old slot: `oldSlot.isBooked = false`
   - Reserve new slot: load `newSlot`; check `isBooked == false`; set `isBooked = true`; call `SaveChangesAsync` — catch `DbUpdateConcurrencyException` → rollback → return HTTP 409
   - Update `appointment.slotId = newSlotId`; save within same transaction
   - Commit transaction
   - Enqueue `GeneratePdfConfirmationJob` with `{ appointmentId, isReschedule: true }` (US_015)
   - Write `APPOINTMENT_RESCHEDULED` audit entry
4. Add `[HttpPatch("{id:guid}/cancel")]` and `[HttpPatch("{id:guid}/reschedule")]` actions in `AppointmentsController`; `[Authorize(Policy = "PatientPolicy")]`; map exceptions to appropriate HTTP status codes (422, 409)

## Current Project State
```
backend/
  src/
    UPACIP.API/Controllers/AppointmentsController.cs  (POST /appointments from US_013)
    UPACIP.Infrastructure/BackgroundJobs/             (AccountLockoutNotificationJob from US_010)
    UPACIP.Infrastructure/Audit/AuditLogService.cs
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/UPACIP.Application/Commands/Appointments/CancelAppointmentCommand.cs | Cancel command |
| CREATE | backend/src/UPACIP.Application/Commands/Appointments/RescheduleAppointmentCommand.cs | Reschedule command with transaction |
| CREATE | backend/src/UPACIP.Infrastructure/BackgroundJobs/WaitlistNotificationJob.cs | Hangfire job notifying waitlisted patients |
| MODIFY | backend/src/UPACIP.API/Controllers/AppointmentsController.cs | Add PATCH cancel and reschedule actions |

## External References
- [EF Core Transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions)
- [Hangfire — Background job enqueueing](https://docs.hangfire.io/en/latest/background-methods/calling-methods-in-background.html)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] PATCH cancel on future appointment → HTTP 200; `status = "Cancelled"`, `slot.isBooked = false`; audit entry present; Hangfire job enqueued
- [ ] PATCH cancel on past appointment → HTTP 422; appointment unchanged
- [ ] PATCH reschedule with available new slot → HTTP 200; old slot `isBooked = false`; new slot `isBooked = true`; single DB transaction
- [ ] PATCH reschedule with already-booked new slot → HTTP 409; both slots unchanged (verify via DB)

## Implementation Checklist
- [ ] Implement `CancelAppointmentHandler`: future-date guard (HTTP 422 for past); status="Cancelled"; slot released; audit written (AC-001, AC-003)
- [ ] Enqueue `WaitlistNotificationJob` after successful cancel; job no-ops if no waitlisted patients (AC-001)
- [ ] Implement `RescheduleAppointmentHandler` with EF Core transaction; `DbUpdateConcurrencyException` → HTTP 409 full rollback (AC-004, AC-005)
- [ ] Implement `WaitlistNotificationJob` (SMTP dispatch; handles zero waitlist entries gracefully) (AC-001)
- [ ] Write `APPOINTMENT_CANCELLED` and `APPOINTMENT_RESCHEDULED` audit entries in respective handlers (AC-001, AC-004)
- [ ] Enqueue `GeneratePdfConfirmationJob` after successful reschedule (AC-004, US_015 integration)
- [ ] Add PATCH actions to `AppointmentsController` with `[Authorize(Policy = "PatientPolicy")]` (AC-001)
