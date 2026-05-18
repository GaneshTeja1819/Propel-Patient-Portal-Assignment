-- =============================================================================
-- audit-schema.sql — HIPAA-compliant audit schema for UPACIP
-- =============================================================================
-- Purpose : Create the dedicated `audit` PostgreSQL schema and `audit.audit_log`
--           table. Enforce immutability by granting the application database role
--           INSERT and SELECT only — no UPDATE or DELETE (DR-003, NFR-007, AC-003).
--
-- Prerequisites:
--   • Run once as a PostgreSQL superuser (e.g. the Supabase `postgres` role).
--   • The public schema tables from US_005/task_002 must already exist.
--
-- Usage in Supabase:
--   1. Open the Supabase SQL editor.
--   2. Replace every occurrence of <app_role> below with your application's
--      PostgreSQL role name (e.g. the dedicated service role you created, NOT the
--      superuser `postgres`).  If you have not created a dedicated role yet, do so
--      first:
--        CREATE ROLE upacip_app WITH LOGIN PASSWORD '<strong-password>';
--   3. Paste and run this script.
--   4. Run scripts/db-check.sh (or the psql snippet at the bottom) to verify.
--
-- For local development the role placeholder may be left as-is; the schema
-- creation steps are idempotent. Only the GRANT / REVOKE lines need the real role.
-- =============================================================================

-- ── 1. Audit schema ──────────────────────────────────────────────────────────
CREATE SCHEMA IF NOT EXISTS audit;

-- ── 2. Audit log table ───────────────────────────────────────────────────────
-- Columns match IAuditLogService.LogAsync signature exactly.
-- id and timestamp have DB-side defaults so the application role needs no
-- privilege on sequences or system functions.
CREATE TABLE IF NOT EXISTS audit.audit_log (
    id            uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    actor_id      uuid,                        -- nullable: system/background jobs
    actor_role    text        NOT NULL,
    action_type   text        NOT NULL,
    target_entity text        NOT NULL,
    target_id     uuid,                        -- nullable: non-entity actions
    timestamp     timestamptz NOT NULL DEFAULT now(),
    metadata      text                         -- optional JSON bag for extra context
);

-- ── 3. Lock down PUBLIC access ───────────────────────────────────────────────
-- PostgreSQL grants USAGE on public schema to PUBLIC by default in older
-- versions; revoke here for defence-in-depth.
REVOKE ALL ON SCHEMA audit FROM PUBLIC;
REVOKE ALL ON audit.audit_log FROM PUBLIC;

-- ── 4. Application role — INSERT + SELECT only (AC-003) ─────────────────────
-- Replace <app_role> with your actual application PostgreSQL role name.
GRANT USAGE  ON SCHEMA       audit           TO <app_role>;
GRANT INSERT, SELECT ON audit.audit_log      TO <app_role>;
-- Explicitly deny mutation operations (belt-and-suspenders; REVOKE from PUBLIC
-- above already removes them, but explicit denial is self-documenting).
REVOKE UPDATE, DELETE ON audit.audit_log FROM <app_role>;

-- ── 5. Audit reader role — SELECT only (for compliance/audit queries) ────────
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'audit_reader') THEN
        CREATE ROLE audit_reader;
    END IF;
END
$$;
GRANT USAGE  ON SCHEMA       audit           TO audit_reader;
GRANT SELECT ON audit.audit_log              TO audit_reader;

-- =============================================================================
-- Verification snippet (run after this script to confirm AC-003):
-- =============================================================================
-- -- Should succeed (INSERT):
-- SET ROLE <app_role>;
-- INSERT INTO audit.audit_log (actor_role, action_type, target_entity)
--   VALUES ('test', 'VERIFY', 'audit_schema_check');
--
-- -- Should return error 42501 (permission denied):
-- UPDATE audit.audit_log SET actor_role = 'x' WHERE false;
--
-- -- Cleanup test row:
-- RESET ROLE;
-- DELETE FROM audit.audit_log WHERE action_type = 'VERIFY';
-- =============================================================================
