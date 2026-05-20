# Implementation Analysis — task_002_backend-manual-intake-persist.md

<!-- analyze-implementation workflow output -->
<!-- sha: 851E3D3A2CCDB39B750056E96BA7389E7101495B8A02611B382CD24B34E22538 -->
<!-- date: 2026-05-20 -->

## Verdict

**Status:** ⚠️ Conditional Pass

**Summary:** All three AC-003 acceptance criteria are functionally satisfied: `IntakeRecord.FormType` is set to `"Manual"` for the manual path, PHI is transparently encrypted via EF Core's `EncryptedStringConverter`, and an `INTAKE_COMPLETED` audit entry is written with `"method":"Manual"` in the metadata. Both edge cases (null optional fields and duplicate submit idempotency) are handled correctly. The implementation necessarily expanded scope beyond the task's single-file `Expected Changes` table — `ConfirmIntakeCommand.cs` and `IntakeController.cs` were also modified — which is correct and required. Two actionable gaps remain before production: (1) the `Method` field from the HTTP request body is not validated against an allowlist, allowing arbitrary strings to be stored in `FormType`; and (2) the string comparison is case-sensitive, meaning `"manual"` (lowercase) silently falls through to the AI path and persists with `FormType = "AI"`. No automated tests cover the new Manual path.

---

## Traceability Matrix

| Requirement / Acceptance Criterion | Evidence (file:fn/line) | Result |
|------------------------------------|------------------------|--------|
| **AC-003a** — IntakeRecord created with `method="Manual"` | `ConfirmIntakeHandler.cs` L77–84: `method = "Manual"` assigned when `command.Method == "Manual"`; stored as `FormType = method` at L116 | ✅ Pass |
| **AC-003b** — PHI fields encrypted | `AppDbContext.cs` L118–128: `EncryptedStringConverter` applied to `EncryptedFormData` on SaveChanges (AES-256-GCM) | ✅ Pass |
| **AC-003c** — Audit entry written | `ConfirmIntakeHandler.cs` L128–137: `_auditService.LogAsync(actionType: "INTAKE_COMPLETED", metadata: {…"method":"Manual"…})` | ✅ Pass |
| **Edge: All optional fields blank → no validation error** | `IntakeController.cs` L181: `request.CapturedFields?.Select(…).ToList() ?? []` handles null; empty list serialises to `"{}"` in `formDataJson` | ✅ Pass |
| **Edge: Duplicate submit → HTTP 200 + existing ID** | `ConfirmIntakeHandler.cs` L71–73: `FindByAppointmentIdAsync` returns existing; `WasAlreadyCompleted: true`; controller L189 returns `Ok(response)` | ✅ Pass |
| **Method value forwarded from request body** | `IntakeController.cs` L184: `Method: request.Method ?? "AI"` | ✅ Pass |
| **Redis session NOT called on Manual path** | `ConfirmIntakeHandler.cs` L77–84: `GetSessionAsync` is inside `else` branch only | ✅ Pass |
| **Redis clear NOT called on Manual path** | `ConfirmIntakeHandler.cs` L123–124: `if (command.Method != "Manual")` guard | ✅ Pass |
| **`Method` validated against allowlist** | No validation found in `IntakeController.cs` or `ConfirmIntakeHandler.cs` | ❌ Gap |
| **Case-insensitive `Method` comparison** | `ConfirmIntakeHandler.cs` L77: `== "Manual"` (ordinal, case-sensitive) | ⚠️ Gap |
| **Unit tests for Manual path** | No test files found under `UPACIP.Tests/` | ❌ Gap |

---

## Logical & Design Findings

### Business Logic

- **`Method` allowlist absent (MEDIUM):** `ConfirmIntakeRequest.Method` is `string?` with no `[AllowedValues]` annotation or controller-level guard. Any arbitrary string (e.g., `"Fax"`, `"Voice"`) will be accepted, converted via `request.Method ?? "AI"`, and stored as `IntakeRecord.FormType`. This corrupts reporting and audit integrity.

- **Case-sensitive `"Manual"` comparison (LOW):** `command.Method == "Manual"` uses ordinal case-sensitive equality. A client sending `"manual"` or `"MANUAL"` falls through to the AI path, calls `GetSessionAsync` (returns null — no session exists), then persists with `method = "AI"` instead of `"Manual"`. Both the `IntakeRecord.FormType` column and the audit entry will be wrong.

- **Scope expansion (OBSERVATION):** The task's `Expected Changes` table listed only `ConfirmIntakeHandler.cs`. The implementation correctly also modified `ConfirmIntakeCommand.cs` (added `Method` parameter) and `IntakeController.cs` (added `Method` to `ConfirmIntakeRequest` DTO and command construction). These changes are necessary and correct; the task's expected-changes list was incomplete.

- **Handler XML doc stale (LOW):** The class-level `<summary>` bullet 3 reads *"Loads the Redis session; falls back to the request payload on expiry"* — it does not acknowledge that the Manual path bypasses Redis entirely. The `HandleAsync` parameter XML doc similarly only describes the AI fallback scenario.

### Security

- **No `FormType` DB-level constraint:** `AppDbContext` does not add a check constraint on `intake_records.form_type` to restrict allowed values. A malformed request can insert any string. Mitigation: add `HasMaxLength(20)` + allowlist validation at the controller layer.

- **`Method` in audit metadata is set via string interpolation (F021, pre-existing):** `$"{{\"appointmentId\":\"{command.AppointmentId}\",\"method\":\"{method}\"}}"`— if `method` contained `"` the JSON would be malformed. For `"Manual"` this is safe, but the pattern is fragile. Tracked as F021.

