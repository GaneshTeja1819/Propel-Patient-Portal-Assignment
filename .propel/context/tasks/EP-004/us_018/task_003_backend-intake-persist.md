# Task - TASK_003

## Requirement Reference
- **User Story:** us_018
- **Story Location:** .propel/context/tasks/EP-004/us_018/us_018.md
- **Acceptance Criteria:**
  - AC-004: Patient confirms summary → IntakeRecord created with method="AI" (or "AI-Partial"); PHI fields encrypted before persistence; audit entry written
- **Edge Cases:**
  - Session state expired from Redis before confirm → load partial state from request body fallback; persist what was captured; log Warning
  - Duplicate confirm request (double-tap) → idempotency check: if IntakeRecord already exists for `appointmentId` return HTTP 200 with existing record ID

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
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | TR-002 — POST /api/v1/intake/confirm |
| Database | PostgreSQL via Supabase | 15 | TR-003 — IntakeRecord table |
| Security | AES-256-GCM (.NET 8 built-in) | .NET 8 | NFR-001, DR-001 — PHI field encryption before persistence |

---

## Task Overview
Implement `POST /api/v1/intake/confirm` in `IntakeController`. The handler loads the final session state from Redis (falls back to the request body payload if Redis has expired), encrypts all PHI fields via `IEncryptionService`, persists the `IntakeRecord` to the database, clears the Redis session key, and writes an `INTAKE_COMPLETED` audit entry. An idempotency check prevents duplicate `IntakeRecord` creation for the same `appointmentId`.

## Dependent Tasks
- `task_002_ai-gemini-intake-session.md` (US_018) — `IntakeSessionService` and Redis session must exist
- `task_001_backend-phi-encryption.md` (US_006) — `IEncryptionService` must be registered
- `task_001_backend-migrations.md` (US_005) — `IntakeRecord` entity and table must exist

## Impacted Components
- `backend/src/UPACIP.Application/Commands/Intake/ConfirmIntakeCommand.cs` — new command
- `backend/src/UPACIP.Application/Handlers/Intake/ConfirmIntakeHandler.cs` — new handler
- `backend/src/UPACIP.API/Controllers/IntakeController.cs` — add POST /confirm action (extends task_002)

## Implementation Plan
1. Define `ConfirmIntakeCommand(Guid appointmentId, Dictionary<string, object?> capturedFields, string method)` — `capturedFields` is the fallback payload if Redis has expired
2. Implement `ConfirmIntakeHandler.Handle`:
   - Idempotency: check if `IntakeRecord` already exists for `appointmentId`; if yes → return HTTP 200 with `{ intakeRecordId }` (no re-insertion)
   - Load session from Redis via `IntakeSessionService.GetSessionAsync`; if null → use `command.capturedFields` fallback; log Warning
   - Identify PHI fields (chiefComplaint, allergies, currentMedications, notes etc.); encrypt each with `IEncryptionService.Encrypt(string)` (AES-256-GCM, synchronous)
   - Create `IntakeRecord`: `appointmentId`, `method`, all fields, `completedAt = UtcNow`; persist
   - Delete Redis session key via `IntakeSessionService.ClearSessionAsync(appointmentId)`
   - Write `INTAKE_COMPLETED` audit entry with `actorId` and `appointmentId`
   - Return HTTP 201 with `{ intakeRecordId }`
3. Add `POST /api/v1/intake/confirm` to `IntakeController`; `[Authorize(Policy = "PatientPolicy")]`

## Current Project State
```
backend/
  src/
    UPACIP.API/Controllers/IntakeController.cs  (from task_002)
    UPACIP.Infrastructure/Security/AesEncryptionService.cs  (from US_006)
    UPACIP.Domain/Entities/IntakeRecord.cs  (from US_005)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/UPACIP.Application/Commands/Intake/ConfirmIntakeCommand.cs | Confirm intake command |
| CREATE | backend/src/UPACIP.Application/Handlers/Intake/ConfirmIntakeHandler.cs | PHI encryption + IntakeRecord persistence + audit |
| MODIFY | backend/src/UPACIP.API/Controllers/IntakeController.cs | Add POST /confirm action |

## External References
- [AES-256-GCM in .NET 8](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm)
- [HIPAA PHI encryption requirements](https://www.hhs.gov/hipaa/for-professionals/security/guidance/index.html)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] POST /api/v1/intake/confirm → HTTP 201; `IntakeRecord` row exists with encrypted PHI fields; audit entry present
- [ ] POST confirm twice for same appointmentId → HTTP 200 with existing `intakeRecordId`; no duplicate row
- [ ] Expire Redis session before confirm; include fields in request body → record still created; Warning logged

## Implementation Checklist
- [x] Idempotency check: existing IntakeRecord for appointmentId → HTTP 200; no re-insert (edge case)
- [x] Load session from Redis; fallback to request body `capturedFields` if expired (edge case)
- [x] Encrypt PHI fields via `IEncryptionService.Encrypt(string)` before persistence (AC-004, NFR-001)
- [x] Create `IntakeRecord` with `method = "AI" | "AI-Partial"`; persist (AC-004)
- [x] Clear Redis session key on successful persistence (cleanup)
- [x] Write `INTAKE_COMPLETED` audit entry (AC-004)
- [x] `[Authorize(Policy = "PatientPolicy")]` on confirm action (OWASP A01)
