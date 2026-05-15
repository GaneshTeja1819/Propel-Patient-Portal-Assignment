# Task - TASK_001

## Requirement Reference
- **User Story:** us_029
- **Story Location:** .propel/context/tasks/EP-010/us_029/us_029.md
- **Acceptance Criteria:**
  - AC-001: Code suggestion job triggered after extraction completion (auto) OR on-demand by Staff; Gemini invoked with structured ICD-10/CPT prompt
  - AC-002: Each MedicalCodeSuggestion stored: codeType (ICD10/CPT), suggestedCode (validated against reference codeset), rank, confidenceScore (0.00–1.00), derivedFrom (FK to ExtractedClinicalData)
  - AC-003: Zero suggestions → no records created; audit entry written; SCR-014 shows "No codes suggested — manual coding required"
  - AC-004: Every Gemini call → AI_INVOCATION audit with modelVersion, promptHash, inputTokenCount, outputTokenCount, responseLatencyMs, httpStatusCode
  - AC-005: Gemini failure → retry exactly once; if retry fails → "Code suggestion failed" notification on SCR-014; ExtractedClinicalData unchanged
- **Edge Cases:**
  - Concurrent jobs for same ExtractedClinicalDataId → idempotency guard (exit if existing Pending/Processing set)
  - Unrecognised terminology → low-confidence suggestions (confidenceScore < 0.5) stored; Staff can still accept/modify/reject
  - "Regenerate" CTA on SCR-014 triggers on-demand POST endpoint for re-run

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
| **AI Impact** | Yes |
| **AIR Requirements** | AIR-003, AIR-006 |
| **AI Pattern** | Structured Output |
| **Prompt Template Path** | backend/src/UPACIP.Infrastructure/AI/CodeSuggestionPrompt.json |
| **Guardrails Config** | ICD-10/CPT reference codeset validation; max 1 retry; idempotency guard |
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
| Background Jobs | Hangfire (PostgreSQL storage) | 1.8.x | TR-004 — CodeSuggestionJob; auto + on-demand trigger |
| AI | Google Gemini API (gemini-1.5-pro) | Google.Ai.Generativelanguage 1.x | AIR-003 — structured ICD-10/CPT extraction |
| Database | PostgreSQL via Supabase | 15 | TR-003 — MedicalCodeSuggestion entity; reference codeset |

---

## Task Overview
Implement `CodeSuggestionJob` as a Hangfire job with an idempotency guard (exit if any `MedicalCodeSuggestion` exists with status Pending/Processing for the same `ExtractedClinicalDataId`). The job calls Gemini with a structured prompt to extract ICD-10 and CPT codes from the clinical text, validates each `suggestedCode` against a reference codeset loaded from a static JSON or DB table, stores ranked `MedicalCodeSuggestion` records, and writes an `AI_INVOCATION` audit entry. The on-demand trigger is exposed via `POST /api/v1/code-suggestions/generate`. `[AutomaticRetry(Attempts = 1)]` is applied; failure sets `MedicalCodeSuggestion` batch status to "Failed" and writes a `CODE_SUGGESTION_FAILED` audit entry.

## Dependent Tasks
- `task_001_ai-clinical-extraction-job.md` (US_026) — `ClinicalDataExtractionJob` enqueues `CodeSuggestionJob` after successful extraction
- `task_002_ai-gemini-sdk.md` (US_008) — `IGeminiService` registered

## Impacted Components
- `backend/src/UPACIP.Infrastructure/BackgroundJobs/CodeSuggestionJob.cs` — new Hangfire job
- `backend/src/UPACIP.Infrastructure/AI/CodeSuggestionPrompt.json` — structured prompt + ICD-10/CPT schema
- `backend/src/UPACIP.Infrastructure/AI/CodeSuggestionAdapter.cs` — new Gemini adapter
- `backend/src/UPACIP.API/Controllers/CodeSuggestionsController.cs` — on-demand trigger endpoint

## Implementation Plan
1. Create `CodeSuggestionPrompt.json`: defines output schema `{ suggestions: [{ codeType: "ICD10|CPT", code, rank, confidenceScore, derivedFromField }] }`; instructs Gemini to use recognised ICD-10/CPT codes only; includes schema version
2. Create `CodeSuggestionAdapter.SuggestCodesAsync(clinicalText)`:
   - Calls `IGeminiService.CallStructuredOutputAsync` with `CodeSuggestionPrompt.json` schema
   - Records `responseLatencyMs` and token counts
   - Returns validated response or throws `SuggestionSchemaValidationException`