- **Ownership check unchanged and correct:** `GetAppointmentPatientIdAsync` + `appointmentPatientId.Value != command.UserId` guard at Step 1 applies to all paths including Manual. No IDOR introduced.

### Error Handling

- **Audit failure is non-blocking:** `try/catch` around `_auditService.LogAsync` ensures a failed audit write does not roll back the persisted `IntakeRecord`. This is the correct pattern for audit non-criticality.

- **`SaveChangesAsync` is not in a try/catch:** If the DB write fails (e.g., constraint violation, connection loss), the exception propagates to the controller's outer `catch (InvalidOperationException)` handler — but only if it's an `InvalidOperationException`. Generic `DbUpdateException` is not caught at the controller; this would bubble as a 500. Pre-existing risk, not introduced by this task.

### Data Access

- **`FindByAppointmentIdAsync` uses `AsNoTracking()`:** Correct for a read-only idempotency check. Consistent with repository pattern.

- **No N+1 risk:** The handler makes exactly 2–3 sequential DB calls (GetAppointmentPatientId → FindByAppointment → AddAsync + SaveChanges). No loops over collections.

- **`fields.ToDictionary` — no duplicate key guard:** If `FallbackFields` contained two entries with the same `FieldKey`, `ToDictionary` would throw `ArgumentException`. Unlikely from the UI, but no defensive guard is present.

### Performance

- No performance concerns introduced. The Manual path removes one Redis call (`GetSessionAsync`), making it slightly more efficient than the AI path.

---

## Test Review

### Existing Tests

None found. `backend/src/UPACIP.Tests/` contains no `ConfirmIntakeHandler` test files (pre-existing gap from US_018 task_003, tracked as F011/F005 family).

### Missing Tests (must add)

- [ ] **Unit — Manual path happy path:** `HandleAsync` with `Method="Manual"` → `IntakeRecord.FormType == "Manual"`, no call to `GetSessionAsync`, no call to `ClearSessionAsync`
- [ ] **Unit — AI path regression:** `HandleAsync` with `Method="AI"` → `GetSessionAsync` is called, `ClearSessionAsync` is called
- [ ] **Unit — Idempotency (Manual):** `FindByAppointmentIdAsync` returns existing record → `WasAlreadyCompleted = true`, no `AddAsync` called
- [ ] **Unit — Ownership failure (Manual):** `GetAppointmentPatientIdAsync` returns different patient → `UnauthorizedAccessException` thrown
- [ ] **Unit — Empty FallbackFields:** `FallbackFields = []` → `formDataJson = "{}"`, persists successfully, no exception
- [ ] **Unit — Audit failure non-blocking:** `_auditService.LogAsync` throws → `IntakeRecord` still returned (no rethrow)
- [ ] **Negative — Invalid Method value:** `Method = "Fax"` → should reject with `400` (gap currently exists)
- [ ] **Integration — E2E Manual confirm:** `POST /api/v1/intake/confirm` with `Method="Manual"` body → HTTP 201; DB row has `form_type = 'Manual'`

---

## Validation Results

### Commands from Task File

| Scenario | Expected | Analysis Result |
|----------|----------|----------------|
| `POST /confirm` with `method="Manual"` + full form values | HTTP 201; `FormType="Manual"`; PHI encrypted; audit written | ✅ Logic verified by code trace |
| `POST /confirm` with optional fields null | HTTP 201; null optional columns; no error | ✅ `CapturedFields` null-coalesced to `[]`; `AppointmentId` is nullable FK |
| `POST /confirm` twice same `appointmentId` | HTTP 200 + existing ID; no duplicate row | ✅ Idempotency at Step 2; `WasAlreadyCompleted = true` |

*Note: No automated test execution was possible. Validation is code-trace only.*

---

## Fix Plan (Prioritized)

| # | Fix | File | Effort | Risk |
|---|-----|------|--------|------|
| 1 | Add `[AllowedValues("AI", "Manual")]` (or `IValidatableObject` / ModelState check) on `ConfirmIntakeRequest.Method`; return `HTTP 400` for unrecognised values | `IntakeController.cs` — `ConfirmIntakeRequest` DTO | 30 min | L |
| 2 | Change `command.Method == "Manual"` to `string.Equals(command.Method, "Manual", StringComparison.OrdinalIgnoreCase)` | `ConfirmIntakeHandler.cs` L77 | 5 min | L |
| 3 | Add unit tests covering Manual path, idempotency, empty fields, ownership failure, audit non-blocking | `UPACIP.Tests/` (new file `ConfirmIntakeHandlerTests.cs`) | 3–4 h | M |
| 4 | Update `ConfirmIntakeHandler` XML `<summary>` to document Manual bypass at step 3 | `ConfirmIntakeHandler.cs` L9–20 | 10 min | L |
| 5 | Add `HasMaxLength(20)` to `FormType` column in `AppDbContext` and add DB check constraint `form_type IN ('AI','AI-Partial','Manual')` | `AppDbContext.cs` + new migration | 30 min | M |

---

## Appendix

### Search Evidence

| Pattern | File | Purpose |
|---------|------|---------|
| `ConfirmIntakeCommand` | 3 files | Locate all command usage sites |
| `FindByAppointmentId` | `IIntakeRecordRepository.cs` L20, `IntakeRecordRepository.cs` L17 | Idempotency check chain |
| `IntakeRecord` (AppDbContext) | L118–130 | Verify EF Core encryption + nullable FK config |
| `FormType` (AppDbContext) | No match in model config | Confirms absence of column constraint |
| `UPACIP.Tests/**/*.cs` | No files found | Confirms zero test coverage |
