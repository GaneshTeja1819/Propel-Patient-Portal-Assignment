#!/usr/bin/env bash
#
# token-audit.sh — Design Token Compliance Check (AC-002, UXR-401)
#
# Scans src/ for raw hex colour values and raw px dimension values
# appearing outside of variables.css (the single source of truth).
#
# Exit codes:
#   0 — no violations (audit passed)
#   1 — one or more violations found (audit failed; CI must block)
#
# Rules:
#   1. Raw hex values (#RRGGBB, #RGB, #RRGGBBAA) are forbidden in all
#      files except src/styles/variables.css.
#   2. Raw px values in CSS property declarations are forbidden in all
#      CSS files except src/styles/variables.css.
#      @media query conditions are excluded from rule 2 because CSS
#      custom properties cannot be used inside @media expressions.

set -euo pipefail

SRC_DIR="${1:-src}"
VIOLATIONS=0
VARIABLES_CSS="$SRC_DIR/styles/variables.css"

echo "============================================================"
echo " Design Token Audit"
echo " Scanning: $SRC_DIR"
echo " Exempted: $VARIABLES_CSS"
echo "============================================================"

# ──────────────────────────────────────────────────────────────────
# Rule 1: Raw hex colour values in .css, .tsx, .ts files
#         (excluding variables.css)
# ──────────────────────────────────────────────────────────────────
echo ""
echo "[Rule 1] Checking for raw hex colour values..."

HEX_HITS=$(
  grep -rn \
    --include="*.css" \
    --include="*.tsx" \
    --include="*.ts" \
    -E '#[0-9A-Fa-f]{3,8}\b' \
    "$SRC_DIR" \
    2>/dev/null \
  | grep -v "variables\.css" \
  | grep -v "^[[:space:]]*\*" \
  | grep -v "^[[:space:]]*//" \
  || true
)

if [ -n "$HEX_HITS" ]; then
  echo ""
  echo "ERROR: Raw hex colour values found outside variables.css:"
  echo "$HEX_HITS"
  VIOLATIONS=$((VIOLATIONS + 1))
else
  echo "PASS: No raw hex values found outside variables.css."
fi

# ──────────────────────────────────────────────────────────────────
# Rule 2: Raw px values in CSS property declarations
#         (excluding variables.css and @media query conditions)
# ──────────────────────────────────────────────────────────────────
echo ""
echo "[Rule 2] Checking for raw px values in CSS property declarations..."

PX_HITS=$(
  grep -rn \
    --include="*.css" \
    -E '[0-9]+px' \
    "$SRC_DIR" \
    2>/dev/null \
  | grep -v "variables\.css" \
  | grep -v "@media" \
  | grep -v "^[[:space:]]*\*" \
  | grep -v "^[[:space:]]*//" \
  || true
)

if [ -n "$PX_HITS" ]; then
  echo ""
  echo "ERROR: Raw px values found in CSS property declarations outside variables.css:"
  echo "$PX_HITS"
  echo ""
  echo "Fix: Replace raw px values with token variables (e.g. var(--space-4))."
  VIOLATIONS=$((VIOLATIONS + 1))
else
  echo "PASS: No raw px values found in CSS declarations outside variables.css."
fi

# ──────────────────────────────────────────────────────────────────
# Result
# ──────────────────────────────────────────────────────────────────
echo ""
echo "============================================================"
if [ "$VIOLATIONS" -gt 0 ]; then
  echo " AUDIT FAILED — $VIOLATIONS rule violation(s) detected."
  echo " All colours and dimensions must use tokens from variables.css."
  echo "============================================================"
  exit 1
fi

echo " AUDIT PASSED — Zero token violations."
echo "============================================================"
exit 0
