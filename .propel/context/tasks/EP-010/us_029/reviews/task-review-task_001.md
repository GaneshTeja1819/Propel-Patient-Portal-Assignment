---
task: task_001_ai-code-suggestion
us: us_029
reviewer: GitHub Copilot (analyze-implementation workflow)
reviewed_at: 2026-05-20
verdict: Conditional Pass
---

# Implementation Analysis — task_001_ai-code-suggestion.md

## Verdict

**Status:** Conditional Pass

**Summary:** The core ICD-10/CPT code suggestion pipeline is implemented correctly and aligns with the majority of acceptance criteria: the Hangfire job auto-triggers after extraction, the on-demand `POST /generate` endpoint enforces idempotency and StaffPolicy, the GeminiInvocationLogger decorator handles `AI_INVOCATION` audit automatically, and the `[AutomaticRetry(Attempts=1)]` failure path leaves `ExtractedClinicalData` unchanged. Two material gaps block an unconditional Pass: (1) AC-002 requires code-level validation of each `suggestedCode` against a reference codeset — only a prompt-level guardrail is present; and (2) Gemini's `derivedFromField` value is returned in the structured output but discarded rather than stored. Additionally, no unit or integration tests exist for the new job, adapter, or controller, leaving the implementation validation strategy section fully unverified.

---

## Traceability Matrix

| Requirement / Acceptance Criterion | Evidence (file : fn / line) | Result |
|---|---|---|
| **AC-001 auto-trigger** — Job enqueued after extraction completion | `ClinicalDataExtractionJob.cs` L153: `_client.Enqueue<CodeSuggestionJob>(...)` | Pass |
| **AC-001 on-demand** — Staff-only POST endpoint enqueues job | `CodeSuggestionsController.cs` L49–L78: `Generate()`, `[Authorize(Policy="StaffPolicy")]` | Pass |
| **AC-001 on-demand idempotency** — HTTP 409 on active Pending/Processing | `CodeSuggestionsController.cs` L66–L68: `Conflict(...)` | Pass |
| **AC-002 codeType stored** — "ICD10" / "CPT" mapped to `CodeSystem` | `CodeSuggestionJob.cs` L165: `CodeSystem = item.CodeType` | Pass |
| **AC-002 suggestedCode stored** | `CodeSuggestionJob.cs` L166: `SuggestedCode = item.Code` | Pass |
| **AC-002 reference codeset validation** — validated against reference codeset | Not implemented — no `MedicalCodeReferenceTable` query or `icd10_cpt_reference.json` deserialisation | **Gap** |
| **AC-002 rank stored** | `CodeSuggestionJob.cs` L170: `Rank = item.Rank` | Pass |
| **AC-002 confidenceScore stored** | `CodeSuggestionJob.cs` L168: `ConfidenceScore = item.ConfidenceScore` | Pass |
| **AC-002 derivedFrom FK (ClinicalDataId)** | `CodeSuggestionJob.cs` L163: `ClinicalDataId = extractedClinicalDataId` | Pass |
| **AC-002 derivedFromField string stored** | `CodeSuggestionItem.DerivedFromField` exists in DTO but never mapped to entity | **Gap** |
| **AC-003 zero suggestions — no records created** | `CodeSuggestionJob.cs` L137–L149: sentinel removed, `SaveChangesAsync`, early return | Pass |
| **AC-003 `CODE_SUGGESTION_EMPTY` audit written** | `CodeSuggestionJob.cs` L141: `_auditLog.LogAsync(..., "CODE_SUGGESTION_EMPTY", ...)` | Pass |
| **AC-004 AI_INVOCATION audit — modelVersion** | `GeminiInvocationLogger.cs` L32 + L77: `ModelVersion = "gemini-1.5-pro"` in metadata | Pass |
| **AC-004 AI_INVOCATION audit — promptHash** | `GeminiInvocationLogger.cs` L55, L77: SHA-256 of prompt computed and written | Pass |
| **AC-004 AI_INVOCATION audit — inputTokenCount / outputTokenCount** | `GeminiInvocationLogger.cs` L57–L63: from SDK UsageMetadata | Pass |
| **AC-004 AI_INVOCATION audit — responseLatencyMs** | `GeminiInvocationLogger.cs` L77: `sw.ElapsedMilliseconds` | Pass |
| **AC-004 AI_INVOCATION audit — httpStatusCode** | `GeminiInvocationLogger.cs` L57, L77: `httpStatus` variable | Pass |
| **AC-005 retry exactly once** | `CodeSuggestionJob.cs` L1: `[AutomaticRetry(Attempts = 1, DelaysInSeconds = new[] { 60 })]` | Pass |
| **AC-005 failure audit written** | `CodeSuggestionJob.cs` L209: `RemoveSentinelAndWriteFailedAuditAsync(...)`, `CODE_SUGGESTION_FAILED` | Pass |
| **AC-005 ExtractedClinicalData unchanged** | `CodeSuggestionJob.cs` — no writes to `ExtractedClinicalData` entity in any path | Pass |
| **AIR-003 structured output + schema validation** | `GeminiCodingAdapter.cs` L67–L72: `InvokeStructuredAsync<CodeSuggestionResult>`, schema version check | Pass |
| **AIR-006 PromptHash per record** | `CodeSuggestionJob.cs` L172: `PromptHash = _gemini.PromptHash` | Pass |
| **AIR-006 ModelVersion per record** | `CodeSuggestionJob.cs` L171: `ModelVersion = "gemini-1.5-pro"` | Pass |
| **Edge — concurrent job idempotency guard** | `CodeSuggestionJob.cs` L90–L100: `AnyAsync` on `Pending/Processing`, exit if found; sentinel row on start | Pass |
| **Edge — low-confidence suggestions stored** | No confidence filter in persist loop (`CodeSuggestionJob.cs` L155–L177) | Pass |
| **Edge — schema mismatch non-retriable** | `CodeSuggestionJob.cs` L182–L188: `catch (SchemaVersionMismatchException)` → `RemoveSentinelAndWriteFailedAuditAsync`, no rethrow | Pass |
| **Edge — "Regenerate" CTA on SCR-014** | `CodeSuggestionsController.cs` — endpoint available and returns `{ jobId }` | Pass |
| **OWASP A01 — Staff-only access** | `[Authorize(Policy = "StaffPolicy")]` on controller class | Pass |
| **OWASP A03 — prompt injection fencing** | `GeminiCodingAdapter.cs` L63: `<clinical_data>{...}</clinical_data>` fence | Pass |

