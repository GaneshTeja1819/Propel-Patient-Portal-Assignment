# Implementation Analysis — task_002_database-apply-migrations

## Verdict

**Status:** Conditional Pass  
**Summary:** The `InitialCreate` migration was applied successfully to the Supabase PostgreSQL instance (exit code 0, "Done" — confirmed 17 May 2026). All 15 domain tables were created with FK constraints using `ReferentialAction.Restrict`, the pgvector extension annotation is present in the migration, and three InsuranceRecord seed rows were inserted. `scripts/db-check.sh` has been created to automate post-deploy verification. Two items remain open: (1) live database confirmation checks (AC-002, AC-003, AC-004) cannot be re-executed until `appsettings.json` carries real credentials (currently contains `[YOUR-PASSWORD]` placeholder) — evidence is from migration source only; (2) Supabase PITR enable/verify step (AC-005) requires manual Supabase dashboard access and cannot be automated.

---

## Traceability Matrix

| Requirement / Acceptance Criterion | Evidence | Result |
|---|---|---|
| **AC-001** — Migration applied idempotently (exit code 0) | Terminal output: `Applying migration '20260517145221_InitialCreate'` → `Done.` (17 May 2026, exit code 0) | **Pass** |
| **AC-001 edge** — Table count ≥ 15 | Migration file: 15 `CreateTable` calls — `audit_logs`, `insurance_records`, `users`, `appointment_slots`, `calendar_syncs`, `data_conflicts`, `notifications`, `patient_profiles_360`, `waitlist_entries`, `appointments`, `clinical_documents`, `intake_records`, `extracted_clinical_data`, `medical_code_suggestions`, `verified_medical_codes` | **Pass (code evidence)** |
| **AC-001 edge** — `__EFMigrationsHistory` row present | `dotnet ef database update` exit 0 inserts the row automatically; `scripts/db-check.sh` check [1] confirms at runtime | **Pass (inferred)** |
| **AC-002** — pgvector extension active | `20260517145221_InitialCreate.cs` L1–10: `migrationBuilder.AlterDatabase().Annotation("Npgsql:PostgresExtension:vector", ",,")` → emits `CREATE EXTENSION IF NOT EXISTS vector` | **Pass (code evidence)** |
| **AC-002** — Live DB row in `pg_extension` | Cannot re-execute `SELECT * FROM pg_extension WHERE extname='vector'` — credentials placeholder in `appsettings.json`; `scripts/db-check.sh` check [2] verifies at runtime | **Gap (credentials)** |
| **AC-003** — All FKs use `ON DELETE RESTRICT` | Migration: all 16 FK definitions carry `onDelete: ReferentialAction.Restrict`; `scripts/db-check.sh` check [4] confirms 23503 rejection at runtime | **Pass (code evidence)** |
| **AC-004** — ≥ 3 InsuranceRecord seed rows | Migration `InsertData`: 3 rows — GUIDs `...0001/0002/0003`; Blue Cross Blue Shield, Aetna, United Healthcare | **Pass (code evidence)** |
| **AC-005** — Supabase PITR enabled | No automated path; requires Supabase Dashboard → Project Settings → Add-ons → Point-in-Time Recovery | **Gap (manual)** |
| **Edge** — `scripts/db-check.sh` exists and covers storage warn | `scripts/db-check.sh` created; check [6] warns when `pg_database_size` ≥ 419 MB (80% of 500 MB) | **Pass** |

---

## Logical & Design Findings

