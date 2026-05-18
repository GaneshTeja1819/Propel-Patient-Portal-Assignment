# Task - TASK_001

## Requirement Reference
- **User Story:** us_006
- **Story Location:** .propel/context/tasks/EP-DATA/us_006/us_006.md
- **Acceptance Criteria:**
  - AC-001: PHI columns store ciphertext in PostgreSQL; application layer decrypts correctly on read
  - AC-002: AES-256-GCM key loaded from env var only; zero raw key values in any committed file
  - AC-004: Audit log entry written for every tracked action with non-null actorId, actorRole, actionType, targetEntity, targetId, timestamp
  - AC-005: `storagePath` column stores encrypted path string; no binary PDF content in relational table
  - AC-006: `DELETE /api/v1/patients/{id}/data` returns HTTP 202; audit log entry written with actorId, timestamp, targetId
- **Edge Cases:**
  - Encryption key rotation → re-encryption tooling required before old key decommission; zero downtime
  - PHI field value exceeds max ciphertext column length → `DbUpdateException` raised; HTTP 422 returned with field-level validation error

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
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | TR-002, NFR-004 — hosts encryption service and audit log service |
| Backend | ASP.NET Core Data Protection (AES-256-GCM) | Built-in .NET 8 | NFR-004, NFR-007, TR-015 — PHI encryption at application layer; keys from env vars |
| Database | PostgreSQL via Supabase | 15 | DR-001, DR-003 — ciphertext stored in PostgreSQL columns; audit schema |

---

## Task Overview
Implement the `IEncryptionService` / `AesEncryptionService` using AES-256-GCM with the key loaded exclusively from environment variables. Wire the service as an EF Core value converter on all PHI columns (`IntakeRecord.fields`, `ExtractedClinicalData.vitals/medications/diagnoses/rawGeminiResponse`, `CalendarSync.accessToken/refreshToken`, `ClinicalDocument.storagePath`). Implement `IAuditLogService` and an `AppDbContext` save-changes interceptor that writes an `audit.audit_log` entry on every tracked action. Scaffold the HIPAA patient data deletion endpoint (`DELETE /api/v1/patients/{id}/data`).

## Dependent Tasks
- `task_001_backend-migrations.md` (US_005) — PHI entity tables must exist
- `task_002_database-audit-schema.md` (US_006) — `audit.audit_log` table must exist before the service can write to it

## Impacted Components
- `backend/src/UPACIP.Application/Interfaces/IEncryptionService.cs` — new interface
- `backend/src/UPACIP.Infrastructure/Security/AesEncryptionService.cs` — new AES-256-GCM implementation
- `backend/src/UPACIP.Infrastructure/Persistence/Converters/EncryptedStringConverter.cs` — new EF Core value converter
- `backend/src/UPACIP.Application/Interfaces/IAuditLogService.cs` — new interface
- `backend/src/UPACIP.Infrastructure/Audit/AuditLogService.cs` — new implementation
- `backend/src/UPACIP.Infrastructure/Persistence/Interceptors/AuditSaveChangesInterceptor.cs` — new EF Core interceptor
- `backend/src/UPACIP.API/Controllers/PatientDataController.cs` — new deletion endpoint

## Implementation Plan
1. Define `IEncryptionService` in Application layer: `Encrypt(string plaintext): string`, `Decrypt(string ciphertext): string`
2. Implement `AesEncryptionService` using `System.Security.Cryptography.AesGcm`; load 32-byte key from `Environment.GetEnvironmentVariable("PHI_ENCRYPTION_KEY")`; throw `InvalidOperationException` at startup if key is absent or not 32 bytes
3. Create `EncryptedStringConverter` as EF Core `ValueConverter<string, string>` that calls `Encrypt` on write and `Decrypt` on read
4. Apply `EncryptedStringConverter` in `AppDbContext.OnModelCreating` on all PHI columns: `IntakeRecord.fields`, `ExtractedClinicalData.vitals/medications/diagnoses/rawGeminiResponse`, `CalendarSync.accessToken/refreshToken`, `ClinicalDocument.storagePath`
5. Define `IAuditLogService` in Application layer: `LogAsync(Guid actorId, string actorRole, string actionType, string targetEntity, Guid targetId)`
6. Implement `AuditLogService` in Infrastructure: insert directly into `audit.audit_log` using ADO.NET (bypassing EF Core to avoid recursive interceptor calls); uses INSERT-only DB role
7. Create `AuditSaveChangesInterceptor` implementing `ISaveChangesInterceptor`; on `SavingChangesAsync`, detect tracked entity changes and call `IAuditLogService.LogAsync` for auditable entity types
8. Register encryption service, audit service, and interceptor in `Program.cs` DI; scaffold `DELETE /api/v1/patients/{id}/data` returning HTTP 202 with audit entry

