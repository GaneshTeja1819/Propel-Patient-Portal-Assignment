# Implementation Analysis — task_003_backend-intake-persist.md

## Verdict

**Status:** Conditional Pass
**Score:** 89 / 100
**Summary:** All AC-004 acceptance criteria and both edge cases are correctly implemented. The ownership check (CR-001 fix) is authoritative — it loads `appointment.PatientId` from the database and asserts equality with the JWT `sub` claim. PHI encryption is applied via EF Core's `EncryptedStringConverter` (AES-256-GCM) at DB write, covering the full form-data blob. The idempotency guard returns HTTP 200 with the existing record ID on a duplicate confirm. The Redis fallback path logs a `Warning` and persists the request-body fields correctly. Two issues block unconditional pass: (1) the idempotency check has a race-condition gap — no unique constraint on `intake_records.appointment_id`; (2) `InvalidOperationException` (appointment not found) is unhandled in the controller and surfaces as HTTP 500. Additionally, zero unit tests exist for `ConfirmIntakeHandler`.

---

## Traceability Matrix

| Requirement / Acceptance Criterion | Evidence (file:fn/line) | Result |
|---|---|---|
| AC-004: `IntakeRecord` created on confirm | `ConfirmIntakeHandler.cs` L100–113: `new IntakeRecord {...}` + `_repo.AddAsync` + `_uow.SaveChangesAsync` | ✅ Pass |
| AC-004: `method = "AI" \| "AI-Partial"` | `ConfirmIntakeHandler.cs` L96: `fields.Any(f => f.ManualRequired) ? "AI-Partial" : "AI"` → stored in `FormType` | ✅ Pass |
| AC-004: PHI fields encrypted before persistence | `AppDbContext.cs` L119–132: `EncryptedStringConverter` (AES-256-GCM) on `EncryptedFormData`; raw JSON passed by handler | ✅ Pass |
| AC-004: Audit entry written with `actorId` + timestamp | `ConfirmIntakeHandler.cs` L114–128: `_auditService.LogAsync(actorId: command.UserId, actionType: "INTAKE_COMPLETED", ...)` | ✅ Pass |
| Edge case: Redis expired → fallback to request body; log Warning | `ConfirmIntakeHandler.cs` L77–93: `GetSessionAsync` null → `LogWarning` → uses `command.FallbackFields` | ✅ Pass |
| Edge case: Duplicate confirm → HTTP 200 + existing ID | `ConfirmIntakeHandler.cs` L65–67 + `IntakeController.cs` L184–186: `FindByAppointmentIdAsync` → `WasAlreadyCompleted: true` → `Ok(response)` | ✅ Pass |
| OWASP A01: Caller must own the appointment | `ConfirmIntakeHandler.cs` L56–65: `GetAppointmentPatientIdAsync` + `!= command.UserId` → `UnauthorizedAccessException` → controller `Forbid()` | ✅ Pass |
| `[Authorize(Policy = "PatientPolicy")]` on action | `IntakeController.cs` class-level `[Authorize(Policy = "PatientPolicy")]` — inherited by all actions | ✅ Pass |
| Redis session cleared after successful persist | `ConfirmIntakeHandler.cs` L130: `ClearSessionAsync` called after `SaveChangesAsync` | ✅ Pass |

---

## Logical & Design Findings

**Business Logic:**
- The `method` derivation from `ManualRequired` flags is correct and consistent with how `IntakeSessionService` sets `ManualRequiredFields` (AC-002 fallback path).
- Empty `FallbackFields` (no fields captured at all) is valid — an `IntakeRecord` with an empty JSON object `{}` in `EncryptedFormData` will be persisted. No guard exists for this case; if the business requires at least one field to confirm, validation should be added.
- `IntakeRecord.IsReviewed` defaults to `false` (EF Core default). Not set explicitly, which is correct for a newly submitted record.

**Security:**
- Ownership check is authoritative — `appointment.PatientId` loaded from DB, not derived from session state. ✅
- PHI encryption via EF Core converter applies to the whole `EncryptedFormData` column. This is broader than per-field encryption (all data including non-PHI fields is encrypted), which is more conservative. ✅
- `EncryptedStringConverter` is only applied when `_encryptionService != null` (design-time constructor uses null). At runtime this is non-null (singleton registered in DI). No PHI would be stored unencrypted in production if DI is wired correctly.
- Audit metadata is constructed via raw string interpolation: `$"{{\"appointmentId\":\"{...}\"...}}"`. If `method` or `appointmentId` contained `"` characters, the JSON would be malformed. A `Guid` cannot contain quotes and `method` is only `"AI"` or `"AI-Partial"`, so no injection risk in practice — but using `JsonSerializer.Serialize(new {...})` is safer.

