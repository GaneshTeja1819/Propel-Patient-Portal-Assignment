# Implementation Analysis -- task_002_backend-conflict-api.md

## Verdict

**Status:** Conditional Pass
**Summary:** TASK_002 delivers all three conflict management endpoints with correct routing, StaffPolicy enforcement, and xmin-based optimistic concurrency. ACs 1, 2, 4, and 5 pass cleanly. Four gaps prevent a full Pass: (1) `MarkReviewedHandler` has no status guard, allowing it to silently overwrite a Resolved conflict's status — a data integrity violation against AC-003; (2) `ResolveConflictRequest.AuthoritativeValue` has no input validation, allowing an empty canonical value to be persisted; (3) `ConflictRepository.TouchLastReviewedAtAsync` calls `_db.SaveChangesAsync()` directly, bypassing the `IUnitOfWork` abstraction and the audit interceptor; (4) no unit or integration tests were added. The three endpoint flows are architecturally sound and the migration is correct.

---

## Traceability Matrix

| Requirement / Acceptance Criterion | Evidence (file:fn/line) | Result |
|---|---|---|
| AC-001: GET returns severity, conflicting values, source document refs | `ConflictsController.GetConflicts()` L46; `DataConflictDto` record L183; `ConflictingValueDto` with `SourceDocumentId` L179 | Pass |
| AC-001: StaffPolicy enforced on GET | `[Authorize(Policy = "StaffPolicy")]` on class L18 | Pass |
| AC-001: isNew computed from lastConflictReviewedAt | `MapToDto()` L133: `isNew = lastReviewedAt is null \|\| c.CreatedAt > lastReviewedAt.Value` | Pass |
| AC-002: PATCH /resolve sets status="Resolved" | `ResolveConflictHandler.HandleAsync()` L51: `conflict.Status = "Resolved"` | Pass |
| AC-002: Stores canonical value | `ResolveConflictHandler` L52: `conflict.CanonicalValue = command.AuthoritativeValue` | Pass |
| AC-002: Writes immutable CONFLICT_RESOLVED audit | `ResolveConflictHandler` L58: `_audit.LogAsync(..., "CONFLICT_RESOLVED", ...)` | Pass |
| AC-002: HTTP 409 if already resolved | `ResolveConflictHandler` L49: `if (conflict.Status != "Open") throw InvalidOperationException` → controller maps to 409 L97 | Pass |
| AC-002: HTTP 409 on concurrent resolution (rowVersion) | `AppDbContext`: `xmin` shadow property with `IsRowVersion()`; `DbUpdateConcurrencyException` caught at L100 | Pass |
| AC-003: PATCH /mark-reviewed sets status="ReviewedUnresolved" | `MarkReviewedHandler.HandleAsync()` L38: `conflict.Status = "ReviewedUnresolved"` | Pass |
| AC-003: Writes CONFLICT_REVIEWED audit | `MarkReviewedHandler` L44: `_audit.LogAsync(..., "CONFLICT_REVIEWED", ...)` | Pass |
| AC-003: No status guard → can overwrite Resolved status | `MarkReviewedHandler` L36-50: no `if (conflict.Status != "Open")` check | **Fail** |
| AC-004: Empty patient → GET returns [] | `GetByPatientIdAsync` returns empty list; `Ok(dtos)` serializes as `[]` | Pass |
| AC-005: Patient PATCH → HTTP 403 | `[Authorize(Policy = "StaffPolicy")]` class-level attribute; all 3 endpoints inherit | Pass |
| Edge: Concurrent resolve → second HTTP 409 | xmin IsRowVersion + `DbUpdateConcurrencyException` → `Conflict(...)` at controller L100 | Pass |
| Edge: isNew=true after new document upload | `TouchLastReviewedAtAsync` sets `LastConflictReviewedAt = UtcNow`; `isNew` recomputed on next GET | Pass |
| DataConflict entity: Status, Severity, ConflictingValues, CanonicalValue, ResolvedById | `DataConflict.cs` L16-33 | Pass |
| EF migration: adds new columns, drops is_resolved | `20260520130000_ExtendDataConflict.cs` Up() + data migration SQL L57 | Pass |
| AppDbContext: xmin rowVersion, (PatientId,Status) index | `AppDbContext` DataConflict config: `HasRowVersion()` + `HasIndex(new{PatientId,Status})` | Pass |
| DI: handlers + repository registered as scoped | `DependencyInjection.cs` L62-64 | Pass |

---

## Logical & Design Findings

- **Business Logic:** `MarkReviewedHandler` has no guard against calling mark-reviewed on an already-`Resolved` conflict. A Staff member could call `PATCH /mark-reviewed` on a resolved conflict and silently revert its status to `ReviewedUnresolved`, clearing the semantic meaning of resolution. The fix: add `if (conflict.Status is "Resolved") throw InvalidOperationException("Cannot mark a resolved conflict as reviewed.")`. For `ReviewedUnresolved` → idempotent no-op (return without save).

- **Security:** `ResolveConflictRequest.AuthoritativeValue` has no `[Required]` attribute and no null/whitespace guard in the handler. A Staff user can submit `""` as the canonical value, storing an empty string as `CanonicalValue`. Fix: add `[Required, MinLength(1)]` to the request record and/or `if (string.IsNullOrWhiteSpace(command.AuthoritativeValue))` guard in `ResolveConflictHandler`.

