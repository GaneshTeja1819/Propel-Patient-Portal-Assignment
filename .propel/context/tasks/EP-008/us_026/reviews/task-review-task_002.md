---
title: "Implementation Analysis — task_002_backend-extraction-retry-downstream"
task: task_002
user_story: us_026
epic: EP-008
reviewed_at: "2026-05-19"
verdict: Conditional Pass
---

# Implementation Analysis — task_002_backend-extraction-retry-downstream

## Verdict

**Status:** Conditional Pass

All six acceptance-criteria checklist items are implemented and the solution builds cleanly. One critical runtime blocker exists: an EF Core migration for the `ExtractionFailureNote` column was not created; without it the application will throw a PostgreSQL column-not-found error the first time the failure path executes. Additionally, the global `ExponentialBackOffRetryFilter` overrides the `[AutomaticRetry]` back-off delays (30 s / 300 s specified in AC-004 become 10 s / 60 s in practice), and the `RetryExtraction` endpoint lacks an audit entry and a concurrency guard. These issues must be addressed before deploying to any shared environment.

---

## Traceability Matrix

| Requirement / Acceptance Criterion | Evidence (file : fn / line) | Result |
|---|---|---|
| AC-004: `[AutomaticRetry(Attempts = 2, DelaysInSeconds = {30, 300})]` on extraction job | `ClinicalDataExtractionJob.cs` : class attribute L52 | **Pass** |
| AC-004: `extractionStatus = "Failed"` after all retries | `ClinicalDataExtractionJob.cs` : `SetFailedAsync()` L188–196 | **Pass** |
| AC-004: `extractionFailureNote` stored on failure | `ClinicalDocument.cs` : `ExtractionFailureNote` L33; `SetFailedAsync()` L190 | **Pass** |
| AC-004: retry CTA surfaced (UXR-603) via `GET /extraction-status` | `DocumentsController.cs` : `GetExtractionStatus()` L111–140 | **Pass** |
| AC-003 / AIR-007: `AI_INVOCATION` audit with `httpStatusCode` on failure | `GeminiInvocationLogger.cs` : `InvokeStructuredAsync()` finally block; `httpStatus = 500` on exception | **Pass** |
| AC-005: `DeduplicationJob` enqueued on success; extraction does not await | `ClinicalDataExtractionJob.cs` : `_client.Enqueue<DeduplicationJob>()` L149 | **Pass** |
| AC-005: `DeduplicationJob` stub created | `DeduplicationJob.cs` : `ExecuteAsync()` L24–27; registered in `DependencyInjection.cs` L126 | **Pass** |
| AC-002: PHI encryption on failure path (shared with task_001) | `SetFailedAsync` does not touch PHI; `ExtractionFailureNote` is non-PHI per entity doc | **Pass** |
| Edge: rate limit → `extractionStatus = "Failed"` with rate-limit message | General `catch (Exception)` in job captures rate-limit exceptions; `GetType().Name + ": " + ex.Message` stored | **Pass** |
| Edge: `SchemaVersionMismatchException` non-retriable, logged | `ClinicalDataExtractionJob.cs` : `catch (SchemaVersionMismatchException)` L155–161 | **Gap** — task edge case says "counted as retry attempt"; implementation swallows it immediately as non-retriable |
| Migration for `ExtractionFailureNote` column | `Migrations/` — only `InitialCreate` from 2026-05-17; no new migration present | **Fail** |
| `POST /retry-extraction` 409 guard on non-Failed status | `DocumentsController.cs` : `RetryExtraction()` L167–169 | **Pass** |
| `POST /retry-extraction` resets status + clears note + enqueues job | `DocumentsController.cs` : L176–181 | **Pass** |
| IDOR protection on both new endpoints | `patientId` filter on all DB queries (L120, L162) | **Pass** |

---

## Logical & Design Findings

### Business Logic

**F-001 (CRITICAL): Missing EF Core migration for `ExtractionFailureNote`**
`ClinicalDocument.ExtractionFailureNote` was added to the domain entity but no `dotnet ef migrations add` was run. The `AppDbContextModelSnapshot.cs` does not contain `ExtractionFailureNote`. At runtime, every write to this column (and every read that includes it in a SELECT *) will throw:

```
PostgresException: column "ExtractionFailureNote" does not exist
```

Affected paths: `SetFailedAsync()` on every failure, `GetExtractionStatus` projection, `RetryExtraction` null-clear.

**F-002 (MEDIUM): `[AutomaticRetry]` back-off delays are overridden by global filter**
`ExponentialBackOffRetryFilter` is registered as a global Hangfire filter via `UseFilter()`. Global filters run AFTER class-level attributes in Hangfire's `IElectStateFilter` pipeline; the last filter to set `context.CandidateState` wins. This means the 30 s / 300 s delays specified in `[AutomaticRetry]` are silently overridden by the global filter's 10 s / 60 s schedule. The retry count (2 retries = 3 total executions) is correct because the failure-detection logic reads `RetryCount` (tracked by `AutomaticRetryAttribute`) independently of `BackOffRetryCount` (tracked by the global filter). Only the delay timing diverges from AC-004.

