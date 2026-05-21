---
id: task-review-task_001
task: task_001_ai-clinical-extraction-job
us: us_026
epic: EP-008
reviewer: GitHub Copilot
date: 2026-05-19
status: conditional-pass
---

# Implementation Analysis -- task_001_ai-clinical-extraction-job.md

## Verdict

**Status:** Conditional Pass

**Summary:** All three acceptance criteria (AC-001, AC-002, AC-003) are met by the implementation. Every required extraction pipeline stage is present — `ClinicalDocument` status transitions are correct, PdfPig text extraction is wired correctly, Gemini structured output is called via the versioned `ClinicalExtractionPrompt.json` prompt, PHI encryption is satisfied transparently through EF Core's `EncryptedStringConverter`, and `AI_INVOCATION` audit entries are written automatically by the existing `GeminiInvocationLogger` decorator for every `IGeminiClient` call. All three edge cases (image-only PDF, schema version mismatch, Gemini rate limit) are handled. Seven files were changed — three more than the four listed in Expected Changes — because `DownloadAsync` had to be added to `IDocumentStorageService` and implemented in `SupabaseStorageService`; these additions are necessary and architecturally correct. The implementation is blocked from a **full Pass** by the absence of any automated tests; the remaining gaps (token budget cap, idempotency guard) are Medium and Low severity respectively.

---

## Traceability Matrix

| Requirement / Acceptance Criterion | Evidence (file : symbol / line) | Result |
|---|---|---|
| AC-001: `extractionStatus` Pending → Processing | `ClinicalDataExtractionJob.cs` : `ExecuteAsync()` L70 | Pass |
| AC-001: PDF text extraction via PdfPig | `ClinicalDataExtractionJob.cs` : `IPdfTextExtractor.ExtractText()` L82 | Pass |
| AC-001: Gemini structured extraction call | `GeminiExtractionAdapter.cs` : `InvokeStructuredAsync<ClinicalExtractionResult>()` L58 | Pass |
| AC-001: Schema version validated before persistence | `GeminiExtractionAdapter.cs` : `SchemaVersion` equality check L63–66 | Pass |
| AC-001: `extractionStatus` Processing → Completed | `ClinicalDataExtractionJob.cs` : `ExecuteAsync()` L107 | Pass |
| AC-002: PHI encrypted with AES-256-GCM before persistence | `ClinicalDataExtractionJob.cs` L99 — `EncryptedExtractedJson` set to plaintext; `EncryptedStringConverter` (AES-256-GCM) encrypts on `SaveChanges` | Pass |
| AC-003: `AI_INVOCATION` audit entry on every Gemini call | `GeminiInvocationLogger.cs` — decorator wrapping `IGeminiClient`; writes audit in `finally` block (success + failure) | Pass |
| AC-003: `promptHash` (SHA-256) in audit | `GeminiInvocationLogger.cs` — SHA-256 computed on raw prompt | Pass |
| AC-003: `inputTokenCount` / `outputTokenCount` in audit | `GeminiInvocationLogger.cs` — sourced from `InvokeStructuredCoreAsync` return tuple | Pass |
| AC-003: `responseLatencyMs` in audit | `GeminiInvocationLogger.cs` — `Stopwatch` timing around core call | Pass |
| AC-003: `httpStatusCode` in audit | `GeminiInvocationLogger.cs` — HTTP status from Gemini SDK response | Pass |
| AC-003: `modelVersion` in audit | `GeminiInvocationLogger.cs` — resolved from `IGeminiClient` configuration | Pass |
| Edge: image-only PDF → `extractionStatus = "Failed"` | `ClinicalDataExtractionJob.cs` L86–90 — `string.IsNullOrEmpty(text)` → `SetFailedAsync("No text extracted from document.")` | Pass |
| Edge: schema mismatch → non-retriable failure | `GeminiExtractionAdapter.cs` L63–66 throws `SchemaVersionMismatchException`; `ClinicalDataExtractionJob.cs` L114–119 catches and calls `SetFailedAsync` | Pass |
| Edge: Gemini 429 rate limit → Hangfire retry | All exceptions except `SchemaVersionMismatchException` propagate → `ExponentialBackOffRetryFilter` (3 attempts: 10 s / 60 s / 360 s) | Pass |
| AIR-002: Structured Output pattern with versioned prompt | `ClinicalExtractionPrompt.json` (embedded resource) + `GeminiExtractionAdapter` + `ClinicalExtractionResult` record | Pass |
| AIR-006: AI audit logging | `GeminiInvocationLogger` decorator (same path as AC-003) | Pass |
| OWASP A03: Prompt injection prevention | `GeminiExtractionAdapter.cs` L55 — document text fenced with `<document_text>…</document_text>` | Pass |
| OWASP A02: Secrets not hardcoded | `SupabaseStorageService.cs` — credentials loaded exclusively from environment variables | Pass |