- **Business Logic:** `AuditLog.ActorId` has no FK to `users` — intentional (actor may be a deleted user). This is correct for audit trails but means orphaned actor IDs are not caught at the DB layer. Accept as-is.
- **Security:** `appsettings.json` carries `[YOUR-PASSWORD]` placeholder — passwords must be supplied via `dotnet user-secrets` (local) or environment variable `ConnectionStrings__DefaultConnection` (CI/CD). Do not commit real credentials. Current state is compliant.
- **Security:** `EncryptedFormData` (intake_records) and `EncryptedExtractedJson` (extracted_clinical_data) are stored as plain `text` columns — encryption is expected at the application layer before persistence. This pattern is correct provided the application actually encrypts; no DB-layer enforcement.
- **Error Handling:** `AppDbContextDesignTimeFactory` searches three `appsettings.json` locations and falls back to a localhost stub. The stub fallback will produce a runtime error for `dotnet ef` commands run from unexpected directories — acceptable for dev-time tooling.
- **Data Access:** `patient_profiles_360` has a unique index on `PatientId` enforcing 1:1 with `users`. `verified_medical_codes` has a unique index on `SuggestionId` enforcing 1:1 with `medical_code_suggestions`. Both correct.
- **Performance:** All FK columns have corresponding indexes (`IX_*`). `users.Email` has a unique index. No N+1 concerns at schema level.
- **Patterns & Standards:** Migration follows snake_case table naming convention throughout. All FK deletes use `Restrict` (no implicit cascades). Row-version (`xmin`/`xid`) applied to `appointments` and `appointment_slots` for optimistic concurrency. Consistent with project EF Core conventions.

---

## Test Review

- **Existing Tests:** None covering the migration or database schema at time of review.
- **Missing Tests (must add):**
  - [ ] Integration: Run `scripts/db-check.sh` against a live Supabase instance and assert exit code 0 as part of post-deploy smoke test
  - [ ] Integration: Verify `SELECT extname FROM pg_extension WHERE extname='vector'` returns a row after migration
  - [ ] Negative/Edge: Verify FK 23503 rejection — attempt INSERT into `appointments` with non-existent `PatientId`
  - [ ] Negative/Edge: Storage warning threshold — mock `pg_database_size` returning ≥ 419430400 bytes, confirm `db-check.sh` prints `WARN`
  - [ ] Unit: `AppDbContextDesignTimeFactory` falls back correctly when no `appsettings.json` exists in any search path

---

## Validation Results

- **Commands Executed:**
  1. `dotnet ef database update` (from `backend/` CWD, using `AppDbContextDesignTimeFactory` reading pooler connection string from `appsettings.json`)
  2. Static analysis of `20260517145221_InitialCreate.cs` (lines 1–530)
- **Outcomes:**
  - Command 1: Exit code 0 — "Applying migration '20260517145221_InitialCreate'... Done." (17 May 2026)
  - Command 2: All 15 tables confirmed; all FKs `ReferentialAction.Restrict`; pgvector annotation present; 3 seed rows confirmed; `xmin`/`xid` rowVersion on `appointments` + `appointment_slots`; `Down()` method present
  - Live DB re-verification (checks [2]–[5]) blocked by `[YOUR-PASSWORD]` placeholder in `appsettings.json` — run `scripts/db-check.sh` with real credentials to confirm

---

## Fix Plan (Prioritized)

1. **Confirm pgvector + FK + seed live** — Replace `[YOUR-PASSWORD]` in `appsettings.json` (or set `ConnectionStrings__DefaultConnection` env var), run `./scripts/db-check.sh` — `appsettings.json` — ETA 0.1 h — Risk: **L**
2. **Enable PITR (AC-005)** — Supabase Dashboard → Project Settings → Add-ons → Point-in-Time Recovery → Enable — ETA 0.1 h — Risk: **L** (manual, no code change)
3. **Move credentials to user-secrets** — `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<real-conn>"` for local dev; confirm `AppDbContextDesignTimeFactory` reads via `CreateDefaultBuilder` host (optional improvement) — `AppDbContextDesignTimeFactory.cs` — ETA 0.5 h — Risk: **L**

---

## Appendix

- **Search Evidence:**
  - `20260517145221_InitialCreate.cs` lines 1–530 — 15 table definitions, 16 FK constraints, 3 seed inserts, index definitions, `Down()` method
  - `task_002_database-apply-migrations.md` — AC definitions and checklist
  - `appsettings.json` — pooler connection string with `[YOUR-PASSWORD]` placeholder
  - `AppDbContextDesignTimeFactory.cs` — multi-path `appsettings.json` resolver
  - `scripts/db-check.sh` — created; checks [1]–[6]
