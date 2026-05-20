# Task - TASK_002

## Requirement Reference
- **User Story:** us_027
- **Story Location:** .propel/context/tasks/EP-009/us_027/us_027.md
- **Acceptance Criteria:**
  - AC-001: `GET /api/v1/profile/{patientId}` returns unified profile for Staff; `GET /api/v1/profile/me` for Patient own-only
  - AC-003: De-duplication merges overlapping entries (case-insensitive, normalised); canonical entry links to source documents
  - AC-005: Profile API P95 ≤ 500 ms
- **Edge Cases:**
  - Dedup not completed → profile query returns raw `ExtractedClinicalData` entries; `deduplicationStatus = "Processing"`
  - > 50 entries per section in DB → query with `LIMIT 20 OFFSET n` via indexed column; total count returned for pagination
  - Concurrent dedup runs for same patient → second job sees `deduplicationStatus = "Processing"`; exits without creating duplicates

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
| **AIR Requirements** | AIR-004 |
| **AI Pattern** | Structured Output |
| **Prompt Template Path** | backend/src/UPACIP.Infrastructure/AI/DeduplicationPrompt.json |
| **Guardrails Config** | Gemini dedup prompt; canonical output with source references; no PHI in prompt without encryption |
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
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | TR-002 — GET profile endpoint |
| Database | PostgreSQL via Supabase | 15 | TR-003 — indexed query for P95 ≤ 500 ms |
| AI | Google Gemini API (gemini-1.5-pro) | Google.Ai.Generativelanguage 1.x | AIR-004 — structured de-duplication |
| Background Jobs | Hangfire (PostgreSQL storage) | 1.8.x | TR-004 — DeduplicationJob |

---

## Task Overview
Implement the `DeduplicationJob` Hangfire job (replacing the stub from US_026) and the `GET /api/v1/profile/{patientId}` profile endpoint. The dedup job loads all `ExtractedClinicalData` for a patient, decrypts PHI fields, sends them to Gemini with a structured dedup prompt, validates the response, encrypts canonical output, and stores `MergedClinicalEntry` records with `sourceDocumentIds`. The profile endpoint aggregates from `MergedClinicalEntry` (or raw entries if dedup is Processing/Failed), applies pagination via LIMIT/OFFSET, and returns a strongly typed `PatientProfileDto`.

## Dependent Tasks
- `task_001_ai-clinical-extraction-job.md` (US_026) — `ExtractedClinicalData` populated; `DeduplicationJob` stub enqueued
- `task_002_ai-gemini-sdk.md` (US_008) — `IGeminiService` registered

## Impacted Components
- `backend/src/UPACIP.Infrastructure/BackgroundJobs/DeduplicationJob.cs` — replace stub; implement full dedup logic
- `backend/src/UPACIP.Infrastructure/AI/DeduplicationPrompt.json` — new dedup structured prompt
- `backend/src/UPACIP.Domain/Entities/MergedClinicalEntry.cs` — new Domain entity for canonical clinical entries after deduplication
- `backend/src/UPACIP.Application/Queries/Profile/GetPatientProfileQuery.cs` — new CQRS query
- `backend/src/UPACIP.API/Controllers/ProfileController.cs` — new controller with GET endpoints

## Implementation Plan
1. Create `DeduplicationPrompt.json`: lists all clinical entry types; dedup schema = `{ canonicalEntries: [{ value, normalised, sourceDocumentIds[], confidence }] }`
2. Implement `DeduplicationJob.ExecuteAsync(patientId)`:
   - Idempotency: check `Patient.deduplicationStatus`; if "Processing" → exit without duplicating
   - Set `deduplicationStatus = "Processing"`
   - Load all `ExtractedClinicalData` for `patientId`; decrypt PHI fields; build prompt payload
   - Call `IGeminiService.CallStructuredOutputAsync` with `DeduplicationPrompt.json`; validate response schema
   - For each canonical entry: create `MergedClinicalEntry` { `patientId`, `entryType`, `canonicalValue`, `sourceDocumentIds` (FK array or JSON), `mergedAt`, `confidence`; PHI fields encrypted }
   - Set `deduplicationStatus = "Completed"`; write `DEDUP_COMPLETED` audit entry
