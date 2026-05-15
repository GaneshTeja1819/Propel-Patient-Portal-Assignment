# Task - TASK_002

## Requirement Reference
- **User Story:** us_022
- **Story Location:** .propel/context/tasks/EP-006/us_022/us_022.md
- **Acceptance Criteria:**
  - AC-001: Appointment created for existing patient; createdByStaffId = Staff ID; slot booked; audit entry actorRole="Staff"
  - AC-002: Anon booking — no User record required; Appointment holds anonymous patient details; audit records anon nature
  - AC-003: Staff creates patient account; User record created; Appointment.patientId updated; audit records account creation + link; Staff actor attributed for both
  - AC-004: Audit log review shows actorRole="Staff" and actorId=staffId; no Patient actor attribution on booking event
- **Edge Cases:**
  - Duplicate patient email on account creation → HTTP 409 "Email already registered"; Staff informed; no duplicate User record
  - Network failure after slot reserved → atomic EF Core transaction rollback; slot released; HTTP 500 returned to client

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
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | TR-002 — walk-in booking endpoints; Staff-scoped policy |
| Database | PostgreSQL via Supabase | 15 | TR-003 — Appointment (nullable patientId); User creation |
| Security | BCrypt.Net-Next | Latest stable | NFR-004 — password hashing for newly created patient account |

---

## Task Overview
Implement three endpoints under `[Authorize(Policy = "StaffPolicy")]`:
1. `GET /api/v1/patients/search?q=` — patient type-ahead search (name/DOB)
2. `POST /api/v1/appointments/walk-in` — create walk-in appointment (existing or anonymous patient); sets `createdByStaffId`; uses same slot concurrency logic as `BookAppointmentHandler` (US_013)
3. `POST /api/v1/patients/create-from-walkin` — create patient `User` record and link to existing anonymous `Appointment`

All audit entries explicitly set `actorRole = "Staff"` and `actorId = staffUserId` (extracted from JWT claims). No patient actor attribution appears on Staff-initiated events.

## Dependent Tasks
- `task_002_backend-booking-endpoint.md` (US_013) — slot concurrency logic; `ISlotCacheService.InvalidateSlotAsync`
- `task_007_backend-jwt-auth.md` (US_007) — JWT claims extraction for Staff actorId

## Impacted Components
- `backend/src/UPACIP.API/Controllers/StaffBookingController.cs` — new controller
- `backend/src/UPACIP.Application/Commands/Appointments/WalkInBookingCommand.cs` — new command
- `backend/src/UPACIP.Application/Handlers/Appointments/WalkInBookingHandler.cs` — new handler
- `backend/src/UPACIP.Application/Commands/Patients/CreatePatientFromWalkInCommand.cs` — new command
- `backend/src/UPACIP.Application/Handlers/Patients/CreatePatientFromWalkInHandler.cs` — new handler

## Implementation Plan
1. Implement `GET /api/v1/patients/search?q=` in `StaffBookingController`: query `User` table by `firstName + lastName ILIKE '%{q}%'` or `dateOfBirth = parsed date`; return list `{ id, fullName, dob, email }`; `[Authorize(Policy = "StaffPolicy")]`
2. Create `WalkInBookingCommand`: `{ Guid? PatientId, string? AnonName, DateOnly? AnonDob, string? AnonPhone, Guid SlotId, Guid StaffActorId }`
3. Implement `WalkInBookingHandler`:
   - Validate `PatientId` XOR anon details present (not both null)
   - Reserve slot using same EF Core rowVersion concurrency as `BookAppointmentHandler`
   - Create `Appointment`: `patientId = command.PatientId` (nullable for anon), `createdByStaffId = command.StaffActorId`, anon fields stored in `Appointment.anonymousPatientDetails` (JSON column)
   - Write `WALKIN_APPOINTMENT_CREATED` audit entry: `actorRole = "Staff"`, `actorId = StaffActorId`
   - Call `ISlotCacheService.InvalidateSlotAsync(slotId)`
