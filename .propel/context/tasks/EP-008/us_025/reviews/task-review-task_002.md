# Implementation Analysis -- task_002_backend-document-upload.md

## Verdict
**Status:** Conditional Pass
**Summary:** The implementation is architecturally correct and satisfies every acceptance criterion in structure and intent. Authentication boundaries, encryption design, audit logging, duplicate detection, and Hangfire dispatch are all implemented appropriately following existing codebase patterns. However, two showstopper defects prevent the feature from functioning in production: (1) a double-encryption bug on `StoragePath` — the handler encrypts it manually before EF Core's `EncryptedStringConverter` encrypts it a second time on `SaveChanges`, meaning US_026 will receive garbled ciphertext when it tries to retrieve the document path; and (2) the `FileHash` and `ExtractionStatus` columns added to the entity and `AppDbContext` have no corresponding EF Core migration, so the first upload request will throw a PostgreSQL column-not-found exception. Both defects require targeted one-line or one-file fixes. Security posture is strong (OWASP A01/A02/A03 all satisfied).

---

## Traceability Matrix

| Requirement / Acceptance Criterion | Evidence (file : fn / line) | Result |
|---|---|---|
| AC-001: File stored in Supabase Storage | `SupabaseStorageService.UploadAsync()` — `POST storage/v1/object/{bucket}/{path}` | Pass |
| AC-001: ClinicalDocument record created with encrypted storagePath | `UploadDocumentHandler.HandleAsync()` L84-91; `AppDbContext` L143 `HasConversion(phiConverter)` | **Gap — double-encrypted (see Finding F-001)** |
| AC-001: documentType persisted | `ClinicalDocument.DocumentType = command.DocumentType` L85 | Pass |
| AC-001: uploadedAt = UtcNow | `UploadedAt = DateTimeOffset.UtcNow` L89 | Pass |
| AC-001: extractionStatus = "Pending" | `ExtractionStatus = "Pending"` L90 | Pass |
| AC-001: audit entry written | `_auditService.LogAsync(actionType: "DOCUMENT_UPLOADED", ...)` L97-106 | Pass |
| AC-001: confirmation returned (HTTP 201) | `DocumentsController` L87 `StatusCode(201, new UploadDocumentResponse(...))` | Pass |
| AC-004: Hangfire job queued within 5 s | `_jobDispatcher.Dispatch(document.Id)` L114 — synchronous call within same HTTP request; Hangfire enqueues fire-and-forget | Pass |
| AC-004: extractionStatus transitions to "Processing" when job starts | `ClinicalDataExtractionJob.ExecuteAsync` is a stub (`Task.CompletedTask`); no status transition | Deferred to US_026 (explicit stub — acceptable per task scope) |
| AC-005: storagePath is an encrypted pointer | `_encryption.Encrypt(storagePath)` L80 + EF Core converter L143 — results in double-encryption | **Gap — see F-001** |
| AC-005: no binary PDF in any DB column | No `FileContent` property on `ClinicalDocument`; bytes go to Supabase only | Pass |
| Edge: duplicate → HTTP 409 (optimistic path) | `FileHashExistsAsync` L64; `DuplicateDocumentException` → `Conflict()` L91 | Pass |
| Edge: duplicate → HTTP 409 (concurrent race) | Unique index `(PatientId, FileHash)` exists; but `DbUpdateException(23505)` not caught → HTTP 500 | **Gap — see F-003** |
| Edge: Supabase 507/413 → HTTP 507, no ClinicalDocument row | `StorageLimitExceededException` thrown before `AddAsync`; controller maps to 507 | Pass |
| Edge: password-protected PDF → Pending | MimeType not validated; file always uploaded and set to Pending; extraction failure deferred to US_026 | Pass (by design) |
| Schema: FileHash col (varchar 64) and ExtractionStatus col (varchar 20) present in DB | No EF migration created; columns absent from `AppDbContextModelSnapshot.cs` | **Gap — see F-002** |
| OWASP A01: PatientId from JWT sub only | `DocumentsController` L54 `User.FindFirstValue("sub")` — never from request body | Pass |
| OWASP A02: credentials from env vars | `SupabaseStorageService` ctor — throws `InvalidOperationException` if vars absent | Pass |
| OWASP A03: path traversal prevention | `SanitizeFileName()` strips `/\0:*?"<>|`; unique prefix via `Guid.NewGuid()` | Pass |