- **Error Handling:** `DeserializeConflictingValues()` in `ConflictsController` swallows all JSON parse exceptions silently and returns an empty list with no logging. If `DataConflict.ConflictingValues` contains malformed JSON (written by `DeduplicationJob`), the conflict appears to have zero source values with no indication of the error. Fix: inject `ILogger<ConflictsController>` and log a warning with `conflictId` on deserialization failure.

- **Data Access:** `ConflictRepository.TouchLastReviewedAtAsync()` calls `_db.SaveChangesAsync(ct)` directly, bypassing the `IUnitOfWork` abstraction. This is the only repository method in the codebase that commits through the raw `DbContext` instead of `IUnitOfWork`. It creates a second separate transaction after the conflict status update, meaning a failure in `TouchLastReviewedAtAsync` leaves the conflict resolved but `LastConflictReviewedAt` un-updated (partial state). Fix: remove `_db.SaveChangesAsync()` from the repository method; have the handlers call a second `_uow.SaveChangesAsync(ct)` after `TouchLastReviewedAtAsync`.

- **Data Access:** `GetConflicts` makes two sequential DB round-trips (one for conflicts, one for `LastConflictReviewedAt`). These could be parallelized with `Task.WhenAll` or combined into a single projection query. Minor for MVP scale but worth noting.

- **Performance:** No pagination on `GET /patients/{id}/conflicts`. If a patient accumulates hundreds of conflicts, the response will be unbounded. Acceptable for current scale but consider `?limit=&offset=` as a future NFR.

- **Patterns & Standards:** Handler constructors inject `IConflictRepository` and `IUnitOfWork` correctly. DTOs are sealed records following codebase conventions. `ResolveActorId()` helper is duplicated across `ConflictsController` and `PatientDataController` — minor DRY violation; a base controller helper could extract it.

---

## Test Review

- **Existing Tests:** `backend/tests/UPACIP.Tests/IntegrationTests/AuditPermissionTest.cs` — covers audit table permissions only; no conflict-specific tests exist.

- **Missing Tests (must add):**
  - [ ] Unit: `ResolveConflictHandlerTests` — happy path sets status/canonicalValue/resolvedAt, calls audit; status != Open throws InvalidOperationException
  - [ ] Unit: `MarkReviewedHandlerTests` — happy path sets ReviewedUnresolved, calls audit; Resolved status guard (post-fix)
  - [ ] Unit: `ConflictsControllerTests` — `ResolveConflict` maps InvalidOperationException → 409; maps DbUpdateConcurrencyException → 409; maps KeyNotFoundException → 404
  - [ ] Integration: Concurrent PATCH /resolve on same conflict — one 204, one 409 (validates xmin guard end-to-end)
  - [ ] Negative/Edge: PATCH /resolve with empty `AuthoritativeValue` → 400 (post-fix)
  - [ ] Negative/Edge: Patient JWT calling PATCH /resolve → 403
  - [ ] Negative/Edge: PATCH /mark-reviewed on Resolved conflict → 409 (post-fix)

---

## Validation Results

- **Commands Executed:** `dotnet build UPACIP.sln` via `get_errors` on all three projects
- **Outcomes:** 0 compile errors, 0 warnings on Application, API, Infrastructure projects. Build: **PASS**
- **Runtime tests:** Not executed (no test infrastructure available for integration run)

---

## Fix Plan (Prioritized)

1. **Add status guard to MarkReviewedHandler** — `backend/src/UPACIP.Application/Handlers/Conflicts/MarkReviewedHandler.cs` — ETA 30 min — Risk: **H** (data integrity; Resolved conflicts can be re-opened silently)
2. **Input validation on AuthoritativeValue** — `backend/src/UPACIP.API/Controllers/ConflictsController.cs` (add `[Required]` + `[MinLength(1)]`), `backend/src/UPACIP.Application/Handlers/Conflicts/ResolveConflictHandler.cs` (null/whitespace guard) — ETA 15 min — Risk: **M** (data quality; empty CanonicalValue persisted)
3. **Fix TouchLastReviewedAtAsync to not call SaveChanges directly** — `backend/src/UPACIP.Infrastructure/Persistence/ConflictRepository.cs` (remove `_db.SaveChangesAsync`), `ResolveConflictHandler.cs` + `MarkReviewedHandler.cs` (add second `_uow.SaveChangesAsync` call) — ETA 30 min — Risk: **M** (pattern violation; partial transaction on failure)
4. **Log deserialization failures in DeserializeConflictingValues** — `backend/src/UPACIP.API/Controllers/ConflictsController.cs` — ETA 15 min — Risk: **L** (observability; silent data corruption)
5. **Add unit tests for handlers and controller** — `backend/tests/UPACIP.Tests/UnitTests/` (3 new files) — ETA 2 h — Risk: **L** (coverage; no regression protection)

---

## Appendix

- **Rules adopted:** `security-standards-owasp.md`, `backend-development-standards.md`, `dotnet-architecture-standards.md`, `language-agnostic-standards.md`, `code-anti-patterns.md`, `dry-principle-guidelines.md`, `performance-best-practices.md`
- **Search Evidence:** `grep "ConflictRepository"` → DI.cs L62 confirmed registration; `grep "data_conflicts"` → InitialCreate.cs L125 confirmed PascalCase column names (`IsResolved`) match migration SQL; `get_errors` on Application + API + Infrastructure → 0 errors
- **Context7 References:** Not fetched (ASP.NET Core 10 / EF Core patterns well-established in codebase; existing xmin pattern in `AppointmentSlot` used as source of truth)