4. Create `CreatePatientFromWalkInCommand`: `{ Guid AppointmentId, string Email, string Password, string FirstName, string LastName, Guid StaffActorId }`
5. Implement `CreatePatientFromWalkInHandler`:
   - Check `User` table for existing email; if found → HTTP 409 "Email already registered"
   - Hash password with `BCrypt.HashPassword(password, workFactor: 12)`
   - Create `User` record with role = "Patient"; set `createdByStaffId = StaffActorId`
   - Update `Appointment.patientId = newUser.Id`
   - Write two audit entries: `PATIENT_ACCOUNT_CREATED` and `WALKIN_APPOINTMENT_LINKED`; both with `actorRole = "Staff"`, `actorId = StaffActorId`
   - Return HTTP 201 with `{ patientId, appointmentId }`

## Current Project State
```
backend/
  src/
    UPACIP.Application/Handlers/Appointments/BookAppointmentHandler.cs  (from US_013)
    UPACIP.Infrastructure/Security/PhiEncryptionService.cs
    UPACIP.Infrastructure/Auth/JwtAuthService.cs  (Staff policy configured)
    UPACIP.Domain/Entities/Appointment.cs
    UPACIP.Domain/Entities/User.cs
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/UPACIP.API/Controllers/StaffBookingController.cs | Staff walk-in endpoints (search, book, create account) |
| CREATE | backend/src/UPACIP.Application/Commands/Appointments/WalkInBookingCommand.cs | Walk-in booking command |
| CREATE | backend/src/UPACIP.Application/Handlers/Appointments/WalkInBookingHandler.cs | Slot reservation + Staff audit attribution |
| CREATE | backend/src/UPACIP.Application/Commands/Patients/CreatePatientFromWalkInCommand.cs | Account creation command |
| CREATE | backend/src/UPACIP.Application/Handlers/Patients/CreatePatientFromWalkInHandler.cs | User creation + Appointment link + dual audit |
| MODIFY | backend/src/UPACIP.Domain/Entities/Appointment.cs | Add createdByStaffId (nullable Guid) + anonymousPatientDetails (JSON) |

## External References
- [OWASP A01 Access Control — Role-based authorization](https://owasp.org/Top10/A01_2021-Broken_Access_Control/)
- [BCrypt.Net-Next password hashing](https://github.com/BcryptNet/bcrypt.net)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Staff POST walk-in for existing patient → Appointment.createdByStaffId set; audit actorRole="Staff"
- [ ] Staff POST anon walk-in → Appointment persisted with nullpatientId + anon details; no User record; audit logs anon nature
- [ ] Staff POST create account → User record created; Appointment.patientId updated; two audit entries (CREATED + LINKED); both actorRole="Staff"
- [ ] Duplicate email → HTTP 409; no duplicate User record

## Implementation Checklist
- [ ] `GET /api/v1/patients/search` with `[Authorize(Policy = "StaffPolicy")]`; case-insensitive search (AC-001)
- [ ] `WalkInBookingHandler`: slot reservation via EF Core concurrency; nullable patientId for anon; `WALKIN_APPOINTMENT_CREATED` audit with Staff actor (AC-001, AC-002, AC-004)
- [ ] Anon details stored in `Appointment.anonymousPatientDetails` JSON column; no User record required (AC-002)
- [ ] `CreatePatientFromWalkInHandler`: BCrypt password hash; duplicate email → HTTP 409 (AC-003, edge case)
- [ ] Dual audit entries (`PATIENT_ACCOUNT_CREATED` + `WALKIN_APPOINTMENT_LINKED`) with `actorRole = "Staff"` (AC-003, AC-004)
- [ ] Atomic transaction on slot reservation; rollback on failure; slot released (edge case)
- [ ] All endpoints `[Authorize(Policy = "StaffPolicy")]`; Patient cannot call these (OWASP A01)
