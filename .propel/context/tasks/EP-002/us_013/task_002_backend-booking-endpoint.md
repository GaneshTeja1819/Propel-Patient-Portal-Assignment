# Task - TASK_002

## Requirement Reference
- **User Story:** us_013
- **Story Location:** .propel/context/tasks/EP-002/us_013/us_013.md
- **Acceptance Criteria:**
  - AC-001: Appointment created with status "Booked"; slot marked booked; confirmation response returned
  - AC-002: No-show risk score computed (rule-based) and stored; default 0 on exception; no UI error surfaced
  - AC-003: Insurance validated against InsuranceRecord; result stored; "Validated" or "Not Recognised" indicator returned in response
  - AC-004: HTTP 409 returned when slot already booked; neither appointment nor slot changes
- **Edge Cases:**
  - Concurrent requests for same slot → optimistic concurrency via `rowVersion`; second request gets HTTP 409
  - Insurance blank → skip validation; store `insuranceValidationStatus = "NotProvided"`
  - Risk score computation throws exception → catch; log; store default 0; continue booking

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
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | TR-002 — POST /api/v1/appointments endpoint |
| Database | PostgreSQL via Supabase | 15 | TR-003 — Appointment, AppointmentSlot, InsuranceRecord tables |
| Caching | Upstash Redis | Serverless | NFR-002 — invalidate slot cache on booking |

---

## Task Overview
Implement `POST /api/v1/appointments` (Patient-scoped) using the CQRS pattern. The `BookAppointmentHandler` validates the target slot availability, acquires it via EF Core optimistic concurrency (`rowVersion`), performs a soft insurance validation against the `InsuranceRecord` table, computes a rule-based no-show risk score (with a safe default of 0), creates the `Appointment` entity, writes a `APPOINTMENT_CREATED` audit entry, enqueues the PDF confirmation Hangfire job (US_015), and returns HTTP 201 with the appointment ID. A concurrent booking for the same slot triggers EF Core `DbUpdateConcurrencyException`, resulting in HTTP 409.

## Dependent Tasks
- `task_001_backend-migrations.md` (US_005) — Appointment, AppointmentSlot, InsuranceRecord entities and tables must exist
- `task_002_backend-slots-endpoint.md` (US_012) — `ISlotCacheService.InvalidateSlotAsync` must be available

## Impacted Components
- `backend/src/UPACIP.Application/Commands/Appointments/BookAppointmentCommand.cs` — new command
- `backend/src/UPACIP.Application/Handlers/Appointments/BookAppointmentHandler.cs` — new handler
- `backend/src/UPACIP.Application/Services/NoShowRiskScorer.cs` — new risk scorer service
- `backend/src/UPACIP.API/Controllers/AppointmentsController.cs` — new controller (or action)

## Implementation Plan
1. Define `BookAppointmentCommand(Guid slotId, string? insuranceProvider, string? insuranceId)` record
2. Implement `BookAppointmentHandler.Handle`: load `AppointmentSlot` by `slotId`; if `isBooked == true` → return HTTP 409; otherwise set `isBooked = true` and call `SaveChangesAsync`; catch `DbUpdateConcurrencyException` → map to HTTP 409 result
3. Insurance soft-validation: if `insuranceProvider` and `insuranceId` not blank → query `InsuranceRecord` where `providerName == insuranceProvider`; match `insuranceId` against `insuranceIdPattern` regex; set `insuranceValidationStatus = "Validated" | "NotRecognised"`; if blank → `"NotProvided"`
4. No-show risk score: inject `INoShowRiskScorer`; call `ScoreAsync(patientId, slotDateTime)`; rule logic: +10 per previous no-show, −1 per day lead time, floor 0, cap 100; wrap in `try/catch`; default 0 on exception; log exception at Warning level
5. Create `Appointment` entity: `status = "Booked"`, `noShowRiskScore`, `insuranceValidationStatus`; persist via `IAppointmentRepository.AddAsync`
6. Call `ISlotCacheService.InvalidateSlotAsync(slotId)` so slot grid reflects "Unavailable" within the next 5-second poll
7. Write `APPOINTMENT_CREATED` audit entry via `IAuditLogService`
8. Enqueue Hangfire `GeneratePdfConfirmationJob` with `appointmentId`
9. Return HTTP 201 with `{ appointmentId, insuranceValidationStatus, slotId }`

## Current Project State
```
backend/
  src/
    UPACIP.Domain/Entities/AppointmentSlot.cs  (from US_005)
    UPACIP.Domain/Entities/InsuranceRecord.cs  (from US_005)
    UPACIP.Infrastructure/Caching/SlotCacheService.cs  (from US_003)
    UPACIP.Infrastructure/Audit/AuditLogService.cs  (from US_006)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/UPACIP.Application/Commands/Appointments/BookAppointmentCommand.cs | Booking command |
| CREATE | backend/src/UPACIP.Application/Handlers/Appointments/BookAppointmentHandler.cs | Booking handler with concurrency, insurance, risk score |
| CREATE | backend/src/UPACIP.Application/Services/NoShowRiskScorer.cs | Rule-based risk scorer (safe default 0) |
| CREATE | backend/src/UPACIP.API/Controllers/AppointmentsController.cs | POST /api/v1/appointments action |

## External References
- [EF Core Optimistic Concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)
- [Hangfire — Enqueue background job](https://docs.hangfire.io/en/latest/background-methods/calling-methods-in-background.html)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] POST with valid slot → HTTP 201; `Appointment` row exists with `status = "Booked"`, `noShowRiskScore` present
- [ ] POST with same slot from two simultaneous requests → one gets HTTP 201, other HTTP 409; only one appointment created
- [ ] POST with valid insurance → `insuranceValidationStatus = "Validated"` in response and DB
- [ ] Simulate risk scorer exception → appointment still created with `noShowRiskScore = 0`; Warning logged

## Implementation Checklist
- [x] Implement `BookAppointmentCommand` + `BookAppointmentHandler` with EF Core optimistic concurrency (`DbUpdateConcurrencyException` → HTTP 409) (AC-001, AC-004)
- [x] Soft insurance validation: query InsuranceRecord; regex match; set validation status; blank → NotProvided (AC-003)
- [x] Implement `NoShowRiskScorer` with safe try/catch default 0; log exception at Warning (AC-002)
- [x] Call `ISlotCacheService.InvalidateSlotAsync` after successful booking (US_012 AC-002 consistency)
- [x] Write `APPOINTMENT_CREATED` audit entry (AC-001)
- [x] Enqueue `GeneratePdfConfirmationJob` (US_015 trigger)
- [x] Return HTTP 201 with `{ appointmentId, insuranceValidationStatus }` (AC-001, AC-003)
