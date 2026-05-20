# Task - TASK_002

## Requirement Reference
- **User Story:** us_025
- **Story Location:** .propel/context/tasks/EP-008/us_025/us_025.md
- **Acceptance Criteria:**
  - AC-001: File stored in Supabase Storage; ClinicalDocument record created with encrypted storagePath, documentType, uploadedAt, extractionStatus="Pending"; audit written; confirmation returned
  - AC-004: Hangfire AI extraction job queued within 5 s; extractionStatus transitions to "Processing" when job starts
  - AC-005: storagePath is an encrypted pointer to Supabase Storage; no binary PDF content in any DB column
- **Edge Cases:**
  - Duplicate file (same SHA-256 hash) → HTTP 409 "This document appears to have been uploaded already"; no partial record
  - Supabase Storage limit → HTTP 507 returned to frontend; no ClinicalDocument record created
  - Password-protected PDF → uploaded successfully; extractionStatus = "Pending"; extraction failure handled in US_026

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
| **AI Impact** | No |
| **AIR Requirements** | N/A |
| **AI Pattern** | N/A |
| **Prompt Template Path** | N/A |
| **Guardrails Config** | N/A |
| **Model Provider** | N/A |

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
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | TR-002 — POST /api/v1/documents/upload |
| Database | PostgreSQL via Supabase | 15 | TR-003 — ClinicalDocument entity; DR-005 — no binary in DB |
| Security | AES-256-GCM (.NET 8 built-in) | .NET 8 | NFR-001, DR-005 — encrypt storagePath before DB persistence |
| Background Jobs | Hangfire (PostgreSQL storage) | 1.8.x | TR-004 — enqueue extraction job within 5 s |

---

## Task Overview
Implement `POST /api/v1/documents/upload` as a multipart/form-data endpoint. The handler computes a SHA-256 hash of the file bytes for duplicate detection, stores the PDF in Supabase Storage via the Supabase Storage REST API, encrypts the resulting storage path with `IPhiEncryptionService`, creates a `ClinicalDocument` record with `extractionStatus = "Pending"`, writes an audit entry, and enqueues `ClinicalDataExtractionJob` via Hangfire. No PDF binary content is stored in PostgreSQL.

## Dependent Tasks
- `task_001_backend-migrations.md` (US_005) — `ClinicalDocument` entity must exist
- `task_001_backend-phi-encryption.md` (US_006) — `IPhiEncryptionService` for storagePath encryption
- `task_001_backend-hangfire.md` (US_003) — Hangfire server for extraction job

## Impacted Components
- `backend/src/UPACIP.API/Controllers/DocumentsController.cs` — new controller
- `backend/src/UPACIP.Application/Commands/Documents/UploadDocumentCommand.cs` — new command
- `backend/src/UPACIP.Application/Handlers/Documents/UploadDocumentHandler.cs` — new handler
- `backend/src/UPACIP.Infrastructure/Storage/SupabaseStorageService.cs` — new Supabase Storage adapter

## Implementation Plan
1. Create `SupabaseStorageService.cs`: wraps Supabase Storage REST API (`POST /storage/v1/object/{bucket}/{path}`); credentials from env vars (`SUPABASE_URL`, `SUPABASE_SERVICE_KEY`); returns storage path; throws `StorageLimitExceededException` on HTTP 507/413
2. Create `UploadDocumentHandler.Handle`:
   - Compute `SHA-256` hash of `IFormFile.OpenReadStream()` bytes
   - Check `ClinicalDocument` for existing `fileHash == computedHash` and `patientId == actorId`; if found → HTTP 409 "Document already uploaded"
   - Call `SupabaseStorageService.UploadAsync(patientId, fileName, bytes)` → returns `storagePath`; on `StorageLimitExceededException` → HTTP 507
   - Encrypt `storagePath` with `IPhiEncryptionService.EncryptAsync`
   - Create `ClinicalDocument`: `patientId`, `documentType`, `uploadedAt = UtcNow`, `encryptedStoragePath`, `fileHash`, `extractionStatus = "Pending"`; no `fileContent` column
   - Write `DOCUMENT_UPLOADED` audit entry
   - Enqueue `BackgroundJob.Enqueue<ClinicalDataExtractionJob>(j => j.ExecuteAsync(clinicalDocumentId))` — within the same HTTP request handling (within 5 s of upload completion)
   - Return HTTP 201 with `{ clinicalDocumentId, extractionStatus: "Pending" }`
3. Create `DocumentsController` at `POST /api/v1/documents/upload`; `[Authorize(Policy = "PatientPolicy")]`

## Current Project State
```
backend/
  src/
    UPACIP.Domain/Entities/ClinicalDocument.cs  (from US_005)
    UPACIP.Infrastructure/Security/PhiEncryptionService.cs  (from US_006)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/UPACIP.API/Controllers/DocumentsController.cs | Document upload endpoint |
| CREATE | backend/src/UPACIP.Application/Commands/Documents/UploadDocumentCommand.cs | Upload command |
| CREATE | backend/src/UPACIP.Application/Handlers/Documents/UploadDocumentHandler.cs | SHA-256 dedup + Storage + encrypt + audit + job enqueue |
| CREATE | backend/src/UPACIP.Infrastructure/Storage/SupabaseStorageService.cs | Supabase Storage REST adapter |

## External References
- [Supabase Storage REST API](https://supabase.com/docs/reference/javascript/storage-from-upload)
- [SHA-256 in .NET 8 — System.Security.Cryptography.SHA256](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.sha256)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Upload valid PDF → HTTP 201; ClinicalDocument row with encrypted storagePath; no binary column; audit entry; Hangfire job enqueued
- [ ] Upload same PDF again → HTTP 409 "Document already uploaded"; no new ClinicalDocument row
- [ ] Simulate Supabase 507 → HTTP 507 to client; no ClinicalDocument row created

## Implementation Checklist
- [x] SHA-256 dedup check before upload; HTTP 409 if duplicate (edge case)
- [x] Supabase Storage upload; `StorageLimitExceededException` → HTTP 507 (edge case)
- [x] Encrypt storagePath before DB persistence; no binary content in ClinicalDocument (AC-001, AC-005, DR-005)
- [x] ClinicalDocument created with extractionStatus="Pending" (AC-001)
- [x] Enqueue `ClinicalDataExtractionJob` immediately after successful record creation (AC-004)
- [x] Write `DOCUMENT_UPLOADED` audit entry (AC-001)
- [x] `[Authorize(Policy = "PatientPolicy")]`; credentials from env vars only (OWASP A01, OWASP A02)
