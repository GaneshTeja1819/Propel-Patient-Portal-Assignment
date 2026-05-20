---
task_id: task_002
us_id: us_027
review_date: 2026-05-20
reviewer: GitHub Copilot (Claude Sonnet 4.6)
sha: 44EA20933F50A16B37B589A235B1A6D083890B86DBC976707937A9A64E9CA383
verdict: Conditional Pass
score: 84
---

# Implementation Analysis — task_002_ai-deduplication.md

## Verdict

**Status:** Conditional Pass
**Score:** 84 / 100

Five of seven checklist items are fully implemented and correct. Two required fixes (FIX-001 idempotency race condition, FIX-002 VisitHistory never populated) must be resolved before production deployment. Three medium-priority fixes improve correctness and architecture alignment. Build is clean with zero compilation errors.

---

## Traceability Matrix

| Requirement / Acceptance Criterion | Evidence (file : fn / line) | Result |
|---|---|---|
| AC-001: `GET /me` (PatientPolicy, JWT sub only) | `ProfileController.cs` : `GetMyProfile()` L54 — `ResolvePatientId()` uses JWT `sub` claim only | ✅ Pass |
| AC-001: `GET /{patientId}` (StaffPolicy) | `ProfileController.cs` : `GetPatientProfile()` L79 — `[Authorize(Policy = "StaffPolicy")]` | ✅ Pass |
| AC-001: HTTP 403 on wrong role | ASP.NET Core policy middleware handles 403 automatically when policy fails | ✅ Pass |
| AC-001: Non-existent patient returns 404 | `ProfileController.cs` L92 — `result.PatientId != Guid.Empty` always `true`; 404 never returned | ❌ Fail |
| AC-003: Dedup merges overlapping entries (case-insensitive, normalised) | `GeminiDeduplicationAdapter.cs` — prompt instructs Gemini to normalise; `DeduplicationPrompt.json` rule 1 | ✅ Pass |
| AC-003: Canonical entry links to source documents | `MergedClinicalEntry.cs` `SourceDocumentIds` (JSON); `CanonicalEntry.SourceDocumentIds[]` in Gemini response | ✅ Pass |
| AC-003: PHI encryption on canonical entries | `AppDbContext.cs` L180 — `phiConverter` applied to `EncryptedLabel` + `EncryptedCanonicalValue` | ✅ Pass |
| AC-003: VisitHistory section populated | `DeduplicationJob.FlattenExtractions()` — `ClinicalExtractionResult` has no `VisitHistory` field; never generated | ❌ Gap |
| AC-005: Indexed query on PatientId | `20260520120000_AddMergedClinicalEntry.cs` — `IX_MergedClinicalEntry_PatientId` index created | ✅ Pass |
| AC-005: P95 ≤ 500 ms | `GetPatientProfileHandler.cs` L66 — `AsNoTracking()` + indexed WHERE; but SKIP/TAKE is in-memory (C# LINQ), not DB-level LIMIT/OFFSET | ⚠️ Risk |
| Edge: Dedup not completed → raw data fallback | `GetPatientProfileHandler.cs` L85-105 — reads `ExtractedClinicalData` when status != "Completed" | ✅ Pass |
| Edge: > 50 entries → pagination LIMIT 20 | `GetPatientProfileQuery.cs` default `PageSize=20`; `MaxPageSize=100`; applied via `Skip/Take` | ⚠️ Cross-section |
| Edge: Concurrent dedup → second exits | `DeduplicationJob.cs` L90-96 — status check present but TOCTOU race condition exists | ❌ Gap |
| Idempotency guard | `DeduplicationJob.cs` L91 — reads "Processing" and returns; no atomic CAS operation | ❌ Race |
| DEDUP_COMPLETED audit entry | `DeduplicationJob.SetCompletedAsync()` L237 — `_auditLog.LogAsync(Guid.Empty, "System", "DEDUP_COMPLETED", ...)` | ✅ Pass |
| Schema version validation (AIR-004) | `GeminiDeduplicationAdapter.cs` L66 — `SchemaVersionMismatchException` thrown on mismatch | ✅ Pass |
| Prompt injection fencing (OWASP A03, AIR-004) | `GeminiDeduplicationAdapter.cs` L59 — `<clinical_data>...</clinical_data>` fence | ✅ Pass |
| EF Core migration + index | `20260520120000_AddMergedClinicalEntry.cs` — table + FK + index; `AppDbContextModelSnapshot.cs` updated | ✅ Pass |

---

## Logical & Design Findings

### Business Logic

- **VisitHistory always empty**: `ClinicalExtractionResult` (from `GeminiExtractionAdapter.cs`) has `Vitals`, `Medications`, `Diagnoses` but no `VisitHistory` field. `DeduplicationJob.FlattenExtractions()` therefore never produces `VisitHistory` input items. Gemini receives no visit history data and the dedup output section is always empty. The `ProfileController` returns an empty `VisitHistory` list even after dedup completes. **Blockers the task requirement that all four clinical sections are displayed (AC-001).**

- **Pagination is cross-section**: `GetPatientProfileHandler` paginates the combined list of all `MergedClinicalEntry` records across all sections. Task edge case states "> 50 entries per section → 20 per page per section". A patient with 20 Vitals + 20 Medications on page 1 would see only Vitals (no Medications) if sorted by `SectionType` alphabetically. The `total` count is the aggregate across all sections, not per-section.

### Security

- **404 information disclosure (OWASP A01)**: `ProfileController.GetPatientProfile()` L92 — the condition `result.PatientId != Guid.Empty` is always `true` (PatientId is taken from the route input). The 404 branch is unreachable dead code. Staff querying a random GUID always receive `HTTP 200` with empty clinical data, rather than `404 Not Found`. This is a minor information disclosure: the response body shape implies "no data" vs "patient doesn't exist" without explicit status differentiation. Fix: check `user is null` in the handler and propagate a `UserNotFound` signal.

- **JWT sub claim fallback**: `ResolvePatientId()` falls back to `ClaimTypes.NameIdentifier` when `sub` is absent. This is defensive coding but may allow non-standard token issuers. Internal auth only (UPACIP.Infrastructure.Auth issues all tokens); external token issuers are out of scope per AIR-004.

### Error Handling

- **TOCTOU race condition in idempotency guard**: Two concurrent Hangfire executions for the same `patientId` can both call `FirstOrDefaultAsync` when `DeduplicationStatus = "Pending"`, both pass the idempotency check (L91), and both proceed to set "Processing" and call Gemini. Result: duplicate Gemini API calls, duplicate `DEDUP_COMPLETED` audit entries, and potential duplicate `MergedClinicalEntry` records if step-8 (RemoveRange) is interleaved. Fix: use a database-level atomic compare-and-swap (`UPDATE patient_profiles_360 SET deduplication_status = 'Processing' WHERE patient_id = @id AND deduplication_status != 'Processing'; SELECT ROW_COUNT`) or EF Core's `ExecuteUpdateAsync` with `rowsAffected` check.

- **Corrupt extraction silently skipped**: `DeduplicationJob.FlattenExtractions()` catches `JsonException` and skips corrupted records silently. This is correct behavior but no metric/counter is incremented. Observability gap.

### Data Access

- **In-memory LIMIT/OFFSET (Completed path)**: `GetPatientProfileHandler.cs` L66 — loads ALL `MergedClinicalEntry` records for a patient via `ToListAsync()`, then applies `Skip(skip).Take(pageSize)` in C#. For a patient with 200 merged entries, all 200 are fetched from PostgreSQL, then 20 are selected in memory. The correct implementation should use `.Skip(skip).Take(pageSize)` directly on the IQueryable before `ToListAsync()`.

- **In-memory processing (Fallback path)**: The fallback path deserializes ALL `ExtractedClinicalData` JSON blobs in memory before paginating. For patients with many documents, this could exceed memory thresholds. Acceptable for MVP but should be documented.

- **No cancellation token on RemoveRange load**: `DeduplicationJob.cs` L132 — `_db.MergedClinicalEntries.Where(m => m.PatientId == patientId).ToListAsync()` does not pass a `CancellationToken`. Minor.
- **In-memory processing (Fallback path)**: The fallback path deserializes ALL `ExtractedClinicalData` JSON blobs in memory before paginating. For patients with many documents this may exceed memory thresholds. Tagged as tech debt (no backlog story yet); acceptable for the current MVP milestone per the US-027 scope statement.

### Patterns & Standards

- **Layer violation — handler in Infrastructure**: `GetPatientProfileHandler` is in `UPACIP.Infrastructure.Handlers.Profile` namespace, not `UPACIP.Application.Handlers.Profile`. This violates Clean Architecture's rule that application use-cases live in the Application layer. The violation was necessary because `AppDbContext` (Infrastructure) would create a circular dependency if referenced from Application. Recommend introducing `IPatientProfileQueryRepository` interface in Application, with implementation in Infrastructure.

- **`MergedClinicalEntry` not `sealed`**: All other Infrastructure entities use `sealed` on job classes. Domain entities are typically not sealed. Minor style inconsistency.

- **`DeduplicationJob` depends directly on `AppDbContext`**: Consistent with `ClinicalDataExtractionJob` pattern. Acceptable for background jobs per the existing codebase convention.

### Performance

- **Database-level pagination not used**: The `Completed` path should apply `Skip/Take` to the LINQ IQueryable (before `ToListAsync`) so EF Core generates `LIMIT n OFFSET m` in SQL. Currently all rows are materialized before pagination. Fix: restructure the query to paginate first, then separate count query.

---

## Test Review

**Existing Tests:** `AuditPermissionTest.cs` — tests PostgreSQL GRANT/REVOKE on audit table only. No tests exist for any TASK_002 code.

**Missing Tests (must add):**

- [ ] Unit: `DeduplicationJob.ExecuteAsync` — idempotency: given `DeduplicationStatus = "Processing"` → exits without calling Gemini
- [ ] Unit: `DeduplicationJob.FlattenExtractions` — given two records with same medication → produces two input items with correct docIds
- [ ] Unit: `DeduplicationJob.ExecuteAsync` — given zero extractions → sets status "Completed" with empty entries
- [ ] Unit: `GeminiDeduplicationAdapter.CallDeduplicationAsync` — given wrong `schemaVersion` in response → throws `SchemaVersionMismatchException`
- [ ] Unit: `GetPatientProfileHandler.HandleAsync` — given `DeduplicationStatus = "Completed"` → returns `MergedClinicalEntry` data
- [ ] Unit: `GetPatientProfileHandler.HandleAsync` — given `DeduplicationStatus = "Pending"` → returns `ExtractedClinicalData` fallback
- [ ] Unit: `GetPatientProfileHandler.HandleAsync` — given page=2, pageSize=5 → correct SKIP/TAKE applied
- [ ] Unit: `ProfileController.GetMyProfile` — patient JWT → 200; staff JWT → 403
- [ ] Unit: `ProfileController.GetPatientProfile` — staff JWT → 200; patient JWT → 403
- [ ] Integration: `DeduplicationJob` — two overlapping medication names → one `MergedClinicalEntry` with both source document IDs (validation strategy item)
- [ ] Negative: `DeduplicationJob.ExecuteAsync` — Gemini throws → status set to "Pending" (retryable) not "Failed" on first attempt

---

## Validation Results

**Commands from task file:** None specified (deferred to separate build commands file).

**Build verification:**
```text
dotnet build UPACIP.sln
→ UPACIP.Domain: succeeded
→ UPACIP.Application: succeeded
→ UPACIP.Infrastructure: succeeded (1 pre-existing NU1903 warning)
→ UPACIP.Tests: succeeded
→ 0 error CS lines
```

---

## Fix Plan (Prioritized)

| # | Fix | Files / Functions | Effort | Risk |
|---|-----|-------------------|--------|------|
| FIX-001 | **Atomic idempotency guard** — replace TOCTOU check with `ExecuteUpdateAsync` that sets "Processing" only when current status is "Pending"; check `rowsAffected == 1` before proceeding | `DeduplicationJob.cs` : `ExecuteAsync()` L91 | 1 h | H |
| FIX-002 | **VisitHistory section** — add `VisitHistory` property to `ClinicalExtractionResult`; populate in `GeminiExtractionAdapter.ClinicalExtractionPrompt.json`; add mapping in `FlattenExtractions()` | `GeminiExtractionAdapter.cs`, `ClinicalExtractionPrompt.json`, `DeduplicationJob.FlattenExtractions()` | 2 h | H |
| FIX-003 | **404 for non-existent patient** — add `bool PatientExists` property to `PatientProfileDto`; set it from `user is null` in handler; controller returns 404 when `!result.PatientExists` | `GetPatientProfileHandler.cs`, `GetPatientProfileQuery.cs` (PatientProfileDto), `ProfileController.cs` L92 | 0.5 h | M |
| FIX-004 | **DB-level pagination** — move `Skip(skip).Take(pageSize)` to IQueryable before `ToListAsync()` in Completed path; add separate `.CountAsync()` for `total` | `GetPatientProfileHandler.cs` L66-80 | 0.5 h | M |
| FIX-005 | **Per-section pagination** — add `SectionType` filter parameter to `GetPatientProfileQuery`; or return per-section pagination metadata (`VitalsPagination`, `MedicationsPagination`, etc.) matching task edge case | `GetPatientProfileQuery.cs`, `GetPatientProfileHandler.cs`, `ProfileController.cs` | 2 h | M |
| FIX-006 | **Add unit + integration tests** — per missing test list above | New test files in `UPACIP.Tests` | 4 h | L |
| FIX-007 | **Architecture — handler in Application layer** — introduce `IPatientProfileQueryService` interface in Application; implement in Infrastructure | `UPACIP.Application/Interfaces/`, `UPACIP.Infrastructure/Handlers/Profile/` | 1 h | L |

---

## Appendix

### Rules Applied

- `rules/security-standards-owasp.md` — OWASP A01, A02, A03, A08 guardrails
- `rules/backend-development-standards.md` — idempotency, pagination, resilience, background-job SLA
- `rules/dotnet-architecture-standards.md` — SOLID, layer boundaries, DIP, async/await
- `rules/code-anti-patterns.md` — god objects, magic constants, circular deps
- `rules/language-agnostic-standards.md` — KISS, YAGNI, naming
- `rules/performance-best-practices.md` — indexed queries, N+1 prevention, async I/O

### Context7 References

- Not fetched (ASP.NET Core and EF Core patterns verified against codebase conventions directly)

### Search Evidence

```text
grep "DeduplicationStatus"  → PatientProfile360.cs (property), AppDbContext.cs (model config), DeduplicationJob.cs (state transitions), GetPatientProfileHandler.cs (status read)
grep "IX_MergedClinicalEntry"  → 20260520120000_AddMergedClinicalEntry.cs L49, AppDbContextModelSnapshot.cs, AppDbContext.cs
grep "VisitHistory"  → DeduplicationPrompt.json (output schema), GeminiDeduplicationAdapter.cs (sections), DeduplicationJob.cs (AddEntries), GetPatientProfileHandler.cs (MapMergedEntries)
grep "ClinicalExtractionResult"  → GeminiExtractionAdapter.cs (type definition — no VisitHistory field), DeduplicationJob.cs (FlattenExtractions deserialization)
grep "PatientPolicy\|StaffPolicy"  → ProfileController.cs L48,L73, Program.cs L121-122
```
