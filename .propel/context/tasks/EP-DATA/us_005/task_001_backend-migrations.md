# Task - TASK_001

## Requirement Reference
- **User Story:** us_005
- **Story Location:** .propel/context/tasks/EP-DATA/us_005/us_005.md
- **Acceptance Criteria:**
  - AC-001: EF Core migrations for all 15+ domain entities apply cleanly; `dotnet ef database update` exits code 0 with zero failures
  - AC-003: FK constraints enforced; invalid FK inserts are rejected by PostgreSQL
  - AC-004: InsuranceRecord dummy seed data loaded; `SELECT COUNT(*) FROM insurance_records` returns ≥ 1 row with non-null providerName and insuranceIdPattern
- **Edge Cases:**
  - Migration applied to non-empty DB → `dotnet ef database update` is idempotent; already-applied migrations skipped; no data loss
  - pgvector extension not available → migration step fails with descriptive error; pipeline exits non-zero

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
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | TR-002, NFR-012 — Infrastructure project hosts EF Core migrations |
| Database | EF Core (Npgsql provider) | 8.x | TR-003, DR-002 — ORM for PostgreSQL; Npgsql maps .NET types to PG types |
| Database | PostgreSQL via Supabase (+ pgvector) | PostgreSQL 15; pgvector 0.7+ | DR-001–DR-007, TR-003 — primary relational store; pgvector for AI embeddings |

---

## Task Overview
Define all 15+ domain entity classes in the `UPACIP.Domain` layer with full EF Core fluent API configuration in the Infrastructure `AppDbContext`. This includes FK relationships, `rowVersion` concurrency tokens on `AppointmentSlot` and `Appointment`, the pgvector extension migration, and the `InsuranceRecord` seeder. The resulting migration file (`InitialCreate`) is the sole source of truth for the database schema and must apply cleanly and idempotently to Supabase PostgreSQL.

## Dependent Tasks
- `task_001_backend-scaffold.md` (US_002) — .NET solution and `AppDbContext` stub must exist

## Impacted Components
- `backend/src/UPACIP.Domain/Entities/` — all 15 entity classes
- `backend/src/UPACIP.Infrastructure/Persistence/AppDbContext.cs` — EF Core DbSet registrations and fluent API configurations
- `backend/src/UPACIP.Infrastructure/Persistence/Migrations/` — generated migration files
- `backend/src/UPACIP.Infrastructure/Persistence/Seeders/InsuranceRecordSeeder.cs` — new dummy data seeder

## Implementation Plan
1. Create entity classes in `UPACIP.Domain/Entities/`: `User`, `Appointment`, `AppointmentSlot`, `WaitlistEntry`, `IntakeRecord`, `ClinicalDocument`, `ExtractedClinicalData`, `PatientProfile360`, `DataConflict`, `MedicalCodeSuggestion`, `AuditLog`, `Notification`, `InsuranceRecord`, `CalendarSync`, `VerifiedMedicalCode` — each inheriting `BaseEntity`
2. Register all `DbSet<T>` properties in `AppDbContext`
3. Configure fluent API in `OnModelCreating`: column data types, `NOT NULL` constraints, string max lengths, enum conversions (role, status fields as strings)
4. Configure FK relationships with `DeleteBehavior.Restrict` to prevent cascading deletes on PHI entities; add `rowVersion` concurrency token to `Appointment` and `AppointmentSlot`
5. Add `CREATE EXTENSION IF NOT EXISTS vector` as a raw SQL migration step for pgvector (DR-004); vector columns on future AI entities stubbed as `float[]` with pgvector mapping
6. Create `InsuranceRecordSeeder` using `HasData` in `OnModelCreating`; seed ≥ 3 dummy insurance records with non-null `providerName` and `insuranceIdPattern` (regex string)
7. Run `dotnet ef migrations add InitialCreate` to generate the migration file; review SQL output for completeness
8. Verify migration applies cleanly locally with `dotnet ef database update --connection <local-or-Supabase-string>`

