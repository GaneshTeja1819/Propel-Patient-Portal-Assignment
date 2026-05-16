#!/usr/bin/env node
/**
 * token-audit.js — Design Token Compliance Check (AC-002, UXR-401)
 *
 * Cross-platform (Node.js) replacement for token-audit.sh.
 * Scans src/ for raw hex colour values and raw px dimension values
 * appearing outside of src/styles/variables.css.
 *
 * Exit codes:
 *   0 — no violations (audit passed)
 *   1 — one or more violations found (CI must block)
 *
 * Rules:
 *   1. Raw hex values (#RGB, #RRGGBB, #RRGGBBAA) are forbidden in all
 *      .css / .tsx / .ts files except variables.css.
 *   2. Raw px values in CSS property declarations are forbidden in all
 *      CSS files except variables.css.
 *      @media conditions are exempt because CSS custom properties
 *      cannot be used inside @media expressions (CSS spec limitation).
 */

import { readFileSync, readdirSync, statSync } from 'fs';
import { join, relative } from 'path';
import { fileURLToPath } from 'url';
import { dirname } from 'path';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const SRC_DIR = join(__dirname, '..', 'src');
const VARIABLES_CSS = join(SRC_DIR, 'styles', 'variables.css');

const RAW_HEX_PATTERN = /#[0-9A-Fa-f]{3,8}\b/;
const RAW_PX_PATTERN = /[0-9]+px/;

let violations = 0;

/** Recursively collect files matching extensions */
function collectFiles(dir, extensions) {
  const results = [];
  for (const entry of readdirSync(dir)) {
    const full = join(dir, entry);
    const stat = statSync(full);
    if (stat.isDirectory()) {
      results.push(...collectFiles(full, extensions));
    } else if (extensions.some((ext) => full.endsWith(ext))) {
      results.push(full);
    }
  }
  return results;
}

console.log('============================================================');
console.log(' Design Token Audit');
console.log(` Scanning: ${relative(process.cwd(), SRC_DIR)}`);
console.log(` Exempted: ${relative(process.cwd(), VARIABLES_CSS)}`);
console.log('============================================================');

// ── Rule 1: Raw hex values in .css, .tsx, .ts (excluding variables.css) ──
console.log('\n[Rule 1] Checking for raw hex colour values...');

const hexTargets = collectFiles(SRC_DIR, ['.css', '.tsx', '.ts']).filter(
  (f) => f !== VARIABLES_CSS
);

const hexHits = [];
for (const file of hexTargets) {
  const lines = readFileSync(file, 'utf8').split('\n');
  lines.forEach((line, idx) => {
    const trimmed = line.trim();
    if (trimmed.startsWith('//') || trimmed.startsWith('*') || trimmed.startsWith('/*')) return;
    if (RAW_HEX_PATTERN.test(line)) {
      hexHits.push(`  ${relative(process.cwd(), file)}:${idx + 1}  ${line.trim()}`);
    }
  });
}

if (hexHits.length > 0) {
  console.error('\nERROR: Raw hex colour values found outside variables.css:');
  hexHits.forEach((h) => console.error(h));
  violations++;
} else {
  console.log('PASS: No raw hex values found outside variables.css.');
}

// ── Rule 2: Raw px values in CSS declarations (excluding variables.css, @media) ──
console.log('\n[Rule 2] Checking for raw px values in CSS property declarations...');

const cssTargets = collectFiles(SRC_DIR, ['.css']).filter((f) => f !== VARIABLES_CSS);

const pxHits = [];
for (const file of cssTargets) {
  const lines = readFileSync(file, 'utf8').split('\n');
  lines.forEach((line, idx) => {
    const trimmed = line.trim();
    if (trimmed.startsWith('//') || trimmed.startsWith('*') || trimmed.startsWith('/*')) return;
    if (trimmed.startsWith('@media')) return; // exempt: CSS spec prevents custom properties in @media
    if (RAW_PX_PATTERN.test(line)) {
      pxHits.push(`  ${relative(process.cwd(), file)}:${idx + 1}  ${line.trim()}`);
    }
  });
}

if (pxHits.length > 0) {
  console.error('\nERROR: Raw px values found in CSS declarations outside variables.css:');
  pxHits.forEach((h) => console.error(h));
  console.error('\nFix: Replace raw px values with tokens (e.g. var(--space-4)).');
  violations++;
} else {
  console.log('PASS: No raw px values found in CSS declarations outside variables.css.');
}

// ── Result ──────────────────────────────────────────────────────────────────
console.log('\n============================================================');
if (violations > 0) {
  console.error(` AUDIT FAILED — ${violations} rule violation(s) detected.`);
  console.error(' All colours and dimensions must use tokens from variables.css.');
  console.log('============================================================');
  process.exit(1);
}

console.log(' AUDIT PASSED — Zero token violations.');
console.log('============================================================');
process.exit(0);
