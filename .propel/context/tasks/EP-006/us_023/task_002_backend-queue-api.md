# Task - TASK_002

## Requirement Reference
- **User Story:** us_023
- **Story Location:** .propel/context/tasks/EP-006/us_023/us_023.md
- **Acceptance Criteria:**
  - AC-001: GET /api/v1/queue/today returns all today's appointments in chronological order
  - AC-002: PATCH reorder saves new position; slot-conflict warning returned if applicable; each reorder written to audit
  - AC-003: DELETE with mandatory reason removes entry; Appointment status updated; reason stored in audit
  - AC-004: PATCH arrive updates status to "Arrived"; arrival timestamp + Staff actorId in audit
  - AC-005: Patient-role request to any arrive endpoint returns HTTP 403; no state change
- **Edge Cases:**
  - Two Staff mark same patient arrived simultaneously → second PATCH returns HTTP 409 "Patient already marked as arrived"; no duplicate audit entry
  - Staff removes patient already "Arrived" → endpoint accepts with reason and continues; audit records the override

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
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | TR-002 — queue management endpoints |
| Database | PostgreSQL via Supabase | 15 | TR-003 — Appointment; QueueEntry position column |

---

## Task Overview
Implement the Staff queue management API. `GET /api/v1/queue/today` returns all appointments for `UtcToday` ordered by `slot.startDateTime`. `PATCH /api/v1/queue/{id}/arrive` uses EF Core optimistic concurrency to prevent double-marking; returns HTTP 409 if already "Arrived". `PATCH /api/v1/queue/{id}/reorder` saves a `displayOrder` column on the appointment; detects time-slot conflicts and returns a `slotConflict: true` flag in the response body (override always applied). `DELETE /api/v1/queue/{id}` requires `reason` query param; soft-deletes the queue entry; updates `Appointment.status`. All mutating endpoints are `[Authorize(Policy = "StaffPolicy")]`; `[Authorize(Policy = "PatientPolicy")]` yields HTTP 403 on `arrive`.

## Dependent Tasks
- `task_002_backend-walkin-booking.md` (US_022) — `StaffBookingController` must exist; extend or create new `QueueController`
- `task_001_backend-jwt-auth.md` (US_007) — `StaffPolicy` must be registered

## Impacted Components
- `backend/src/UPACIP.API/Controllers/QueueController.cs` — new controller
- `backend/src/UPACIP.Application/Queries/Queue/GetTodaysQueueQuery.cs` — new query
- `backend/src/UPACIP.Application/Handlers/Queue/GetTodaysQueueHandler.cs` — new handler
- `backend/src/UPACIP.Domain/Entities/Appointment.cs` — add `displayOrder` (int, nullable) column

## Implementation Plan
1. Add `displayOrder` (nullable int) to `Appointment` entity; create EF Core migration
2. Create `GetTodaysQueueQuery` + handler: query `Appointment` where `slot.date == UtcToday`; order by `displayOrder` (if set), then `slot.startDateTime`; project to `QueueEntryDto { id, patientName, scheduledTime, status, displayOrder }`
3. Implement `PATCH /api/v1/queue/{id}/arrive` handler:
   - Load appointment; if `status == "Arrived"` → HTTP 409 with message "Patient already marked as arrived"; no write
   - Set `status = "Arrived"`, `arrivedAt = UtcNow`; save
   - Write `PATIENT_ARRIVED` audit entry: `actorRole = "Staff"`, `actorId = staffId`
4. Implement `PATCH /api/v1/queue/{id}/reorder` handler:
   - Accept `{ newIndex: int }` body; update `displayOrder` on appointment
   - Detect time-slot conflict: check if any appointment in the same `displayOrder` range has an overlapping `slot.startDateTime`; return `{ slotConflict: bool }` in response body (always save — override)
   - Write `QUEUE_REORDERED` audit entry with old and new positions
5. Implement `DELETE /api/v1/queue/{id}` handler:
   - Require non-empty `reason` query param; HTTP 422 if missing
   - Update `Appointment.status = "RemovedFromQueue"`; write `QUEUE_ENTRY_REMOVED` audit entry with reason
6. `QueueController`: all mutating actions `[Authorize(Policy = "StaffPolicy")]`; GET also Staff-only

## Current Project State
```
backend/
  src/
    UPACIP.API/Controllers/StaffBookingController.cs  (from US_022)
    UPACIP.Domain/Entities/Appointment.cs
    UPACIP.Infrastructure/Audit/AuditLogService.cs
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/UPACIP.API/Controllers/QueueController.cs | Queue management endpoints |
| CREATE | backend/src/UPACIP.Application/Queries/Queue/GetTodaysQueueQuery.cs | Today's queue query |
| CREATE | backend/src/UPACIP.Application/Handlers/Queue/GetTodaysQueueHandler.cs | Query handler |
| MODIFY | backend/src/UPACIP.Domain/Entities/Appointment.cs | Add displayOrder, arrivedAt columns |
| CREATE | backend/src/UPACIP.Infrastructure/Persistence/Migrations/[ts]_AddQueueColumns.cs | EF Core migration |

## External References
- [OWASP A01 — Broken Access Control](https://owasp.org/Top10/A01_2021-Broken_Access_Control/)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] GET /api/v1/queue/today returns today's appointments in order; 403 for Patient role
- [ ] PATCH arrive → status "Arrived"; audit entry written; second PATCH → HTTP 409
- [ ] PATCH reorder → `displayOrder` updated; `slotConflict: true` returned when overlap detected
- [ ] DELETE without reason → HTTP 422; with reason → appointment status updated; audit reason stored

## Implementation Checklist
- [ ] Add `displayOrder`, `arrivedAt` to `Appointment`; create migration (AC-001, AC-004)
- [ ] Implement `GetTodaysQueueQuery` handler: ordered by displayOrder then startDateTime (AC-001)
- [ ] PATCH arrive: HTTP 409 if already "Arrived"; set arrivedAt; write audit (AC-004, edge case)
- [ ] PATCH reorder: save displayOrder; detect + return `slotConflict`; write audit (AC-002)
- [ ] DELETE: require reason; HTTP 422 if absent; write audit with reason (AC-003)
- [ ] All mutating endpoints `[Authorize(Policy = "StaffPolicy")]`; arrive returns HTTP 403 for Patient role (AC-005)
