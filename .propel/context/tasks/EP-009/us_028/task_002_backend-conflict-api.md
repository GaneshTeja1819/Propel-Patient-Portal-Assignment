# Task - TASK_002

## Requirement Reference
- **User Story:** us_028
- **Story Location:** .propel/context/tasks/EP-009/us_028/us_028.md
- **Acceptance Criteria:**
  - AC-001: `GET /api/v1/patients/{id}/conflicts` returns conflicts with severity, conflicting values, source document references
  - AC-002: `PATCH /resolve` sets status="Resolved"; stores canonical value; writes audit; HTTP 409 if already resolved
  - AC-003: `PATCH /mark-reviewed` sets status="ReviewedUnresolved"; writes audit
  - AC-004: No conflicts exist → GET returns empty array (frontend decides not to render section)
  - AC-005: Patient-role request to any mutating endpoint → HTTP 403
- **Edge Cases:**
  - Two Staff members resolve same conflict simultaneously → second PATCH returns HTTP 409 "Conflict already resolved"; no duplicate `VerifiedMedicalCode` or duplicate audit entry
  - New document creates new conflicts after old ones resolved → `isNew = true` on new `DataConflict` records (based on `createdAt > lastResolvedAt`)

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
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | TR-002 — conflict management endpoints |
| Database | PostgreSQL via Supabase | 15 | TR-003 — DataConflict entity; optimistic concurrency |

---

## Task Overview
Implement conflict management API. `DataConflict` records are populated by `DeduplicationJob` (US_027). This task adds three endpoints:
- `GET /api/v1/patients/{id}/conflicts` — Staff + Patient (own only, read-only no mutations)
- `PATCH /api/v1/conflicts/{id}/resolve` — Staff only; sets `status = "Resolved"`; HTTP 409 on duplicate with optimistic concurrency row version check
- `PATCH /api/v1/conflicts/{id}/mark-reviewed` — Staff only; sets `status = "ReviewedUnresolved"`
All writes produce immutable audit entries. HTTP 403 for Patient on PATCH endpoints.

## Dependent Tasks
- `task_002_ai-deduplication.md` (US_027) — `DataConflict` entity populated by `DeduplicationJob`
- `task_001_backend-jwt-auth.md` (US_007) — `StaffPolicy` must be registered

## Impacted Components
- `backend/src/UPACIP.API/Controllers/ConflictsController.cs` — new controller
- `backend/src/UPACIP.Application/Commands/Conflicts/ResolveConflictCommand.cs` — new command
- `backend/src/UPACIP.Application/Commands/Conflicts/MarkReviewedCommand.cs` — new command
- `backend/src/UPACIP.Application/Handlers/Conflicts/ResolveConflictHandler.cs` — new handler

## Implementation Plan
1. Extend `DataConflict` entity (if not yet added in US_027): add `status` enum (Open / Resolved / ReviewedUnresolved), `canonicalValue`, `resolvedById`, `resolvedAt`, `isNew` (bool; set to `true` when created after patient's `lastConflictReviewedAt`)
2. `GET /api/v1/patients/{id}/conflicts`:
   - `[Authorize(Policy = "StaffOrPatientPolicy")]` — Patient can read own; Staff can read any
   - Compute `isNew` at query time: `conflict.createdAt > patient.lastConflictReviewedAt`
   - Returns `DataConflictDto`: `{ id, severity, conflictingValues[{value, sourceDocumentId}], status, isNew }`
3. `PATCH /api/v1/conflicts/{id}/resolve`:
   - `[Authorize(Policy = "StaffPolicy")]`; HTTP 403 for Patient (AC-005)
   - Check `conflict.status != "Open"` → HTTP 409 "Conflict already resolved or reviewed"
   - Optimistic concurrency: EF Core rowversion check; DB exception on concurrent update → HTTP 409
   - Update `status = "Resolved"`, `canonicalValue = request.authoritativeValue`, `resolvedById`, `resolvedAt`
   - Write immutable `CONFLICT_RESOLVED` audit entry (actorRole = "Staff", targetId = conflictId)
4. `PATCH /api/v1/conflicts/{id}/mark-reviewed`:
   - `[Authorize(Policy = "StaffPolicy")]`; HTTP 403 for Patient
   - Update `status = "ReviewedUnresolved"`
   - Write immutable `CONFLICT_REVIEWED` audit entry
5. Update `Patient.lastConflictReviewedAt = UtcNow` after any Staff action (for `isNew` calculation)

## Current Project State
```
backend/
  src/
    UPACIP.Domain/Entities/DataConflict.cs  (created by DeduplicationJob, US_027)
    UPACIP.Infrastructure/Audit/AuditLogService.cs
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/UPACIP.API/Controllers/ConflictsController.cs | Conflict management endpoints |
| CREATE | backend/src/UPACIP.Application/Commands/Conflicts/ResolveConflictCommand.cs | Resolve conflict command |
| CREATE | backend/src/UPACIP.Application/Commands/Conflicts/MarkReviewedCommand.cs | Mark reviewed command |
| CREATE | backend/src/UPACIP.Application/Handlers/Conflicts/ResolveConflictHandler.cs | Resolve with concurrency guard + audit |
| MODIFY | backend/src/UPACIP.Domain/Entities/DataConflict.cs | Add status, canonicalValue, rowVersion columns |

## External References
- [EF Core optimistic concurrency — rowversion](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)
- [OWASP A01 — Access Control](https://owasp.org/Top10/A01_2021-Broken_Access_Control/)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Two Staff PATCH /resolve same conflict concurrently → one succeeds; other gets HTTP 409
- [ ] Patient PATCH /resolve → HTTP 403
- [ ] GET /conflicts after new document upload → new conflict has `isNew = true`
- [ ] Resolved conflict writes immutable audit entry; no UPDATE/DELETE permitted on audit table

## Implementation Checklist
- [ ] `DataConflict` entity extended with status, canonicalValue, rowVersion (edge case — concurrency)
- [ ] GET endpoint returns `isNew` flag per conflict (edge case — "New" badge support)
- [ ] PATCH /resolve: HTTP 409 on already-resolved or rowversion conflict; HTTP 403 for Patient (AC-002, AC-005)
- [ ] PATCH /mark-reviewed: HTTP 403 for Patient; immutable audit (AC-003, AC-005)
- [ ] All audit entries immutable (no UPDATE/DELETE on audit table) (AC-002, AC-003)