3. Implement `GetPatientProfileQuery`:
   - Load `MergedClinicalEntry` (if dedup = "Completed") or `ExtractedClinicalData` (if Processing/Pending); paginate with LIMIT/OFFSET indexed by `patientId`
   - Map to `PatientProfileDto`: `{ vitals, medications, diagnoses, visitHistory, deduplicationStatus, hasDocuments, pagination: { page, pageSize, total } }`
   - Index on `MergedClinicalEntry.patientId` to ensure P95 ≤ 500 ms (AC-005)
4. Create `ProfileController`:
   - `GET /api/v1/profile/me` — `[Authorize(Policy = "PatientPolicy")]`; resolves `patientId` from JWT claim
   - `GET /api/v1/profile/{patientId}` — `[Authorize(Policy = "StaffPolicy")]`; validates patientId

## Current Project State
```
backend/
  src/
    UPACIP.Infrastructure/BackgroundJobs/DeduplicationJob.cs  (stub from US_026)
    UPACIP.Infrastructure/AI/GeminiService.cs  (from US_008)
    UPACIP.Infrastructure/Security/PhiEncryptionService.cs
    UPACIP.Domain/Entities/ExtractedClinicalData.cs
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | `backend/src/UPACIP.Domain/Entities/MergedClinicalEntry.cs` | Domain entity: `Id`, `PatientId`, `EntryType` (enum), `EncryptedCanonicalValue`, `SourceDocumentIds` (JSON), `MergedAt`, `Confidence`, `IsPhiField`; navigation to `User` |
| MODIFY | `backend/src/UPACIP.Infrastructure/BackgroundJobs/DeduplicationJob.cs` | Replace stub with full dedup implementation |
| CREATE | `backend/src/UPACIP.Infrastructure/AI/DeduplicationPrompt.json` | Structured dedup prompt + canonical schema |
| CREATE | `backend/src/UPACIP.Infrastructure/Migrations/<timestamp>_AddMergedClinicalEntry.cs` | EF Core migration: `MergedClinicalEntry` table + `IX_MergedClinicalEntry_PatientId` index (AC-005, NFR-004) |
| CREATE | `backend/src/UPACIP.Application/Queries/Profile/GetPatientProfileQuery.cs` | Profile aggregation CQRS query |
| CREATE | `backend/src/UPACIP.API/Controllers/ProfileController.cs` | GET /api/v1/profile endpoints |

## External References
- [Gemini structured output](https://ai.google.dev/gemini-api/docs/structured-output)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] DeduplicationJob runs with two ExtractedClinicalData records containing overlapping medication names → one MergedClinicalEntry with both source document IDs
- [ ] Concurrent job for same patient → second run exits immediately without creating duplicate MergedClinicalEntries
- [ ] GET /api/v1/profile/{patientId} → P95 ≤ 500 ms under 20 concurrent requests (load test)

## Implementation Checklist
- [x] `DeduplicationJob` idempotency guard: exit if `deduplicationStatus = "Processing"` (edge case)
- [x] Dedup job: decrypt extracted data; call Gemini with structured dedup prompt; validate response (AC-003, AIR-004)
- [x] Create `MergedClinicalEntry` records with PHI encryption and source document links (AC-003)
- [x] Profile query: LIMIT/OFFSET pagination; return raw data if dedup not completed (AC-001, edge case)
- [x] Indexed query on `patientId`; P95 ≤ 500 ms (AC-005)
- [x] `GET /me` (Patient own-only) + `GET /{patientId}` (Staff only); HTTP 403 on wrong role (AC-001, OWASP A01)
- [x] Create `MergedClinicalEntry` Domain entity; EF Core migration + `IX_MergedClinicalEntry_PatientId` index; `dotnet ef migrations add` before deploy (AC-003, AC-005, NFR-004)