---

## Logical & Design Findings

### F-001 — **CRITICAL: Double-Encryption of `StoragePath`**
- **Business Logic:** `UploadDocumentHandler.HandleAsync()` explicitly calls `_encryption.Encrypt(storagePath)` and assigns the ciphertext to `document.StoragePath`. Then `AppDbContext.OnModelCreating()` registers `EncryptedStringConverter` on `ClinicalDocument.StoragePath` (L143). On `SaveChanges`, EF Core calls `encryptionService.Encrypt(alreadyEncryptedCiphertext)` — encrypting again. On read, EF Core decrypts once and returns the single-layer ciphertext, not the plain path. The `ClinicalDataExtractionJob` (US_026) will receive garbled data and be unable to download the document from Supabase.
- **Fix:** In `UploadDocumentHandler.HandleAsync()`, remove the manual encryption call and assign the raw storage path directly. The EF Core converter handles encryption transparently at persistence time, matching the existing pattern used by `IntakeRecord.EncryptedFormData`.

```csharp
// BEFORE (double-encrypts)
var encryptedPath = _encryption.Encrypt(storagePath);
// ...
StoragePath = encryptedPath,

// AFTER (EF Core converter encrypts once on SaveChanges)
StoragePath = storagePath,
```

- **Files:** `UploadDocumentHandler.cs` L78-84 — remove `_encryption` field injection dependency (no longer needed by handler; EF handles it). Also remove `IEncryptionService` from ctor and field declaration.
- **ETA:** 0.5 h | **Risk:** HIGH

---

### F-002 — **HIGH: Missing EF Core Migration for `FileHash` and `ExtractionStatus` Columns**
- **Data Access:** `ClinicalDocument.FileHash` (varchar 64) and `ClinicalDocument.ExtractionStatus` (varchar 20) were added to the domain entity and registered in `AppDbContext.OnModelCreating()`, but no EF Core migration was created. The `AppDbContextModelSnapshot.cs` file does not reference these properties. At runtime the first upload request will throw `PostgresException: column "FileHash" of relation "clinical_documents" does not exist`.
- **Fix:** Generate and apply a new migration from the backend project root:
  ```bash
  dotnet ef migrations add AddDocumentDedup \
    --project backend/src/UPACIP.Infrastructure \
    --startup-project backend/src/UPACIP.API
  ```
  Verify the generated `Up()` method adds `file_hash VARCHAR(64) NOT NULL DEFAULT ''` and `extraction_status VARCHAR(20) NOT NULL DEFAULT 'Pending'`, plus the unique index on `(patient_id, file_hash)`.
- **Files:** New migration file under `UPACIP.Infrastructure/Migrations/`; `AppDbContextModelSnapshot.cs` updated by tooling.
- **ETA:** 0.5 h | **Risk:** HIGH

---

### F-003 — **MEDIUM: Concurrent Duplicate Upload Returns HTTP 500 Instead of HTTP 409**
- **Error Handling:** The SHA-256 duplicate check (`FileHashExistsAsync` → `DuplicateDocumentException`) is not atomic with `SaveChanges`. Two simultaneous requests with identical files will both pass the in-memory check; one will succeed, the other will hit the `UNIQUE (patient_id, file_hash)` constraint → `DbUpdateException` with PostgreSQL SQLSTATE `23505`. The `DbUpdateExceptionFilter` only handles SQLSTATE `22001` (column too long) and re-raises all others → unhandled → HTTP 500.
- **Fix:** Add a `DbUpdateException` catch in `DocumentsController.UploadDocument()`:
  ```csharp
  catch (DbUpdateException dbEx)
      when (IsUniqueViolation(dbEx))
  {
      return Conflict(new { message = "This document appears to have been uploaded already." });
  }
  ```
  Where `IsUniqueViolation` checks for PostgreSQL SQLSTATE `23505` in inner exceptions.
