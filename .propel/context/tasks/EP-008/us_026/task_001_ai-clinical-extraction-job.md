# Task - TASK_001

## Requirement Reference
- **User Story:** us_026
- **Story Location:** .propel/context/tasks/EP-008/us_026/us_026.md
- **Acceptance Criteria:**
  - AC-001: Hangfire job extracts PDF text via PdfPig; sends to Gemini structured extraction prompt; validates schema; extractionStatus transitions Pending → Processing → Completed
  - AC-002: Extracted PHI fields encrypted with AES-256-GCM before persistence
  - AC-003: Every Gemini call writes AI_INVOCATION audit entry (modelVersion, promptHash, inputTokenCount, outputTokenCount, responseLatencyMs, httpStatusCode)
- **Edge Cases:**
  - PDF contains only scanned images → PdfPig returns empty string; Gemini returns empty extraction; extractionStatus = "Failed" with "No text extracted" note
  - Schema version mismatch between upload and extraction → schema version field on job; mismatched schema fails validation; logged without silent data loss
  - Gemini rate limit → retry with back-off (handled in task_002)

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
| **AIR Requirements** | AIR-002, AIR-006 |
| **AI Pattern** | Structured Output |
| **Prompt Template Path** | backend/src/UPACIP.Infrastructure/AI/ClinicalExtractionPrompt.json |
| **Guardrails Config** | Schema validation before any field persisted; max 2 Gemini retries |
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
| Background Jobs | Hangfire (PostgreSQL storage) | 1.8.x | TR-004 — async extraction job |
| PDF | PdfPig | Latest stable | TR-007 — PDF text extraction |
| AI | Google Gemini API (gemini-1.5-pro) | Google.Ai.Generativelanguage 1.x | TR-008, AIR-002 — structured clinical data extraction |
| Security | AES-256-GCM (.NET 8 built-in) | .NET 8 | NFR-001 — PHI encryption before persistence |

---

## Task Overview
Implement `ClinicalDataExtractionJob`, the core extraction Hangfire job. On execution the job:
1. Sets `extractionStatus = "Processing"`
2. Decrypts `ClinicalDocument.encryptedStoragePath`; downloads PDF bytes from Supabase Storage
3. Extracts text via PdfPig (`PdfDocument.Open(bytes)` → page text concatenation)
4. Builds the Gemini structured extraction prompt from `ClinicalExtractionPrompt.json`; calls `IGeminiService.CallStructuredOutputAsync`
5. Validates the response against `ExtractedClinicalDataSchema`
6. Encrypts PHI fields; persists `ExtractedClinicalData`
7. Sets `extractionStatus = "Completed"`
8. Writes `AI_INVOCATION` audit entry; enqueues downstream de-duplication job (task_002)

## Dependent Tasks
- `task_002_backend-document-upload.md` (US_025) — `ClinicalDocument` record must exist; job enqueued by upload handler
- `task_002_ai-gemini-sdk.md` (US_008) — `IGeminiService` must be registered

## Impacted Components
- `backend/src/UPACIP.Infrastructure/BackgroundJobs/ClinicalDataExtractionJob.cs` — new Hangfire job
- `backend/src/UPACIP.Infrastructure/AI/ClinicalExtractionPrompt.json` — structured extraction prompt template
- `backend/src/UPACIP.Infrastructure/AI/GeminiExtractionAdapter.cs` — new adapter for clinical extraction

## Implementation Plan
1. Create `ClinicalExtractionPrompt.json`: defines extraction schema fields (vitals: bloodPressure, heartRate, weight; medications: name, dosage, frequency; diagnoses: icdCode, description; rawText); includes schema version
2. Create `GeminiExtractionAdapter.CallExtractionAsync(text, schemaVersion)`:
   - Sends text to Gemini with function-calling schema from `ClinicalExtractionPrompt.json`
   - Validates response against `ExtractedClinicalDataSchema`; throws `SchemaValidationException` on mismatch
   - Records timing for `responseLatencyMs`
3. `ClinicalDataExtractionJob.ExecuteAsync(clinicalDocumentId)`:
   - Load `ClinicalDocument`; update `extractionStatus = "Processing"`
   - Decrypt storagePath; download PDF bytes from Supabase Storage
   - Extract text via PdfPig; if empty → set `extractionStatus = "Failed"` with note "No text extracted"; write audit; return
   - Call `GeminiExtractionAdapter.CallExtractionAsync`; compute `promptHash = SHA-256(prompt)`
   - On success: encrypt PHI fields; create `ExtractedClinicalData`; set `extractionStatus = "Completed"`
   - Write `AI_INVOCATION` audit: `modelVersion`, `promptHash`, `inputTokenCount`, `outputTokenCount`, `responseLatencyMs`, `httpStatusCode`

## Current Project State
```
backend/
  src/
    UPACIP.Infrastructure/AI/GeminiService.cs  (from US_008)
    UPACIP.Domain/Entities/ClinicalDocument.cs
    UPACIP.Infrastructure/Security/PhiEncryptionService.cs
    UPACIP.Infrastructure/Storage/SupabaseStorageService.cs  (from US_025)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/UPACIP.Infrastructure/BackgroundJobs/ClinicalDataExtractionJob.cs | Core extraction Hangfire job |
| CREATE | backend/src/UPACIP.Infrastructure/AI/ClinicalExtractionPrompt.json | Structured prompt + schema definition |
| CREATE | backend/src/UPACIP.Infrastructure/AI/GeminiExtractionAdapter.cs | Gemini structured output adapter for clinical extraction |

## External References
- [PdfPig — Text extraction](https://github.com/UglyToad/PdfPig)
- [Gemini structured output](https://ai.google.dev/gemini-api/docs/structured-output)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Upload valid text-based PDF → job runs; extractionStatus = "Completed"; ExtractedClinicalData row with encrypted PHI fields
- [ ] Upload image-only PDF → PdfPig returns empty string; extractionStatus = "Failed"; "No text extracted" note in DB
- [ ] Verify AI_INVOCATION audit entry written after each Gemini call with all required fields

## Implementation Checklist
- [ ] Create `ClinicalExtractionPrompt.json` with schema version field (edge case — schema evolution)
- [ ] Implement `GeminiExtractionAdapter`: structured output call; schema validation; timing measurement (AC-001, AC-003)
- [ ] `ClinicalDataExtractionJob`: set Processing on start; PdfPig text extraction; empty text → Failed + note (AC-001, edge case)
- [ ] Encrypt PHI fields before persistence; create `ExtractedClinicalData` (AC-002)
- [ ] Set `extractionStatus = "Completed"` after successful persistence (AC-001)
- [ ] Write `AI_INVOCATION` audit with all required fields (AC-003, AIR-006)