## Current Project State
```
backend/
  src/
    UPACIP.Domain/Entities/BaseEntity.cs  (created in US_002)
    UPACIP.Infrastructure/Persistence/AppDbContext.cs  (stub from US_002)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/UPACIP.Domain/Entities/User.cs | User entity (id, email, passwordHash, role, status, etc.) |
| CREATE | backend/src/UPACIP.Domain/Entities/Appointment.cs | Appointment entity with rowVersion concurrency token |
| CREATE | backend/src/UPACIP.Domain/Entities/AppointmentSlot.cs | AppointmentSlot entity with rowVersion concurrency token |
| CREATE | backend/src/UPACIP.Domain/Entities/WaitlistEntry.cs | WaitlistEntry entity |
| CREATE | backend/src/UPACIP.Domain/Entities/IntakeRecord.cs | IntakeRecord entity (fields as encrypted JSON placeholder) |
| CREATE | backend/src/UPACIP.Domain/Entities/ClinicalDocument.cs | ClinicalDocument entity (storagePath placeholder) |
| CREATE | backend/src/UPACIP.Domain/Entities/ExtractedClinicalData.cs | ExtractedClinicalData entity (encrypted JSON columns) |
| CREATE | backend/src/UPACIP.Domain/Entities/PatientProfile360.cs | PatientProfile360 entity |
| CREATE | backend/src/UPACIP.Domain/Entities/DataConflict.cs | DataConflict entity |
| CREATE | backend/src/UPACIP.Domain/Entities/MedicalCodeSuggestion.cs | MedicalCodeSuggestion entity |
| CREATE | backend/src/UPACIP.Domain/Entities/AuditLog.cs | AuditLog entity (no FK enforcement — actor may be deleted) |
| CREATE | backend/src/UPACIP.Domain/Entities/Notification.cs | Notification entity |
| CREATE | backend/src/UPACIP.Domain/Entities/InsuranceRecord.cs | InsuranceRecord entity (read-only reference data) |
| CREATE | backend/src/UPACIP.Domain/Entities/CalendarSync.cs | CalendarSync entity (encrypted token columns) |
| CREATE | backend/src/UPACIP.Domain/Entities/VerifiedMedicalCode.cs | VerifiedMedicalCode entity |
| MODIFY | backend/src/UPACIP.Infrastructure/Persistence/AppDbContext.cs | Add DbSets, fluent API configurations, pgvector migration, seeder |
| CREATE | backend/src/UPACIP.Infrastructure/Persistence/Seeders/InsuranceRecordSeeder.cs | Dummy InsuranceRecord seed data (≥ 3 records) |
| CREATE | backend/src/UPACIP.Infrastructure/Persistence/Migrations/[timestamp]_InitialCreate.cs | EF Core-generated migration file |

## External References
- [EF Core 8 Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Npgsql EF Core Provider](https://www.npgsql.org/efcore/)
- [pgvector .NET (Npgsql.EntityFrameworkCore.PostgreSQL)](https://github.com/pgvector/pgvector-dotnet)
- [EF Core Concurrency Tokens](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] `dotnet ef migrations add InitialCreate` generates a non-empty migration file
- [ ] `dotnet ef database update` against Supabase exits code 0; all tables created
- [ ] Attempt to insert an `Appointment` with non-existent `patientId` → PostgreSQL returns FK violation error
- [ ] `SELECT COUNT(*) FROM insurance_records` returns ≥ 3; `providerName` and `insuranceIdPattern` are non-null

## Implementation Checklist
- [x] Create all 15 entity classes in `UPACIP.Domain/Entities/`; each inherits `BaseEntity` with UUID `id` (AC-001)
- [x] Register all `DbSet<T>` in `AppDbContext`; configure column types, NOT NULL constraints, string max lengths via fluent API (AC-001)
- [x] Configure FK relationships with `DeleteBehavior.Restrict`; add `rowVersion` concurrency token on `Appointment` and `AppointmentSlot` (AC-003)
- [x] Add pgvector extension SQL migration step (`CREATE EXTENSION IF NOT EXISTS vector`) (AC-002 — consumed by task_002)
- [x] Seed ≥ 3 `InsuranceRecord` rows with non-null `providerName` and `insuranceIdPattern` via `HasData` (AC-004)
- [x] Create `InsuranceRecordSeeder.cs` and wire into `AppDbContext.OnModelCreating` (AC-004)
- [x] Run `dotnet ef migrations add InitialCreate`; review generated SQL for all 15 tables and constraints (AC-001)
- [x] Verify migration is idempotent: run `dotnet ef database update` twice; second run exits code 0 with "No pending migrations" (AC-001 edge case)