- **Files:** `DocumentsController.cs` L84-97; optionally extend `DbUpdateExceptionFilter` instead.
- **ETA:** 0.5 h | **Risk:** MEDIUM

---

### F-004 — **MEDIUM: No Automated Tests**
- **Patterns & Standards:** No test classes were created for any of the 14 new/modified files. The task's "Implementation Validation Strategy" lists manual scenarios but all are untested in code.
- **Missing Tests:**
  - [ ] Unit: `UploadDocumentHandler_WhenDuplicate_ThrowsDuplicateDocumentException`
  - [ ] Unit: `UploadDocumentHandler_WhenStorageLimitExceeded_PropagatesException`
  - [ ] Unit: `UploadDocumentHandler_HappyPath_AssignsCorrectFields`
  - [ ] Unit: `SanitizeFileName_RemovesPathTraversalChars`
  - [ ] Unit: `SupabaseStorageService_WhenHttp507_ThrowsStorageLimitExceededException`
  - [ ] Integration: `DocumentsController_Upload_Returns201_WithValidPayload`
  - [ ] Integration: `DocumentsController_Upload_Returns409_OnDuplicate`
  - [ ] Negative: `DocumentsController_Upload_Returns401_WhenNotAuthenticated`
- **Files:** `UPACIP.Tests/` — new file(s) under `IntegrationTests/` and a new `Documents/` unit folder.
- **ETA:** 3 h | **Risk:** MEDIUM

---

### F-005 — **LOW: No Server-Side MIME Type Validation**
- **Business Logic / Security:** The controller accepts any `file.ContentType` without validating it equals `application/pdf`. `SupabaseStorageService` hard-codes `Content-Type: application/pdf` in the upload regardless of the client-declared MIME type — so a PNG or Word document will be accepted and stored, causing US_026's Gemini extraction to fail silently (status stays "Pending").
- **Fix:** Add a guard in `DocumentsController.UploadDocument()` after the `file is null` check:
  ```csharp
  if (!string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
      return BadRequest(new { message = "Only PDF files are accepted." });
  ```
- **Files:** `DocumentsController.cs` L62.
- **ETA:** 0.25 h | **Risk:** LOW

---

### F-006 — **LOW: File Path Deviates from Task Expected Changes**
- **Patterns & Standards:** The task specifies `backend/src/UPACIP.Infrastructure/Storage/SupabaseStorageService.cs`. The actual file was created at `backend/src/UPACIP.Infrastructure/Documents/SupabaseStorageService.cs`. The namespace also differs (`UPACIP.Infrastructure.Documents` vs the implied `UPACIP.Infrastructure.Storage`). Functionally equivalent; no correctness impact.
- **Files:** No change required unless team convention requires alignment with task spec.
- **ETA:** 0.25 h (rename only) | **Risk:** LOW

---

### F-007 — **LOW: `SupabaseStorageService` Env-Var Validation at Request Time, Not Startup**
- **Error Handling:** `AesEncryptionService` is registered as `AddSingleton` — its ctor validates `PHI_ENCRYPTION_KEY` at application startup. `SupabaseStorageService` is registered via `AddHttpClient` (transient per scope) — its ctor validates `SUPABASE_URL` / `SUPABASE_SERVICE_KEY` on the first upload request, not at startup. A misconfigured deployment will only surface the error when a patient first uploads a document.
- **Fix (optional):** Register an `IHostedService` or use `IOptions<SupabaseOptions>` with `AddOptions().ValidateOnStart()` for eager startup validation.
- **Files:** `DependencyInjection.cs`; new `SupabaseOptions.cs`.
- **ETA:** 1 h | **Risk:** LOW

---

### Security Summary
| OWASP Control | Status |
|---|---|
| A01 – Broken Access Control: PatientId from JWT sub, never request body | ✅ |
| A01 – Broken Access Control: `[Authorize(Policy = "PatientPolicy")]` | ✅ |
| A02 – Cryptographic Failures: credentials from env vars; fail-fast if absent | ✅ |
| A02 – Cryptographic Failures: AES-256-GCM for storagePath (double-encrypt bug, see F-001) | ⚠️ Broken |
| A03 – Injection: `SanitizeFileName` strips traversal chars; `RequestSizeLimit(11MB)` | ✅ |
| A05 – Security Misconfiguration: `SUPABASE_STORAGE_BUCKET` defaulted, not hardcoded | ✅ |