---

## Logical & Design Findings

**Business Logic:**
- The `ExtractionFailureNote` field referenced in the task edge-case description ("No text extracted note in DB") is not persisted to `ClinicalDocument` — only logged. This is by design: the `ExtractionFailureNote` column is introduced in `task_002` (US_026 migration). Until that migration runs, the note lives in the application log only. This is a tracked cross-task dependency, not a defect.
- No guard prevents the job from re-processing a document whose `extractionStatus` is already `"Completed"`. If `ClinicalDataExtractionJob` were manually re-dispatched, a second `ExtractedClinicalData` row would be created for the same document. Hangfire's normal deduplication prevents this in the happy path, but the job lacks defensive idempotency.

**Security:**
- All PHI fields encrypted via EF Core `EncryptedStringConverter` (AES-256-GCM). ✅
- No credentials, keys, or patient data appear in log lines. ✅
- Document text is XML-fenced before prompt construction — prompt injection risk mitigated. ✅
- **Gap (MEDIUM):** No maximum text length enforced before the Gemini call. A very large PDF (e.g. 500+ pages) would send the full document text in a single prompt. While `gemini-1.5-pro` supports a 2 M-token context window, unbounded input creates a cost runaway risk (AI Risk: unbounded token spend).

**Error Handling:**
- `SchemaVersionMismatchException` correctly swallowed (non-retriable). ✅
- All other exceptions propagate to `ExponentialBackOffRetryFilter`. ✅
- `string.IsNullOrEmpty(text)` guards the empty extraction path. ✅
- `document is null` guard exits early for deleted documents. ✅
- HTTP errors from `DownloadAsync` throw `InvalidOperationException` — these are retriable and correctly propagate. ✅

**Data Access:**
- `AppDbContext` and `IUnitOfWork` resolve to the same scoped instance per Hangfire job execution — `SaveChangesAsync` calls commit correctly. ✅
- `_db.ClinicalDocuments.FirstOrDefaultAsync` performs a single round-trip by ID — no N+1 concern. ✅
- Two `SaveChangesAsync` calls per successful execution (Processing transition + Completed transition) — acceptable; no transaction wrapping needed since status transitions are idempotent under retry.

**Patterns & Standards:**
- `GeminiExtractionAdapter` is `internal sealed` — consistent with `GeminiIntakeAdapter`. ✅
- `ClinicalDataExtractionJob` is `internal sealed` — consistent with other Hangfire jobs in the assembly. ✅ (corrected from initial `public` to resolve CS0051)
- `ClinicalExtractionPrompt.json` registered as `<EmbeddedResource>` — follows `IntakeQuestions.json` pattern. ✅
- DI registrations added as `AddScoped` — correct lifetime for Hangfire jobs (per-execution scope). ✅
- `ClinicalExtractionResult` is a `public sealed record` — correct visibility since it is serialised to `EncryptedExtractedJson` and may be referenced by downstream jobs. ✅

**Performance:**
- No pagination or chunking of document text before Gemini call — risk of excessive latency for large documents.
- Embedded resource loaded at construction time — cached in-process; no I/O cost after startup. ✅

---

## Test Review

**Existing Tests:** None for the new extraction pipeline. `UPACIP.Tests` project exists but contains no tests targeting `ClinicalDataExtractionJob`, `GeminiExtractionAdapter`, or `DownloadAsync`.

**Missing Tests (must add):**

