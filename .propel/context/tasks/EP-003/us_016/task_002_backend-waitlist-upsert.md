# Task - TASK_002

## Requirement Reference
- **User Story:** us_016
- **Story Location:** .propel/context/tasks/EP-003/us_016/us_016.md
- **Acceptance Criteria:**
  - AC-002: WaitlistEntry created with patientId, appointmentId, preferredSlotId, registeredAt; patient sees acknowledgement
  - AC-003: No preferredSlotId in booking payload → no WaitlistEntry created; no error
  - AC-004: Same patient + same preferredSlotId → upsert: update registeredAt; only one WaitlistEntry per patient-slot pair
- **Edge Cases:**
  - preferredSlotId == bookedSlotId on server side → validate and return HTTP 422 "Preferred slot must differ from booked slot"
  - WaitlistEntry upsert during concurrent requests → UNIQUE constraint on (patientId, preferredSlotId) with ON CONFLICT DO UPDATE handles race condition

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
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | TR-002 — WaitlistEntry persistence wired into booking handler |
| Database | PostgreSQL via Supabase | 15 | TR-003 — WaitlistEntry table with UNIQUE(patientId, preferredSlotId) |

---

## Task Overview
Extend `BookAppointmentHandler` (US_013) to accept an optional `PreferredSlotId` in the `BookAppointmentCommand`. If present (and not equal to `SlotId`), the handler upserts a `WaitlistEntry` record within the same booking transaction: `INSERT INTO WaitlistEntry ... ON CONFLICT (patientId, preferredSlotId) DO UPDATE SET registeredAt = EXCLUDED.registeredAt`. If `PreferredSlotId` is null, no `WaitlistEntry` is created and the booking proceeds as before. The booking response body is extended with `preferredSlotRegistered: bool` for the frontend acknowledgement.

## Dependent Tasks
- `task_002_backend-booking-endpoint.md` (US_013) — `BookAppointmentCommand` and handler must exist; upsert is added within same transaction

## Impacted Components
- `backend/src/UPACIP.Application/Commands/Appointments/BookAppointmentCommand.cs` — add nullable `PreferredSlotId` field
- `backend/src/UPACIP.Application/Handlers/Appointments/BookAppointmentHandler.cs` — add WaitlistEntry upsert logic
- `backend/src/UPACIP.Domain/Entities/WaitlistEntry.cs` — verify entity has UNIQUE constraint config
- `backend/src/UPACIP.Infrastructure/Persistence/Configurations/WaitlistEntryConfiguration.cs` — enforce UNIQUE index via EF Core Fluent API

## Implementation Plan
1. Add `Guid? PreferredSlotId` to `BookAppointmentCommand` record; document as optional
2. In `BookAppointmentHandler.Handle`: after the main slot reservation succeeds, check `command.PreferredSlotId.HasValue`; if null → skip WaitlistEntry creation; set `preferredSlotRegistered = false`
3. If `PreferredSlotId` == `SlotId` (booked slot) → return HTTP 422 with message "Preferred slot must differ from booked slot"; validate before opening transaction
4. If `PreferredSlotId` is set: within the same EF Core transaction, call `IWaitlistRepository.UpsertAsync(patientId, appointmentId, preferredSlotId)` — uses PostgreSQL `ON CONFLICT DO UPDATE` via raw EF Core ExecuteSql or custom upsert method; set `preferredSlotRegistered = true`
5. Ensure `WaitlistEntryConfiguration` has `.HasIndex(e => new { e.PatientId, e.PreferredSlotId }).IsUnique()` so the DB enforces the uniqueness constraint
6. Extend `BookAppointmentResponse` DTO with `preferredSlotRegistered: bool`

## Current Project State
```
backend/
  src/
    UPACIP.Application/Commands/Appointments/BookAppointmentCommand.cs   (from US_013)
    UPACIP.Application/Handlers/Appointments/BookAppointmentHandler.cs  (from US_013)
    UPACIP.Domain/Entities/WaitlistEntry.cs                             (from US_005)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | backend/src/UPACIP.Application/Commands/Appointments/BookAppointmentCommand.cs | Add nullable PreferredSlotId |
| MODIFY | backend/src/UPACIP.Application/Handlers/Appointments/BookAppointmentHandler.cs | WaitlistEntry upsert within booking transaction |
| CREATE | backend/src/UPACIP.Infrastructure/Persistence/Configurations/WaitlistEntryConfiguration.cs | UNIQUE index on (PatientId, PreferredSlotId) |

## External References
- [EF Core — HasIndex + IsUnique](https://learn.microsoft.com/en-us/ef/core/modeling/indexes)
- [PostgreSQL ON CONFLICT — Upsert](https://www.postgresql.org/docs/current/sql-insert.html#SQL-ON-CONFLICT)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] POST /api/v1/appointments with `preferredSlotId` set → HTTP 201; WaitlistEntry row exists; `preferredSlotRegistered: true`
- [ ] POST without `preferredSlotId` → HTTP 201; no WaitlistEntry row; `preferredSlotRegistered: false`
- [ ] POST with `preferredSlotId == slotId` → HTTP 422; appointment NOT created
- [ ] POST same `preferredSlotId` twice → only one WaitlistEntry; `registeredAt` updated to latest (AC-004)

## Implementation Checklist
- [ ] Add nullable `PreferredSlotId` to `BookAppointmentCommand` (AC-002, AC-003)
- [ ] Validate `PreferredSlotId ≠ SlotId` before transaction; HTTP 422 if equal (edge case)
- [ ] Upsert `WaitlistEntry` within booking transaction when `PreferredSlotId` present (AC-002)
- [ ] No WaitlistEntry created when `PreferredSlotId` null; no error (AC-003)
- [ ] `WaitlistEntryConfiguration` enforces UNIQUE(PatientId, PreferredSlotId); ON CONFLICT UPDATE `registeredAt` (AC-004)
- [ ] Extend response DTO with `preferredSlotRegistered: bool` (AC-002)