**Error Handling:**
- `InvalidOperationException("Appointment {id} does not exist.")` thrown by `HandleAsync` L59 is **not caught** by `IntakeController.ConfirmIntake`. The controller only catches `UnauthorizedAccessException`. An unknown appointment ID → HTTP 500 instead of HTTP 404. **Gap.**
- `AuditLogService.LogAsync` already wraps its body in `try/catch` and emits a Warning. The outer `try/catch` in `ConfirmIntakeHandler` L113–128 is redundant (it will only catch exceptions raised by the audit service constructor, not the method). This is harmless.
- `ClearSessionAsync` failure silently logs a warning (correct — session expires naturally via 5-min TTL).

**Data Access:**
- Idempotency guard: `FindByAppointmentIdAsync` (read) → `AddAsync` (write) is not atomic. Two concurrent requests can both see no existing record and both insert. **No unique constraint** on `intake_records.appointment_id` exists in `AppDbContext.OnModelCreating` (L119–132). This is a **race condition gap**.
- `FindByAppointmentIdAsync` uses `AsNoTracking()` — correct for a read-only idempotency check.
- `GetAppointmentPatientIdAsync` uses `AsNoTracking()` + projection (selects only `PatientId`) — efficient.
- Two DB reads (`GetAppointmentPatientIdAsync` + `FindByAppointmentIdAsync`) before the write. Under load this adds latency; could be combined into one query, but not a blocking issue.

**Frontend:** N/A (UI Impact = No)

**Performance:**
- No pagination concerns (single-record operation).
- Two sequential DB reads before the insert. Acceptable for a low-frequency confirm action.

**Patterns & Standards:**
- `ConfirmIntakeHandler` is in `UPACIP.Application` — correct layer (no EF Core references in Application). ✅
- `IIntakeRecordRepository` is a narrow, purpose-built interface (not the generic `IRepository<T>`). ✅
- DI registration: `IIntakeRecordRepository → IntakeRecordRepository` (Scoped), `ConfirmIntakeHandler` (Scoped). Matches EF Core `AppDbContext` lifetime. ✅
- `ConfirmIntakeHandler` injected directly into controller (not behind an interface). For a simple command handler without polymorphic requirements this is acceptable; no `IConfirmIntakeHandler` interface added — YAGNI.
- `ConfirmIntakeCommand` uses `IReadOnlyList<ConfirmIntakeCapturedField>` (typed) instead of `Dictionary<string, object?>` (task plan). This is a deliberate improvement — more type-safe and removes boxing.

---

## Test Review

**Existing Tests:**
- `SmokeTests/ApplicationLayerSmokeTest.cs` — assembly load and `IUnitOfWork` reflection only. Does not touch `ConfirmIntakeHandler`.
- `IntegrationTests/AuditPermissionTest.cs` — audit schema DB permission check. Unrelated to intake confirm flow.
- `SmokeTests/GeminiClientSmokeTest.cs` — Gemini client connectivity only.

**Missing Tests (must add):**