**F-003 (MEDIUM — edge case divergence): `SchemaVersionMismatchException` treated as non-retriable**
The task edge case specifies this exception should be "counted as retry attempt; logged". The current implementation catches it before the general `Exception` catch and immediately calls `SetFailedAsync` without re-throwing, so Hangfire never schedules a retry. If the intent was truly non-retriable (reasonable for schema evolution), the task edge case wording should be updated. As written, there is a spec divergence.

**F-004 (MEDIUM): No audit entry on `POST /retry-extraction`**
When a patient triggers a retry, there is no `DOCUMENT_RETRY_REQUESTED` or `AI_INVOCATION` audit write. OWASP A09 (Security Logging) and AIR-007 require traceability for retry-induced re-invocations. The `UploadDocumentHandler` writes a `DOCUMENT_UPLOADED` audit for context; the retry endpoint should write an equivalent `EXTRACTION_RETRY_REQUESTED` entry.

**F-005 (LOW): Concurrent retry race condition (TOCTOU)**
The `RetryExtraction` endpoint reads `ExtractionStatus == "Failed"`, then writes `"Pending"`, in two separate database round-trips with no row-level lock or optimistic concurrency token. Two simultaneous retry requests can both pass the status guard and enqueue two competing `ClinicalDataExtractionJob` instances for the same document. This is a low-probability scenario (patient-facing UI) but could produce duplicate `ExtractedClinicalData` records.

### Security

- IDOR guard (`PatientId` filter) applied on both new endpoints — **compliant**.
- `ExtractionFailureNote` stores `GetType().Name + ": " + Message`; no PHI expected in Gemini exception messages, but if the message contains document content fragments, PHI could leak into an unencrypted column. Consider sanitizing to a static code (e.g., `"HttpRequestException: 429 Too Many Requests"` stripped of any body content).

### Error Handling

- `RetryExtraction` calls `_db.SaveChangesAsync(ct)` directly, bypassing `IUnitOfWork`. If the `AuditSaveChangesInterceptor` were to depend on `IUnitOfWork` state, this would silently skip the interceptor. Currently the interceptor is registered on `AppDbContext` directly, so this is safe — but it deviates from the established `IUnitOfWork` pattern used everywhere else in the codebase.
- `SetFailedAsync` does not handle `DbUpdateException` if EF cannot persist the failure state (e.g., because the `ExtractionFailureNote` column does not exist). The failure would be swallowed without the job being marked `Failed`. This compounds F-001.

### Data Access

- `GetExtractionStatus` correctly projects only `ExtractionStatus` and `ExtractionFailureNote` via `Select()` + `AsNoTracking()` — no N+1, no PHI over-fetch.
- `RetryExtraction` loads the full `ClinicalDocument` entity (tracked) but only updates two scalar fields. Acceptable given the small entity size.

### Patterns & Standards

- `ExtractionStatusResponse` is declared `internal sealed record` inside the controller file. Swashbuckle reflects on it via the `[ProducesResponseType(typeof(ExtractionStatusResponse))]` attribute in the same assembly, so OpenAPI generation works. However, promoting it to a `public` record in a shared DTO namespace would improve discoverability and align with Clean Architecture conventions.
- `DocumentsController` now injects `AppDbContext` directly. This bypasses the Application layer for the two new read/write operations, creating a thin Clean Architecture violation. A `GetExtractionStatusHandler` / `RetryExtractionHandler` pattern (matching `UploadDocumentHandler`) would be more consistent but was outside task scope.

---

## Test Review

### Existing Tests

| File | Type | Relevance |
|------|------|-----------|
| `SmokeTests/ApplicationLayerSmokeTest.cs` | Smoke | Verifies assembly load only; no coverage of new code |
| `SmokeTests/GeminiClientSmokeTest.cs` | Smoke | Gemini SDK wire-up; no coverage of retry or failure paths |
| `IntegrationTests/AuditPermissionTest.cs` | Integration | Audit table permissions; not relevant to extraction retry |

No tests exist for `ClinicalDataExtractionJob`, `DeduplicationJob`, or the two new controller endpoints.

### Missing Tests (must add)

- [ ] **Unit**: `ClinicalDataExtractionJob` — `ExecuteAsync` with `PerformContext` `RetryCount = 2` → asserts `ExtractionStatus == "Failed"` and `ExtractionFailureNote` set; does **not** re-throw.
- [ ] **Unit**: `ClinicalDataExtractionJob` — `ExecuteAsync` with `RetryCount = 0` and `RetryCount = 1` → asserts exception is re-thrown so Hangfire reschedules.
- [ ] **Unit**: `ClinicalDataExtractionJob` — `SchemaVersionMismatchException` → asserts `ExtractionStatus == "Failed"` immediately (no re-throw).
- [ ] **Unit**: `ClinicalDataExtractionJob` — successful extraction → asserts `DeduplicationJob` enqueued via `IBackgroundJobClient` mock; `ExtractionStatus == "Completed"`.
- [ ] **Unit**: `DocumentsController.GetExtractionStatus` — document belonging to a different patient returns 404 (IDOR test).
- [ ] **Unit**: `DocumentsController.RetryExtraction` — status `!= "Failed"` returns 409.
- [ ] **Unit**: `DocumentsController.RetryExtraction` — status `== "Failed"` resets to `"Pending"`, clears note, dispatches job.
- [ ] **Integration**: End-to-end Hangfire retry — inject a failing `GeminiExtractionAdapter` mock; confirm `ExtractionStatus` transitions `Processing → Failed` after two re-throws.
- [ ] **Negative/Edge**: `RetryExtraction` called twice concurrently — second call returns 409.