---

## Logical & Design Findings

**Business Logic:**

- **GAP-001 (High): Reference Codeset Validation Absent** — AC-002 states `suggestedCode` must be "validated against reference codeset". The Implementation Plan (Step 3) explicitly describes querying `MedicalCodeReferenceTable` or deserialising `icd10_cpt_reference.json` per suggestion. The current implementation delegates this entirely to Gemini's prompt instruction ("Use only valid, recognised ICD-10-CM and CPT codes"). This is a prompt-level guardrail, not a code-level validation. Hallucinated or malformed codes will be persisted unchanged. Mitigation: load a static JSON reference (e.g., `IcdCptReference.json` embedded resource) and filter/flag codes not in the set — consistent with the existing embedded-resource pattern.

- **GAP-002 (Medium): `DerivedFromField` Discarded** — `CodeSuggestionItem.DerivedFromField` (`string`) captures which clinical field informed each suggestion (e.g., `"diagnoses"`, `"medications"`, `"vitals"`). This value is returned by Gemini but never mapped to any property on `MedicalCodeSuggestion`. Adding `DerivedFromField` to the entity and migration is required to preserve this traceability data for Staff review (the task refers to this in AC-002's field list).

- **GAP-003 (Low): `ConfidenceScore` Range Not Validated** — AC-002 specifies `0.00–1.00`. No guard exists to clamp or reject out-of-range values before persistence. A Gemini response with `confidenceScore: 1.5` would be stored as-is.

- `actorId` in `CODE_SUGGESTION_EMPTY` / `CODE_SUGGESTION_FAILED` audit entries is set to `clinicalData.PatientId` with `actorRole: "System"`. This is consistent with the pattern used by `ClinicalDataExtractionJob` and `DeduplicationJob`; acceptable.

**Security:**

- Prompt injection is fenced at `GeminiCodingAdapter.cs` L63. No PHI is present in logs (only IDs). `StaffPolicy` is enforced at the controller class level. No concerns.

**Error Handling:**

- `SchemaVersionMismatchException` is correctly non-retriable (caught separately, sentinel cleaned, `return` instead of rethrow).
- Generic `Exception` path re-throws on non-final attempt, allowing Hangfire's `AutomaticRetry` to back off 60 s. On final attempt, sentinel is cleaned and failure audit written. Pattern is correct.
- `LoadPromptTemplate()` throws `InvalidOperationException` if the embedded resource is missing — this surfaces at construction (DI resolution time), not silently at job execution. Good fail-fast behaviour.

**Data Access:**

- Sentinel row insert and later removal within the same `try` block is a valid in-flight marker approach. However, if the job server crashes between sentinel insert and sentinel removal, the sentinel row will remain in `Processing` state permanently, causing future runs to be blocked by the idempotency guard. A maintenance job or scheduled cleanup should remove stale `Processing` rows older than a TTL. This is a known trade-off documented in-class but not addressed in the task.
- `_uow.SaveChangesAsync()` is called after `_db.MedicalCodeSuggestions.Remove(sentinel)` inside the try block before the foreach — correct; zero-suggestions path saves the removal cleanly.
- N+1 risk: the `foreach` over `codingResult.Suggestions` calls `AddAsync` per item. For typical suggestion counts (< 20) this is acceptable; `AddRange` would be cleaner.

**Performance:**

- `AddAsync` in loop instead of `AddRangeAsync` — minor; Gemini returns ≤ 20 items in practice.
- The idempotency `AnyAsync` query hits `MedicalCodeSuggestions` filtered by `ClinicalDataId` and `Status`. The composite index `IX_MedicalCodeSuggestion_ClinicalDataId_Status` (added in task_002 migration) covers this exactly. ✅

**Patterns & Standards:**

- `GeminiCodingAdapter` correctly follows `GeminiExtractionAdapter` pattern: embedded JSON prompt, `LoadPromptTemplate()` static helper, `ComputePromptHash()` static helper, `PromptHash` property, `InvokeStructuredAsync<T>`, schema version check. ✅
- `CodeSuggestionJob` follows `ClinicalDataExtractionJob` / `DeduplicationJob` pattern: sentinel row, try/catch with non-retriable branch, `RemoveSentinelAndWriteFailedAuditAsync` helper. ✅
- **Low: `Enqueue` instead of `ContinueJobWith`** — Implementation Plan Step 6 specified `BackgroundJob.ContinueJobWith<CodeSuggestionJob>(extractionJobId, ...)`. The delivered code uses `_client.Enqueue<CodeSuggestionJob>(...)` (fire-and-forget). Functionally, `Enqueue` is called after `SaveChangesAsync`, so `ExtractedClinicalData` is committed before the job can run. The semantic difference (guaranteed ordering vs. best-effort) is low risk given `CodeSuggestionJob`'s null-check guard on `ExtractedClinicalData`. No defect, but a plan deviation to note.
- **Low: `CancellationToken` not propagated** — `CodeSuggestionJob.ExecuteAsync` receives no `CancellationToken` from `PerformContext`. `_gemini.SuggestCodesAsync(clinicalData.EncryptedExtractedJson)` uses `ct = default`. Hangfire's `PerformContext` exposes `context.CancellationToken.ShutdownToken` to allow graceful cancellation. Not propagating it means an in-flight Gemini call won't abort on server shutdown.

---

## Test Review

**Existing Tests:** None found for `CodeSuggestionJob`, `GeminiCodingAdapter`, or `CodeSuggestionsController`.

**Missing Tests (must add):**

- [ ] Unit: `GeminiCodingAdapterTests` — `SuggestCodesAsync` returns structured result; `SchemaVersionMismatchException` thrown when `schemaVersion` differs from `"1.0"`
- [ ] Unit: `CodeSuggestionJobTests` — idempotency guard exits when Pending/Processing set exists; sentinel is removed on success; zero suggestions writes `CODE_SUGGESTION_EMPTY` audit and returns; `SchemaVersionMismatchException` writes `CODE_SUGGESTION_FAILED` and does NOT rethrow; retryCount < MaxRetryAttempts rethrows for back-off
- [ ] Integration: `CodeSuggestionsControllerTests` — `POST /generate` returns 202 with `{ jobId }`; returns 404 when `ExtractedClinicalDataId` not found; returns 409 when active Pending/Processing set exists
- [ ] Negative/Edge: `CodeSuggestionJobTests` — concurrent job skipped by idempotency guard; stale Processing sentinel does not cascade to unrelated data

---

## Validation Results

**Commands Executed:** Build verification via `get_errors` on all backend projects.

**Outcomes:** 0 errors — build passes across `UPACIP.Domain`, `UPACIP.Application`, `UPACIP.Infrastructure`, `UPACIP.API`.

---

## Fix Plan (Prioritized)

1. **Add reference codeset validation** — `CodeSuggestionJob.cs` (foreach loop, L155–L177) + new embedded `IcdCptReference.json` in `UPACIP.Infrastructure/AI/` — ETA 3 h — Risk: **M** — Satisfies AC-002 "validated against reference codeset"; low-confidence suggestions remain stored; codes not found in reference set should be flagged (e.g., `ConfidenceScore` capped, log warning) rather than silently accepted.

2. **Add `DerivedFromField` to `MedicalCodeSuggestion` entity** — `MedicalCodeSuggestion.cs` (add `string DerivedFromField`), `AppDbContext.cs` (column config), new EF Core migration, `CodeSuggestionJob.cs` L163 block (map `item.DerivedFromField`) — ETA 1.5 h — Risk: **L** — Prevents silent data loss; Staff review screens can surface which clinical field drove each suggestion.

3. **Add unit + integration tests** — `backend/tests/UPACIP.Tests/` — new `CodeSuggestionJobTests.cs`, `GeminiCodingAdapterTests.cs`, `CodeSuggestionsControllerTests.cs` — ETA 4 h — Risk: **L** — Validates the four scenarios in the Implementation Validation Strategy.

4. **Clamp `ConfidenceScore` range** — `CodeSuggestionJob.cs` L168: add `Math.Clamp(item.ConfidenceScore, 0.0, 1.0)` — ETA 0.25 h — Risk: **L** — Enforces AC-002 `0.00–1.00` constraint.

5. **Propagate `CancellationToken` in `ExecuteAsync`** — `CodeSuggestionJob.cs` L129: add `CancellationToken ct = default` parameter; use `context?.CancellationToken.ShutdownToken ?? ct`; pass to `SuggestCodesAsync` — ETA 0.5 h — Risk: **L** — Enables graceful Hangfire server shutdown.

---

## Appendix

**Rules Applied:**

- `rules/security-standards-owasp.md` — A01 (StaffPolicy), A03 (prompt injection fence)
- `rules/backend-development-standards.md` — service/controller patterns, error handling
- `rules/dotnet-architecture-standards.md` — Clean Architecture layer boundaries, Hangfire job patterns
- `rules/code-anti-patterns.md` — magic constants (`"Pending"`, `"Processing"`, `"gemini-1.5-pro"`), AddAsync in loop
- `rules/dry-principle-guidelines.md` — adapter follows existing embedded-resource pattern exactly
- `rules/language-agnostic-standards.md` — KISS applied; no over-engineering observed
- `rules/performance-best-practices.md` — composite index covers idempotency query; AddAsync loop accepted at current scale

**Search Evidence:**

- `grep "SchemaVersionMismatchException"` → defined in `GeminiExtractionAdapter.cs` L148; accessible across Infrastructure assembly
- `grep "EncryptedExtractedJson.*HasConversion"` → `AppDbContext.cs` L172 — PHI value converter applied; field is decrypted on load
- `grep "MedicalCodeReferenceTable|icd10_cpt_reference"` → 0 matches — confirms reference codeset validation absent
- `grep "CodeSuggestionJob|GeminiCodingAdapter" backend/tests/` → 0 matches — confirms no tests exist
- `grep "DerivedFromField" backend/src/UPACIP.Domain/` → 0 matches — confirms field absent from entity
