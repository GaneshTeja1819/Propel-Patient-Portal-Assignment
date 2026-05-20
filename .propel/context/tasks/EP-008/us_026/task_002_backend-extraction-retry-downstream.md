# Task - TASK_002

## Requirement Reference
- **User Story:** us_026
- **Story Location:** .propel/context/tasks/EP-008/us_026/us_026.md
- **Acceptance Criteria:**
  - AC-002: PHI fields in ExtractedClinicalData encrypted before persistence (shared with task_001 — this task covers the failure and downstream paths)
  - AC-004: Gemini failure retries twice with exponential back-off; after all retries → extractionStatus="Failed"; "Extraction failed — retry" message surfaced (UXR-603); AI_INVOCATION audit written on failure
  - AC-005: Downstream de-duplication Hangfire job enqueued after successful extraction; extraction job does not wait for it
- **Edge Cases:**
  - Gemini rate limit persists across all retries → extractionStatus="Failed"; Hangfire dashboard shows "Rate limit exceeded"
  - ExtractedClinicalData schema version mismatch → SchemaValidationException; counted as retry attempt; logged

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
| **UXR Requirements** | UXR-603 |
| **Design Tokens** | N/A |

---

## AI References [CONDITIONAL: AI Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | Yes |
| **AIR Requirements** | AIR-007 |
| **AI Pattern** | Structured Output |
| **Prompt Template Path** | backend/src/UPACIP.Infrastructure/AI/ClinicalExtractionPrompt.json |
| **Guardrails Config** | Max 2 retries; extractionStatus="Failed" after all retries |
| **Model Provider** | Google Gemini (gemini-1.5-pro) |

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
| Background Jobs | Hangfire (PostgreSQL storage) | 1.8.x | NFR-008 — retry with back-off; downstream job enqueue |
| Database | PostgreSQL via Supabase | 15 | TR-003 — ClinicalDocument.extractionStatus; failure note |

---

## Task Overview
Extend `ClinicalDataExtractionJob` with the failure/retry path and the downstream job chain. The Hangfire `[AutomaticRetry(Attempts = 2)]` attribute handles the retry schedule (back-off: 30 s, 300 s). After both retries fail, an `OnFailure` Hangfire continuation sets `ClinicalDocument.extractionStatus = "Failed"` and stores the failure reason in `extractionFailureNote`. A `GET /api/v1/documents/{id}/extraction-status` endpoint surfaces `extractionStatus` and `extractionFailureNote` to the frontend for UXR-603 retry CTA. On successful extraction, the `DeduplicationJob` is enqueued as a Hangfire continuation (`BackgroundJob.ContinueJobWith`).

## Dependent Tasks
- `task_001_ai-clinical-extraction-job.md` (US_026) — `ClinicalDataExtractionJob` must exist as the base

## Impacted Components
- `backend/src/UPACIP.Infrastructure/BackgroundJobs/ClinicalDataExtractionJob.cs` — add retry attribute; failure continuation; downstream job enqueue
- `backend/src/UPACIP.Infrastructure/BackgroundJobs/DeduplicationJob.cs` — new stub Hangfire job (full logic in US_027 task_002)
- `backend/src/UPACIP.API/Controllers/DocumentsController.cs` — add GET /extraction-status action

## Implementation Plan
1. Add `[AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 30, 300 })]` to `ClinicalDataExtractionJob` class
2. Wrap the Gemini call + schema validation section in a try/catch: on `Exception` → log failure reason; re-throw (Hangfire handles the retry); after max retries Hangfire marks the job as "Failed"
3. Create a Hangfire job failure continuation using `BackgroundJob.ContinueJobWith` on the failed job (or use `IBackgroundJobClient` with a state filter): on final failure → call `MarkExtractionFailedAsync(clinicalDocumentId, failureReason)` which sets `ClinicalDocument.extractionStatus = "Failed"`, `extractionFailureNote = failureReason`; write `AI_INVOCATION` audit with `httpStatusCode` from failure
4. On successful extraction: `BackgroundJob.ContinueJobWith<DeduplicationJob>(extractionJobId, j => j.ExecuteAsync(clinicalDocumentId))` — fire and forget (AC-005); extraction job does not wait
5. Create `DeduplicationJob.cs` as a stub (full implementation in US_027 task_002): accepts `clinicalDocumentId`; logs "Deduplication job started for document {id}"
6. Add `GET /api/v1/documents/{id}/extraction-status` to `DocumentsController`: returns `{ extractionStatus, extractionFailureNote }`; `[Authorize(Policy = "PatientPolicy")]` — surfaces failed status for UXR-603 retry CTA
7. Add `POST /api/v1/documents/{id}/retry-extraction` to `DocumentsController`: resets `ClinicalDocument.extractionStatus = "Pending"`; clears `extractionFailureNote`; enqueues a fresh `ClinicalDataExtractionJob` for the document via `IBackgroundJobClient`; returns 202 Accepted; `[Authorize(Policy = "PatientPolicy")]`; guards against retry storms by rejecting if status is not `"Failed"` (returns 409 Conflict if status is `"Processing"` or `"Completed"`)

## Current Project State
```
backend/
  src/
    UPACIP.Infrastructure/BackgroundJobs/ClinicalDataExtractionJob.cs  (from task_001)
    UPACIP.API/Controllers/DocumentsController.cs  (from US_025)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | backend/src/UPACIP.Infrastructure/BackgroundJobs/ClinicalDataExtractionJob.cs | Add AutomaticRetry; failure continuation; downstream job enqueue |
| CREATE | backend/src/UPACIP.Infrastructure/BackgroundJobs/DeduplicationJob.cs | Dedup job stub (full in US_027) |
| MODIFY | backend/src/UPACIP.API/Controllers/DocumentsController.cs | Add GET /extraction-status and POST /retry-extraction endpoints (UXR-603) |

## External References
- [Hangfire AutomaticRetry with custom delays](https://docs.hangfire.io/en/latest/background-methods/performing-recurrent-tasks.html)
- [Hangfire — ContinueJobWith](https://docs.hangfire.io/en/latest/background-methods/continuations.html)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Force Gemini failure → job retries twice (30 s, 300 s back-off); after 2 failures → extractionStatus="Failed"; failure note stored
- [ ] GET /api/v1/documents/{id}/extraction-status returns "Failed" + reason (UXR-603 support)
- [ ] Successful extraction → DeduplicationJob enqueued; extraction job marked "Succeeded" without waiting for dedup

## Implementation Checklist
- [x] `[AutomaticRetry(Attempts = 2, DelaysInSeconds = [30, 300])]` on extraction job (AC-004)
- [x] Failure continuation: set `extractionStatus = "Failed"`; store `extractionFailureNote` (AC-004)
- [x] Write `AI_INVOCATION` audit on Gemini failure with httpStatusCode (AC-003, AIR-007)
- [x] Enqueue `DeduplicationJob` as continuation on success; extraction does not await it (AC-005)
- [x] Add `GET /extraction-status` endpoint returning status + failure note (UXR-603)
- [x] Add `POST /retry-extraction` endpoint resetting status to `"Pending"` and re-enqueueing extraction job; reject with 409 if not in `"Failed"` state (AC-004, AIR-007)
