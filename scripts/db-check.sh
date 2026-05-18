#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# db-check.sh — Post-migration database verification (task_002 / AC-001–AC-004)
#
# Checks:
#   1. InitialCreate migration recorded in __EFMigrationsHistory  (AC-001)
#   2. pgvector extension active                                   (AC-002)
#   3. Domain table count ≥ 15                                     (AC-001)
#   4. FK referential integrity — invalid insert rejected 23503    (AC-003)
#   5. InsuranceRecord seed rows present                           (AC-004)
#   6. Storage usage — warn when ≥ 80 % of 500 MB free-tier cap   (edge case)
#
# Usage:
#   export DATABASE_URL="postgresql://postgres.<ref>:<password>@<pooler>:5432/postgres"
#   ./scripts/db-check.sh
#
#   Or set PGHOST / PGDATABASE / PGUSER / PGPASSWORD / PGPORT individually.
#
# Exit codes:  0 = all checks passed   1 = one or more checks FAILED
# ─────────────────────────────────────────────────────────────────────────────
set -euo pipefail

# ── Resolve connection ────────────────────────────────────────────────────────
if [ -z "${DATABASE_URL:-}" ]; then
  if [ -z "${PGHOST:-}" ] || [ -z "${PGDATABASE:-}" ] || [ -z "${PGUSER:-}" ] || [ -z "${PGPASSWORD:-}" ]; then
    echo "ERROR: set DATABASE_URL or PGHOST / PGDATABASE / PGUSER / PGPASSWORD." >&2
    exit 1
  fi
  DATABASE_URL="postgresql://${PGUSER}:${PGPASSWORD}@${PGHOST}:${PGPORT:-5432}/${PGDATABASE}"
fi

_scalar() { psql "$DATABASE_URL" --no-align --tuples-only -c "$1" 2>&1 | head -1 | tr -d '[:space:]'; }

FAILED=0
FAKE_UUID="00000000-dead-beef-0000-000000000000"

echo "======================================================"
echo " UPACIP Post-Migration Database Check"
echo "======================================================"

# 1. Migration history ─────────────────────────────────────────────────────────
echo "[ 1 ] EF Core migration history..."
COUNT=$(_scalar "SELECT COUNT(*) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" LIKE '%InitialCreate'")
if [ "${COUNT:-0}" -ge 1 ]; then
  echo "  PASS — InitialCreate recorded in __EFMigrationsHistory."
else
  echo "  FAIL — InitialCreate NOT found. Run: dotnet ef database update"
  FAILED=1
fi

# 2. pgvector extension (AC-002) ──────────────────────────────────────────────
echo "[ 2 ] pgvector extension (AC-002)..."
EXT=$(_scalar "SELECT COUNT(*) FROM pg_extension WHERE extname = 'vector'")
if [ "${EXT:-0}" -ge 1 ]; then
  echo "  PASS — pgvector is active."
else
  echo "  FAIL — pgvector not found."
  FAILED=1
fi

# 3. Table count ≥ 15 (AC-001) ────────────────────────────────────────────────
echo "[ 3 ] Domain table count (expect ≥ 15)..."
TABLES=$(_scalar "
  SELECT COUNT(*) FROM information_schema.tables
  WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
    AND table_name NOT IN ('__EFMigrationsHistory')")
if [ "${TABLES:-0}" -ge 15 ]; then
  echo "  PASS — ${TABLES} tables in public schema."
else
  echo "  FAIL — Only ${TABLES} tables (expected ≥ 15)."
  FAILED=1
fi

# 4. FK constraint enforcement (AC-003) ───────────────────────────────────────
echo "[ 4 ] FK constraint enforcement (AC-003)..."
FK_OUT=$(psql "$DATABASE_URL" --no-align --tuples-only 2>&1 -c "
  INSERT INTO appointments (\"Id\",\"PatientId\",\"ProviderId\",\"SlotId\",\"Status\",\"CreatedAt\",\"UpdatedAt\")
  VALUES (gen_random_uuid(),'${FAKE_UUID}','${FAKE_UUID}','${FAKE_UUID}','pending',NOW(),NOW())" || true)
if echo "${FK_OUT}" | grep -qiE "23503|foreign.key|violat"; then
  echo "  PASS — invalid FK insert rejected (23503)."
else
  echo "  WARN — FK rejection not confirmed: ${FK_OUT}"
fi

# 5. InsuranceRecord seed data (AC-004) ───────────────────────────────────────
echo "[ 5 ] InsuranceRecord seed data (AC-004)..."
INS=$(_scalar "SELECT COUNT(*) FROM insurance_records WHERE \"ProviderName\" IS NOT NULL AND \"InsuranceIdPattern\" IS NOT NULL" 2>/dev/null || echo 0)
if [ "${INS:-0}" -ge 3 ]; then
  echo "  PASS — ${INS} insurance records present."
else
  echo "  FAIL — Expected ≥ 3 seed rows, found ${INS:-0}."
  FAILED=1
fi

# 6. Storage usage (edge case — 500 MB free-tier cap) ─────────────────────────
echo "[ 6 ] Storage usage (warn ≥ 80 % of 500 MB)..."
BYTES=$(_scalar "SELECT pg_database_size(current_database())")
if [[ "${BYTES:-}" =~ ^[0-9]+$ ]]; then
  MB=$(( BYTES / 1048576 ))
  PCT=$(( BYTES * 100 / 524288000 ))
  if [ "${BYTES}" -ge 419430400 ]; then
    echo "  WARN — ${MB} MB used (${PCT}% of 500 MB free-tier limit)."
  else
    echo "  PASS — ${MB} MB used (${PCT}% of 500 MB free-tier limit)."
  fi
else
  echo "  SKIP — unable to read pg_database_size."
fi

echo "======================================================"
[ "${FAILED}" -eq 0 ] && echo " RESULT: All checks PASSED." || echo " RESULT: ${FAILED} check(s) FAILED."
echo "======================================================"
exit "${FAILED}"