## Current Project State
```
backend/
  src/
    UPACIP.Application/Interfaces/  (IRepository, IUnitOfWork from US_002)
    UPACIP.Infrastructure/Persistence/AppDbContext.cs  (entities from US_005)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/UPACIP.Application/Interfaces/IEncryptionService.cs | Encrypt/Decrypt interface |
| CREATE | backend/src/UPACIP.Infrastructure/Security/AesEncryptionService.cs | AES-256-GCM implementation; key from env var |
| CREATE | backend/src/UPACIP.Infrastructure/Persistence/Converters/EncryptedStringConverter.cs | EF Core value converter for PHI columns |
| MODIFY | backend/src/UPACIP.Infrastructure/Persistence/AppDbContext.cs | Apply EncryptedStringConverter to all PHI columns |
| CREATE | backend/src/UPACIP.Application/Interfaces/IAuditLogService.cs | Audit log write interface |
| CREATE | backend/src/UPACIP.Infrastructure/Audit/AuditLogService.cs | ADO.NET-based audit log INSERT implementation |
| CREATE | backend/src/UPACIP.Infrastructure/Persistence/Interceptors/AuditSaveChangesInterceptor.cs | EF Core interceptor triggering audit writes |
| MODIFY | backend/src/UPACIP.API/Program.cs | Register encryption, audit services, and interceptor in DI |
| CREATE | backend/src/UPACIP.API/Controllers/PatientDataController.cs | DELETE /api/v1/patients/{id}/data returning HTTP 202 |

## External References
- [System.Security.Cryptography.AesGcm (.NET 8)](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm)
- [EF Core Value Converters](https://learn.microsoft.com/en-us/ef/core/modeling/value-conversions)
- [EF Core Interceptors (ISaveChangesInterceptor)](https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors)
- [HIPAA Minimum Necessary Standard (45 CFR §164.502(b))](https://www.hhs.gov/hipaa/for-professionals/privacy/guidance/minimum-necessary-requirement/index.html)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Save an `IntakeRecord` with a plaintext `fields` value; read the raw column in Supabase SQL editor — confirm ciphertext, not plaintext
- [ ] Application reads the same record via EF Core — confirm correct plaintext is returned after decryption
- [ ] Static grep of repository confirms zero occurrences of `PHI_ENCRYPTION_KEY` value in committed files
- [ ] Trigger an auditable action (e.g., user creation); confirm row in `audit.audit_log` with all required fields
- [ ] `DELETE /api/v1/patients/{id}/data` returns HTTP 202; audit row written

## Implementation Checklist
- [x] Implement `AesEncryptionService` with AES-256-GCM; load key from env var; throw at startup if key absent or wrong length (AC-001, AC-002)
- [x] Create `EncryptedStringConverter` and apply to all PHI columns in `AppDbContext.OnModelCreating` (AC-001)
- [ ] Verify ciphertext at rest: save entity, read raw column — confirm unreadable without decryption (AC-001)
- [x] Implement `AuditLogService` with ADO.NET INSERT into `audit.audit_log`; wire `AuditSaveChangesInterceptor` (AC-004)
- [x] Confirm `storagePath` column stores encrypted path string; no binary content in DB row (AC-005)
- [x] Scaffold `DELETE /api/v1/patients/{id}/data`; return HTTP 202; write audit entry with actorId, timestamp, targetId (AC-006)
- [x] Verify encryption key not in any committed source file (AC-002)
- [x] Add `DbUpdateException` handler for ciphertext column overflow; return HTTP 422 with field-level error (AC-001 edge case)
