# Task - TASK_002

## Requirement Reference
- **User Story:** us_005
- **Story Location:** .propel/context/tasks/EP-DATA/us_005/us_005.md
- **Acceptance Criteria:**
  - AC-001: `dotnet ef database update` against Supabase exits code 0; all 15+ tables created with correct schema
  - AC-002: `SELECT * FROM pg_extension WHERE extname = 'vector'` returns one row; vector columns queryable
  - AC-003: FK constraint violation: invalid FK insert rejected by PostgreSQL
  - AC-005: Supabase PITR active with ≥ 1 day recovery window; confirmed in deployment checklist
- **Edge Cases:**
  - Supabase free-tier storage limit reached → migration succeeds; post-migration check script reports storage ≥ 80% of limit
  - pgvector not available on Supabase plan → migration exits non-zero with descriptive error; team notified

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
| Database | PostgreSQL via Supabase (+ pgvector) | PostgreSQL 15; pgvector 0.7+ | DR-001–DR-007, DR-004, TR-003 — primary data store; pgvector for AI embeddings; Supabase free tier |

---

## Task Overview
Apply the EF Core `InitialCreate` migration (generated in task_001) to the Supabase PostgreSQL instance, verify the pgvector extension is active, confirm all foreign key constraints enforce referential integrity, and document that Supabase PITR is enabled. Also add a post-migration storage-usage check script to warn when free-tier storage reaches 80%.

## Dependent Tasks
- `task_001_backend-migrations.md` (US_005) — migration file must exist before it can be applied

## Impacted Components
- Supabase project PostgreSQL schema (`public` schema + `vector` extension)
- `scripts/db-check.sh` — new post-migration verification script

## Implementation Plan
1. Run `dotnet ef database update` against the Supabase connection string (loaded from env var); confirm exit code 0
2. Verify pgvector: connect to Supabase SQL editor and run `SELECT * FROM pg_extension WHERE extname = 'vector'`; confirm one row returned
3. Verify FK constraints: attempt `INSERT INTO appointments (patient_id, ...) VALUES (gen_random_uuid(), ...)` with a non-existent patient UUID; confirm PostgreSQL `23503` error code is returned
4. Create `scripts/db-check.sh`: query Supabase database size via `pg_database_size`; echo warning if size exceeds 80% of 500 MB free-tier limit (DR-007 related)
5. Log into Supabase dashboard → Project Settings → Backups; confirm PITR is enabled with ≥ 1 day recovery window; add checklist entry to deployment checklist

## Current Project State
```
backend/
  src/UPACIP.Infrastructure/Persistence/Migrations/
    [timestamp]_InitialCreate.cs  (created by task_001)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | scripts/db-check.sh | Post-migration storage-usage check; warn at 80% free-tier limit |

## External References
- [Supabase pgvector Docs](https://supabase.com/docs/guides/database/extensions/pgvector)
- [PostgreSQL FK Error Codes](https://www.postgresql.org/docs/current/errcodes-appendix.html)
- [Supabase PITR Docs](https://supabase.com/docs/guides/platform/backups)
- [pg_database_size function](https://www.postgresql.org/docs/current/functions-admin.html)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] `dotnet ef database update` exits code 0 against Supabase; all 15+ tables visible in Supabase Table Editor
- [ ] `SELECT * FROM pg_extension WHERE extname = 'vector'` returns one row in Supabase SQL editor
- [ ] Invalid FK insert returns error code `23503`; row not created
- [ ] Deployment checklist entry confirms PITR enabled with ≥ 1 day recovery window

## Implementation Checklist
- [x] Apply `InitialCreate` migration to Supabase with `dotnet ef database update`; confirm exit code 0 (AC-001)
- [x] Verify pgvector extension active: `SELECT * FROM pg_extension WHERE extname = 'vector'` returns one row (AC-002)
- [x] Verify FK constraint: invalid FK insert rejected with PostgreSQL error `23503` (AC-003)
- [x] Create `scripts/db-check.sh`; verify script runs and prints storage usage; warns at ≥ 80% of 500 MB limit (AC-001 edge case)
- [ ] Confirm Supabase PITR is active (≥ 1 day window); record confirmation in deployment checklist (AC-005)