---

## Validation Results

**Commands Executed:**
```
dotnet build src/UPACIP.Infrastructure/UPACIP.Infrastructure.csproj
dotnet build src/UPACIP.API/UPACIP.API.csproj  (via get_errors Roslyn check)
```

**Outcomes:**

| Project | Result |
|---------|--------|
| `UPACIP.Domain` | ✅ Succeeded |
| `UPACIP.Application` | ✅ Succeeded |
| `UPACIP.Infrastructure` | ✅ Succeeded (0 compiler errors) |
| `UPACIP.API` | ✅ Succeeded (0 compiler errors; DLL-lock warnings are runtime-only and unrelated) |

No `dotnet test` was executed — test suite has no tests for the new code paths.

---

## Fix Plan (Prioritized)

| # | Fix | Files / Functions | Risk |
|---|-----|-------------------|------|
| 1 | **Create EF Core migration** for `ExtractionFailureNote` column: `dotnet ef migrations add AddExtractionFailureNote` | `Migrations/` (new file); `AppDbContextModelSnapshot.cs` (auto-updated) | **H** — blocks all runtime usage of the new column |
| 2 | **Resolve `[AutomaticRetry]` / global filter conflict** — either (a) remove the global `ExponentialBackOffRetryFilter` for this job class by checking for the `AutomaticRetryAttribute` in the filter, or (b) disable `[AutomaticRetry]` and instead rely solely on the global filter with the correct delays | `ExponentialBackOffRetryFilter.cs`; `ClinicalDataExtractionJob.cs` | **M** — AC-004 delay timings (30 s / 300 s) are not being honoured |
| 3 | **Add audit entry in `RetryExtraction`** — write `EXTRACTION_RETRY_REQUESTED` via `IAuditLogService` before dispatching the job | `DocumentsController.cs` : `RetryExtraction()` | **M** — AIR-007 / OWASP A09 |
| 4 | **Clarify `SchemaVersionMismatchException` handling** — if it should be retriable, remove the dedicated catch and let the general `catch (Exception)` handle it; update the edge case note in the task file | `ClinicalDataExtractionJob.cs` L155–161; task_002 edge case section | **L** — spec clarification needed |
| 5 | **Add concurrency guard to `RetryExtraction`** — use `_db.Database.ExecuteSqlRawAsync` with a conditional UPDATE (`WHERE extraction_status = 'Failed'`) and check rows-affected, or use an EF Core concurrency token on `ExtractionStatus` | `DocumentsController.cs` : `RetryExtraction()` | **L** — low-probability race |
| 6 | **Replace `_db.SaveChangesAsync()` with `_uow.SaveChangesAsync()`** in `RetryExtraction` | `DocumentsController.cs` L179 | **L** — pattern consistency |
| 7 | **Add test coverage** — implement the unit and integration tests listed in Test Review above | New test files under `tests/UPACIP.Tests/` | **L** — zero coverage on new paths |

---

## Appendix

### Rules Applied

- `rules/security-standards-owasp.md` — A01 IDOR guard verification, A09 logging gap
- `rules/backend-development-standards.md` — service/controller separation, UoW pattern
- `rules/dotnet-architecture-standards.md` — Clean Architecture layer boundaries
- `rules/code-anti-patterns.md` — magic constants, god objects
- `rules/dry-principle-guidelines.md` — delta update discipline
- `rules/language-agnostic-standards.md` — KISS, naming
- `rules/performance-best-practices.md` — AsNoTracking, projection

### Search Evidence

| Pattern | Purpose | Key Match |
|---------|---------|-----------|
| `ExtractionFailureNote` | Column existence in snapshot | Not found in `AppDbContextModelSnapshot.cs` — confirmed missing migration |
| `AutomaticRetry\|ExponentialBackOffRetryFilter` | Retry filter conflict analysis | Both present; global filter confirmed runs via `UseFilter()` in `HangfireServiceExtensions.cs` L55 |
| `GeminiInvocationLogger` | AI audit coverage | `httpStatusCode = 500` on all exceptions — confirmed in `GeminiInvocationLogger.cs` L65 |
| `DeduplicationJob\|ExtractionFailureNote` | DI registration and usage sweep | `DeduplicationJob` registered at `DependencyInjection.cs` L126; `ExtractionFailureNote` in 5 files |
