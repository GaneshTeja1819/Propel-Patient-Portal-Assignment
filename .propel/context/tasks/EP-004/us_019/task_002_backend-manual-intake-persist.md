# Task - TASK_002

## Requirement Reference
- **User Story:** us_019
- **Story Location:** .propel/context/tasks/EP-004/us_019/us_019.md
- **Acceptance Criteria:**
  - AC-003: IntakeRecord created with method="Manual"; PHI fields encrypted; audit entry written
- **Edge Cases:**
  - All optional fields blank → persist with null optional columns; no validation error
  - Duplicate submit for same appointmentId → idempotency: return existing IntakeRecord ID; no duplicate row

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
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | TR-002 — POST /api/v1/intake/confirm handles both AI and Manual methods |
| Database | PostgreSQL via Supabase | 15 | TR-003 — IntakeRecord; null optional columns |
| Security | AES-256-GCM (.NET 8 built-in) | .NET 8 | NFR-001 — PHI field encryption |

---

## Task Overview
The `POST /api/v1/intake/confirm` endpoint implemented in US_018 task_003 already handles PHI encryption, IntakeRecord persistence, and audit. This task extends the handler to support `method = "Manual"`: the same `ConfirmIntakeHandler` accepts the full form payload from the manual form, skips the Redis session lookup (no AI session exists), encrypts PHI fields, and persists the `IntakeRecord` with `method = "Manual"`. Optional fields that are null are stored as null. Idempotency logic is shared with the AI path.

## Dependent Tasks
- `task_003_backend-intake-persist.md` (US_018) — `ConfirmIntakeHandler` must exist; this task only adds method="Manual" support

## Impacted Components
- `backend/src/UPACIP.Application/Handlers/Intake/ConfirmIntakeHandler.cs` — extend to handle method="Manual" path (no Redis session lookup required)

## Implementation Plan
1. In `ConfirmIntakeHandler.Handle`: check `command.method`; if `"Manual"` → skip `IntakeSessionService.GetSessionAsync` (no Redis session); use `command.capturedFields` directly as the source of truth
2. PHI encryption, persistence, and audit flow are identical to the AI path — no separate code path needed for these steps
3. For optional null fields: `IntakeRecord` columns for optional fields are already nullable (defined in US_005 migration); no null-stripping needed
4. Return HTTP 201 with `{ intakeRecordId }` — identical response shape as AI path

## Current Project State
```
backend/
  src/
    UPACIP.Application/Handlers/Intake/ConfirmIntakeHandler.cs  (from US_018 task_003)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | backend/src/UPACIP.Application/Handlers/Intake/ConfirmIntakeHandler.cs | Add Manual method path; skip Redis session lookup when method=Manual |

## External References
- [HIPAA minimum-necessary rule for PHI fields](https://www.hhs.gov/hipaa/for-professionals/privacy/guidance/minimum-necessary-requirement/index.html)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] POST /api/v1/intake/confirm with method="Manual" and full form values → HTTP 201; `IntakeRecord.method = "Manual"`; PHI fields encrypted; audit entry written
- [ ] POST with optional fields null → HTTP 201; IntakeRecord persisted with null optional columns; no validation error
- [ ] POST twice same appointmentId → HTTP 200 with existing ID; no duplicate row

## Implementation Checklist
- [x] Extend `ConfirmIntakeHandler` for `method = "Manual"`: skip Redis lookup; use `capturedFields` from command directly (AC-003)
- [x] PHI field encryption via `IPhiEncryptionService` (same path as AI — already implemented) (AC-003)
- [x] Optional null fields stored as null; no stripping (edge case)
- [x] Idempotency check shared with AI path (edge case)
- [x] Write `INTAKE_COMPLETED` audit entry with `method = "Manual"` (AC-003)