3. Implement `CodeSuggestionJob.ExecuteAsync(extractedClinicalDataId)`:
   - Idempotency: if any `MedicalCodeSuggestion` with `extractedClinicalDataId` and `status ∈ [Pending, Processing]` exists → exit
   - Load `ExtractedClinicalData`; decrypt clinical text
   - Call `CodeSuggestionAdapter.SuggestCodesAsync`
   - For each suggestion: validate `suggestedCode` against reference codeset (query `MedicalCodeReferenceTable` or deserialise `icd10_cpt_reference.json`); store regardless of confidence but set `confidenceScore` accurately
   - Create `MedicalCodeSuggestion[]` records; store in DB
   - Write `AI_INVOCATION` audit
   - On zero results: write `CODE_SUGGESTION_EMPTY` audit; no `MedicalCodeSuggestion` records (AC-003)
4. Apply `[AutomaticRetry(Attempts = 1)]`; after retry failure → write `CODE_SUGGESTION_FAILED` audit; `ExtractedClinicalData` unchanged (AC-005)
5. Create `POST /api/v1/code-suggestions/generate` in `CodeSuggestionsController`:
   - `[Authorize(Policy = "StaffPolicy")]`; body: `{ extractedClinicalDataId }`
   - Runs same idempotency check; enqueues new `CodeSuggestionJob`; returns HTTP 202 with `{ jobId }`
   - On active Pending/Processing: returns HTTP 409 "Suggestion generation already in progress"
6. Extend `ClinicalDataExtractionJob` (US_026): after persisting `ExtractedClinicalData` → `BackgroundJob.ContinueJobWith<CodeSuggestionJob>(extractionJobId, ...)`

## Current Project State
```
backend/
  src/
    UPACIP.Infrastructure/BackgroundJobs/ClinicalDataExtractionJob.cs  (from US_026)
    UPACIP.Infrastructure/AI/GeminiService.cs  (from US_008)
    UPACIP.Domain/Entities/ExtractedClinicalData.cs
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/UPACIP.Infrastructure/BackgroundJobs/CodeSuggestionJob.cs | Code suggestion Hangfire job with idempotency |
| CREATE | backend/src/UPACIP.Infrastructure/AI/CodeSuggestionPrompt.json | Structured ICD-10/CPT prompt + schema |
| CREATE | backend/src/UPACIP.Infrastructure/AI/CodeSuggestionAdapter.cs | Gemini adapter for code suggestions |
| CREATE | backend/src/UPACIP.API/Controllers/CodeSuggestionsController.cs | On-demand trigger + idempotency endpoint |
| MODIFY | backend/src/UPACIP.Infrastructure/BackgroundJobs/ClinicalDataExtractionJob.cs | Enqueue CodeSuggestionJob as continuation |

## External References
- [ICD-10-CM browse](https://www.icd10data.com/)
- [CMS CPT codes](https://www.cms.gov/medicare/coding-billing/physician-fee-schedule/codes)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Extraction job completes → CodeSuggestionJob enqueued automatically; MedicalCodeSuggestion records created
- [ ] Run CodeSuggestionJob while Pending set exists → job exits immediately; no duplicate records
- [ ] POST /generate → HTTP 202; concurrent POST while Pending → HTTP 409
- [ ] Force Gemini failure × 2 → CODE_SUGGESTION_FAILED audit; ExtractedClinicalData unchanged

## Implementation Checklist
- [ ] Idempotency guard: exit if Pending/Processing set exists for same `extractedClinicalDataId` (edge case)
- [ ] Gemini structured call via `CodeSuggestionAdapter`; schema validation (AC-001, AIR-003)
- [ ] Reference codeset validation per suggestion; store all (including low-confidence) (AC-002, edge case)
- [ ] Zero results: no records + `CODE_SUGGESTION_EMPTY` audit; no data change (AC-003)
- [ ] `AI_INVOCATION` audit with all required fields (AC-004, AIR-006)
- [ ] `[AutomaticRetry(Attempts = 1)]`; failure audit + ExtractedClinicalData unchanged (AC-005)
- [ ] `POST /generate` on-demand endpoint with idempotency; Staff only (AC-001 on-demand trigger, OWASP A01)