- [ ] Unit: `GeminiExtractionAdapter` — happy path returns `ClinicalExtractionResult` with `SchemaVersion = "1.0"`
- [ ] Unit: `GeminiExtractionAdapter` — mismatched `schemaVersion` throws `SchemaVersionMismatchException`
- [ ] Unit: `GeminiExtractionAdapter` — prompt injection fencing applied (document text wrapped in `<document_text>` tags)
- [ ] Unit: `ClinicalDataExtractionJob` — empty text path sets `extractionStatus = "Failed"` and returns without persisting `ExtractedClinicalData`
- [ ] Unit: `ClinicalDataExtractionJob` — `SchemaVersionMismatchException` sets `extractionStatus = "Failed"` and does not re-throw
- [ ] Unit: `ClinicalDataExtractionJob` — non-schema exception propagates (allowing Hangfire retry)
- [ ] Unit: `ClinicalDataExtractionJob` — null document exits early without throwing
- [ ] Integration: `DownloadAsync` returns byte array on 200; throws `InvalidOperationException` on 404 / 500
- [ ] Negative/Edge: `GeminiExtractionAdapter` with `null` `schemaVersion` in response → `SchemaVersionMismatchException`

---

## Validation Results

**Commands Executed:** N/A — build-time validation only (task did not list runnable CLI validation commands).

**Outcomes:**

| Check | Result |
|---|---|
| `dotnet build UPACIP.sln` | Pass — zero compiler errors; NU1903 pre-existing warning (Newtonsoft.Json 11.0.1 vulnerability) not introduced by this task |
| `get_errors` on all 4 modified/created files | 0 errors |
| `GeminiExtractionAdapter` CS0051 accessibility violation | Fixed — job changed from `public` to `internal sealed` to match adapter visibility |

---

## Fix Plan (Prioritized)

1. **Add unit tests for `GeminiExtractionAdapter`** — `UPACIP.Tests/UnitTests/AI/GeminiExtractionAdapterTests.cs` — ETA 2 h — Risk: **HIGH** (no regression safety without tests)
2. **Add unit tests for `ClinicalDataExtractionJob`** — `UPACIP.Tests/UnitTests/BackgroundJobs/ClinicalDataExtractionJobTests.cs` — ETA 3 h — Risk: **HIGH** (AC-001 and edge case paths unverified by automation)
3. **Add integration test for `DownloadAsync`** — `UPACIP.Tests/IntegrationTests/SupabaseStorageServiceTests.cs` — ETA 1 h — Risk: **MEDIUM**
4. **Cap document text length before Gemini call** — `GeminiExtractionAdapter.cs` : truncate `documentText` to a configurable max character count (e.g. 400 000 chars ≈ 100 k tokens) before fencing — ETA 1 h — Risk: **MEDIUM** (AI cost runaway)
5. **Add idempotency guard** — `ClinicalDataExtractionJob.cs` : `if (document.ExtractionStatus == "Completed") return;` before setting "Processing" — ETA 0.5 h — Risk: **LOW**

---

## Appendix

**Rules Applied:**

- `rules/security-standards-owasp.md` — OWASP A02, A03 verification
- `rules/code-anti-patterns.md` — checked for god objects, magic constants, circular deps
- `rules/dry-principle-guidelines.md` — adapter follows existing GeminiIntakeAdapter pattern; no duplication
- `rules/dotnet-architecture-standards.md` — Clean Architecture layer boundaries, DI lifetime
- `rules/backend-development-standards.md` — service/job constructor injection pattern
- `rules/language-agnostic-standards.md` — KISS, YAGNI, naming clarity
- `rules/performance-best-practices.md` — embedded resource caching, no N+1
- `rules/code-documentation-standards.md` — XML doc comments on public-facing symbols

**Search Evidence:**

| Pattern | File(s) Found |
|---|---|
| `ClinicalDataExtractionJob` | `BackgroundJobs/ClinicalDataExtractionJob.cs`, `BackgroundJobs/HangfireDocumentExtractionJobDispatcher.cs`, `DependencyInjection.cs` |
| `GeminiExtractionAdapter` | `AI/GeminiExtractionAdapter.cs`, `DependencyInjection.cs` |
| `ClinicalExtractionPrompt.json` | `AI/ClinicalExtractionPrompt.json`, `UPACIP.Infrastructure.csproj` |
| `DownloadAsync` | `Application/Interfaces/IDocumentStorageService.cs`, `Documents/SupabaseStorageService.cs` |
| `SchemaVersionMismatchException` | `AI/GeminiExtractionAdapter.cs` (defined), `BackgroundJobs/ClinicalDataExtractionJob.cs` (caught) |
| `EncryptedStringConverter` | `Persistence/AppDbContext.cs` (applied to `EncryptedExtractedJson`) |
| `GeminiInvocationLogger` | `AI/GeminiInvocationLogger.cs` (decorator — handles AC-003 automatically) |

**Context7 References:** N/A — implementation uses existing registered APIs; no external library documentation fetched.