- [ ] Unit: `ConfirmIntakeHandler_HappyPath_Returns201WithNewId` — mock `IIntakeRecordRepository`, `IUnitOfWork`, `IIntakeSessionService`, `IAuditLogService`; assert `IntakeRecord` created, `method = "AI"`, audit called once
- [ ] Unit: `ConfirmIntakeHandler_Idempotency_ReturnsExistingId_NoSecondInsert` — `FindByAppointmentIdAsync` returns existing record; assert `AddAsync` never called, `WasAlreadyCompleted = true`
- [ ] Unit: `ConfirmIntakeHandler_RedisFallback_PersistsFallbackFields_LogsWarning` — `GetSessionAsync` returns null; `FallbackFields` provided; assert record persisted with fallback data and `LogWarning` called once
- [ ] Unit: `ConfirmIntakeHandler_OwnershipFail_ThrowsUnauthorized` — `appointmentPatientId != command.UserId`; assert `UnauthorizedAccessException` thrown
- [ ] Unit: `ConfirmIntakeHandler_AppointmentNotFound_ThrowsInvalidOperation` — `GetAppointmentPatientIdAsync` returns null; assert `InvalidOperationException` thrown
- [ ] Unit: `ConfirmIntakeHandler_ManualRequiredField_SetsMethodAIPartial` — any field with `ManualRequired = true`; assert `record.FormType == "AI-Partial"`
- [ ] Unit: `ConfirmIntakeHandler_AuditFailure_DoesNotAbortPersist` — `IAuditLogService.LogAsync` throws; assert `SaveChangesAsync` still called and result returned
- [ ] Integration: `POST /api/v1/intake/confirm` with valid JWT → HTTP 201 + `IntakeRecord` row in DB + audit row
- [ ] Integration: Duplicate confirm for same `appointmentId` → HTTP 200 + same `intakeRecordId`, no duplicate row
- [ ] Negative: `POST /confirm` with non-owning JWT → HTTP 403
- [ ] Negative: `POST /confirm` with non-existent `appointmentId` → HTTP 404 (requires fix #2 first)

---

## Validation Results

**Commands Executed:**

```
dotnet build UPACIP.Application  → Build succeeded (0 errors)
dotnet build UPACIP.Infrastructure → Build succeeded (0 errors, 2 NU1903 warnings)
```

**Outcomes:**

| Scenario | Expected | Actual |
|---|---|---|
| `UPACIP.Application` compiles clean | 0 errors | ✅ 0 errors |
| `UPACIP.Infrastructure` compiles clean | 0 errors | ✅ 0 errors |
| `UPACIP.API` compiles clean | 0 errors | ⚠️ 4 file-lock warnings (DLL held by running process PID 11696) — not compile errors |
| Unit tests pass | All green | ⚠️ No unit tests written for confirm flow |
| Integration: HTTP 201 on first confirm | HTTP 201 | Not tested (no DB available) |
| Integration: HTTP 200 on duplicate | HTTP 200 | Not tested |

---

## Fix Plan (Prioritized)

1. **Add unique constraint on `intake_records.appointment_id`** — `AppDbContext.cs`: `IntakeRecord` entity mapping — add `.HasIndex(r => r.AppointmentId).IsUnique()` inside `modelBuilder.Entity<IntakeRecord>`. Add EF Core migration. Risk: **High** (race condition prevents true idempotency under concurrency). Effort: 0.5h.

2. **Handle `InvalidOperationException` in `ConfirmIntake` controller action** — `IntakeController.cs` `ConfirmIntake`: add `catch (InvalidOperationException)` → return `NotFound(new { message = "Appointment not found." })`. Risk: **Medium** (HTTP 500 instead of 404 in prod). Effort: 0.25h.

3. **Write unit tests for `ConfirmIntakeHandler`** — new file `tests/UPACIP.Tests/UnitTests/ConfirmIntakeHandlerTests.cs` — 7 unit tests (see Test Review). Risk: **High** (no regression protection). Effort: 3h.

4. **Replace manual audit metadata JSON with `JsonSerializer.Serialize`** — `ConfirmIntakeHandler.cs` L125: replace string interpolation with `JsonSerializer.Serialize(new { appointmentId = command.AppointmentId, method }, JsonOpts)`. Risk: **Low**. Effort: 0.1h.

5. **Optional: `[Required]` on `ConfirmIntakeRequest.AppointmentId`** — adds explicit HTTP 400 for missing GUID rather than relying on `HasValidSubClaim` returning `false` → HTTP 403 (misleading). Risk: **Low**. Effort: 0.1h.

---

## Appendix

**Search Evidence:**

| Pattern | File | Key Matches |
|---|---|---|
| `ConfirmIntake` | `IntakeController.cs`, `ConfirmIntakeCommand.cs`, `DependencyInjection.cs` | 20+ matches — all wired |
| `IIntakeRecordRepository` | `IIntakeRecordRepository.cs`, `IntakeRecordRepository.cs`, `DependencyInjection.cs`, `ConfirmIntakeHandler.cs` | 6 matches — interface + impl + DI + usage |
| `ClearSessionAsync` | `IIntakeSessionService.cs`, `IntakeSessionService.cs` | Interface + implementation present |
| `intake_records` | `AppDbContext.cs` L121 | Table mapped; no unique index on `appointment_id` found |
| `ConfirmIntake` in test files | None | 0 matches — zero test coverage |