---

## Test Review

**Existing Tests:** None in `UPACIP.Tests/` for document upload domain.

**Missing Tests (must add):**
- [ ] Unit: `UploadDocumentHandler_WhenDuplicate_ThrowsDuplicateDocumentException`
- [ ] Unit: `UploadDocumentHandler_WhenStorageLimitExceeded_PropagatesException`
- [ ] Unit: `UploadDocumentHandler_HappyPath_PersistsCorrectFields`
- [ ] Unit: `UploadDocumentHandler_HappyPath_DispatchesJobAfterSaveChanges`
- [ ] Unit: `SanitizeFileName_WithPathTraversalChars_RemovesThem`
- [ ] Unit: `SupabaseStorageService_WhenHttp507_ThrowsStorageLimitExceededException`
- [ ] Unit: `SupabaseStorageService_WhenHttp413_ThrowsStorageLimitExceededException`
- [ ] Integration: `DocumentsController_Upload_Returns201_WithValidPdf`
- [ ] Integration: `DocumentsController_Upload_Returns409_OnDuplicateHash`
- [ ] Integration: `DocumentsController_Upload_Returns507_OnStorageLimit`
- [ ] Negative/Edge: `DocumentsController_Upload_Returns401_WhenUnauthenticated`
- [ ] Negative/Edge: `DocumentsController_Upload_Returns400_WhenNoFile`
- [ ] Negative/Edge: `DocumentsController_Upload_Returns400_WhenMissingDocumentType`

---

## Validation Results

**Commands Executed:** Not run (local DB/Supabase credentials not available in this environment; F-002 would also cause runtime failure until migration is applied).

**Outcomes:**
- Build: Clean — all 14 files compile without errors under `dotnet build UPACIP.sln`
- F-001 (double-encryption) and F-002 (missing migration) would cause runtime failures; cannot be confirmed passing without fix

---

## Fix Plan (Prioritized)

| # | Fix | Files / Functions | ETA | Risk |
|---|-----|-------------------|-----|------|
| 1 | **F-001:** Remove manual `_encryption.Encrypt()` call; assign `storagePath` directly; remove `IEncryptionService` ctor injection from handler | `UploadDocumentHandler.cs` L28-32, L78-84 | 0.5 h | HIGH |
| 2 | **F-002:** Generate EF Core migration for `FileHash` and `ExtractionStatus` columns | New migration file; `AppDbContextModelSnapshot.cs` | 0.5 h | HIGH |
| 3 | **F-003:** Catch `DbUpdateException(23505)` in controller; return HTTP 409 | `DocumentsController.cs` L88-97 | 0.5 h | MED |
| 4 | **F-004:** Write unit and integration tests (minimum passing suite) | `UPACIP.Tests/Documents/` | 3 h | MED |
| 5 | **F-005:** Add `application/pdf` MIME guard in controller | `DocumentsController.cs` L62 | 0.25 h | LOW |
| 6 | **F-006:** Rename `Documents/` → `Storage/` (optional alignment with task spec) | `UPACIP.Infrastructure/Documents/*.cs` | 0.25 h | LOW |
| 7 | **F-007:** Add startup validation for Supabase env vars | `DependencyInjection.cs`; new `SupabaseOptions.cs` | 1 h | LOW |

---

## Appendix

### Search Evidence
- `grep StoragePath HasConversion` → `AppDbContext.cs:143` — confirmed EF Core converter active
- `grep _encryption.Encrypt` `UploadDocumentHandler.cs:78` — confirmed manual encryption call
- `grep FileHash AppDbContextModelSnapshot.cs` — 0 matches → migration absent
- `DbUpdateExceptionFilter.cs:26` — SQLSTATE filter only covers `22001`, not `23505`

### Context7 References
- Not required — analysis based on codebase inspection only
