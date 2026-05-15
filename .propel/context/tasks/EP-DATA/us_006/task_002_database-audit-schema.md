# Task - TASK_002

## Requirement Reference
- **User Story:** us_006
- **Story Location:** .propel/context/tasks/EP-DATA/us_006/us_006.md
- **Acceptance Criteria:**
  - AC-003: Application database role can only INSERT and SELECT on `audit.audit_log`; UPDATE and DELETE return PostgreSQL `42501` permission denied
- **Edge Cases:**
  - Application role accidentally granted UPDATE on audit schema → integration test asserts HTTP 42501; CI gate fails if permission check does not pass

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
| Database | PostgreSQL via Supabase | 15 | DR-003, NFR-007, TR-014 — dedicated `audit` schema with INSERT-only application role |

---

## Task Overview
Create the dedicated `audit` PostgreSQL schema and `audit.audit_log` table. Grant the application database role INSERT and SELECT privileges only — no UPDATE or DELETE. This enforces immutability at the database permission level per DR-003 and NFR-007, independent of application logic. A separate read-only role is granted SELECT for audit review queries.

## Dependent Tasks
- `task_002_database-apply-migrations.md` (US_005) — Supabase PostgreSQL instance must be accessible

## Impacted Components
- Supabase PostgreSQL — `audit` schema and `audit.audit_log` table
- `scripts/audit-schema.sql` — new SQL script for schema, table, and permission grants

## Implementation Plan
1. Create `scripts/audit-schema.sql` containing:
   - `CREATE SCHEMA IF NOT EXISTS audit`
   - `CREATE TABLE IF NOT EXISTS audit.audit_log (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), actor_id uuid, actor_role text NOT NULL, action_type text NOT NULL, target_entity text NOT NULL, target_id uuid, timestamp timestamptz NOT NULL DEFAULT now(), metadata text)`
   - `REVOKE ALL ON audit.audit_log FROM PUBLIC`
   - `GRANT INSERT, SELECT ON audit.audit_log TO <app_role>`
   - `CREATE ROLE audit_reader; GRANT SELECT ON audit.audit_log TO audit_reader`
2. Run the script against Supabase using the Supabase SQL editor or `psql`
3. Verify permissions: using the application role, attempt `UPDATE audit.audit_log SET actor_role = 'x' WHERE false` — confirm PostgreSQL error `42501`
4. Add an integration test assertion in `UPACIP.Tests` that executes an UPDATE against `audit.audit_log` using the application connection string and asserts `PostgresException` with `SqlState = "42501"`

## Current Project State
```
Supabase PostgreSQL:
  public schema  (all entity tables from US_005/task_002)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | scripts/audit-schema.sql | CREATE SCHEMA, CREATE TABLE, REVOKE, GRANT for audit.audit_log |
| CREATE | backend/tests/UPACIP.Tests/IntegrationTests/AuditPermissionTest.cs | xUnit integration test asserting UPDATE returns 42501 |

## External References
- [PostgreSQL GRANT Statement](https://www.postgresql.org/docs/current/sql-grant.html)
- [PostgreSQL Schema Docs](https://www.postgresql.org/docs/current/ddl-schemas.html)
- [Npgsql PostgresException.SqlState](https://www.npgsql.org/doc/api/Npgsql.PostgresException.html)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] `audit.audit_log` table exists in Supabase with all required columns
- [ ] Application role `UPDATE` on `audit.audit_log` returns PostgreSQL `42501` error
- [ ] Application role `INSERT` on `audit.audit_log` succeeds
- [ ] `AuditPermissionTest.cs` xUnit test passes in CI

## Implementation Checklist
- [ ] Create `scripts/audit-schema.sql`; run against Supabase; confirm `audit.audit_log` table created (AC-003)
- [ ] REVOKE all privileges from PUBLIC; GRANT INSERT + SELECT to application role only (AC-003)
- [ ] Verify application role UPDATE returns `42501` via Supabase SQL editor or psql (AC-003)
- [ ] Create `AuditPermissionTest.cs` asserting `PostgresException` with `SqlState = "42501"` on UPDATE attempt; run in CI (AC-003 edge case)
