# Implementation Analysis -- .propel/context/tasks/EP-009/us_028/task_001_frontend-conflict-review.md

## Verdict

**Status:** Conditional Pass
**Score:** 80 / 100
**Summary:** The ConflictsSection feature is correctly scaffolded and AC-004, the 409 edge case, and the "New" badge edge case are fully implemented. However three acceptance criteria have gaps: AC-002's optimistic update and clinical section refresh are absent (the implementation refetches conflicts only, leaving `ClinicalSections` stale); AC-003's status value is misaligned (`'Reviewed'` vs the spec's `'ReviewedUnresolved'`), which will break the reviewed-state display and `isActioned` guard once the backend is deployed; and the "Mark Reviewed" button is incorrectly restricted to Medium-severity conflicts. The "Other" free-text option required by the implementation plan is also missing. Zero test coverage for all new files is a further gap. These items must be remediated before production sign-off.

---

## Traceability Matrix

| Requirement / Acceptance Criterion | Evidence (file : symbol / line) | Result |
|---|---|---|
| AC-001: Conflicts section lists each conflict with severity badge, conflicting values, and source document references | `ConflictRow.tsx` L93–130: `evidenceBox` + `evidenceSourceRow` per `conflictingValue`; severity badge L98–104; `sourceLabel` text shown | **Partial** — `sourceLabel` rendered as text only; no `<a>` link (missing `sourceDocumentUrl` in DTO) |
| AC-002: "Resolve" → staff picks authoritative value → status = "Resolved"; canonical value appears in clinical section **immediately** (optimistic update) | `useConflicts.ts` L93–120: `resolveConflict()` fires PATCH then `refetch()` (server round-trip); `StaffPatientProfilePage.tsx` L159: no `profileRefetch` callback passed to `ConflictsSection` | **Gap** — not optimistic; `ClinicalSections` profile data never refreshed after resolve |
| AC-003: "Mark Reviewed — Unresolved" → conflict status = "ReviewedUnresolved"; row remains with label | `ConflictRow.tsx` L189: button only rendered when `severity === 'Medium'`; `ConflictStatus` in `conflict.ts` L16 is `'Reviewed'` not `'ReviewedUnresolved'` | **Gap** — button hidden on High; type mismatch with spec status value |
| AC-004: No conflicts → Conflicts section not rendered at all (not an empty state) | `ConflictsSection.tsx` L30: `if (loadState === 'success' && conflicts.length === 0) return null` | **Pass** |
| Edge: HTTP 409 on resolve → inline error + refetch | `useConflicts.ts` L104–108: 409 → `setConflictError(id, …)` + `refetch()`; `ConflictRow.tsx` L166: `role="alert"` renders `inlineError` | **Pass** |
| Edge: New document → new conflicts with "New" badge; resolved remain resolved | `ConflictRow.tsx` L109–113: `{conflict.isNew && <span className={styles.badgeNew}>New</span>}`; resolved state guarded by `isActioned` | **Pass** |
| Impl plan: Resolve form includes "Other" free-text option | `ConflictRow.tsx` L214–232: radio group only from `conflictingValues` — no "Other" entry | **Gap** |
| Design tokens `--color-severity-high` / `--color-severity-medium` in `variables.css` | `variables.css` L113–115: `--color-severity-high: var(--color-conflict-critical)` + `--color-severity-medium: var(--color-conflict-high)` | **Pass** |
| `ConflictsSection` imported and rendered in `StaffPatientProfilePage` | `StaffPatientProfilePage.tsx` L18 import; L159: `{patientId && <ConflictsSection patientId={patientId} />}` | **Pass** |
| TypeScript clean build | `node node_modules/typescript/bin/tsc --noEmit` → 0 errors | **Pass** |

---

## Logical & Design Findings

- **Business Logic:**
  - **FIX-001 (HIGH)** — AC-002 requires the canonical value to appear *in the clinical section* immediately after resolve. `ConflictsSection` calls its own `refetch()` but has no mechanism to trigger `usePatientProfile`'s refetch. `StaffPatientProfilePage` must destructure `refetch as refetchProfile` from `usePatientProfile` and pass it as an `onAfterResolve` prop through `ConflictsSection` → `ConflictRow`. Without this, `ClinicalSections` displays stale pre-resolution data indefinitely.
  - **FIX-002 (HIGH)** — AC-003 specifies status value `'ReviewedUnresolved'`. The type at `conflict.ts` L16 uses `'Reviewed'`. When the backend sends `'ReviewedUnresolved'`, the `isActioned` guard (`ConflictRow.tsx` L44: `status === 'Reviewed'`) evaluates `false`, action buttons re-appear on already-reviewed conflicts, and the `.reviewedMeta` label (`L162: status === 'Reviewed'`) is never rendered.
  - **FIX-003 (MEDIUM)** — `ConflictRow.tsx` L189 gates "Mark Reviewed" to `severity === 'Medium'`. AC-003 imposes no severity restriction. Staff cannot triage High-severity conflicts as "Reviewed — Unresolved" — only Resolve is available for them.
  - **FIX-004 (MEDIUM)** — The Implementation Plan specifies "two conflicting values + **Other** free-text option" in the resolve selector. No "Other" radio entry or conditional text input exists. Staff cannot record a canonical value not present in either source document.

- **Security:**
  - `encodeURIComponent` applied to all user-controlled IDs in fetch URLs (OWASP A03 Injection) ✅
  - Credentials via HttpOnly cookie only — no token in JS memory or `Authorization` header ✅
  - No `dangerouslySetInnerHTML`; all rendered strings are scalar values from typed DTO ✅
  - `resolutionNote` textarea value sent as JSON string — correctly serialised, no injection surface ✅

- **Error Handling:**
  - HTTP 409 on both `resolveConflict` and `markReviewed` handled with per-ID inline error + refetch ✅
  - HTTP 403 handled; `ConflictsSection` returns `null` on forbidden — no information leakage ✅
  - `cancelled` flag prevents stale `setState` on unmount ✅
  - `clearConflictError` invoked before each mutation — stale errors cleared before retry ✅

- **Frontend:**
  - `selectedValue` initialised from `conflictingValues[0]?.value`; "Resolve conflict" button disabled when empty ✅
  - Resolve form stays open after 409 — allows retry without losing selection ✅
  - AI-extracted badge in `ConflictRow.tsx` L117–131 uses inline `style={{…}}` — minor deviation from CSS module convention (FIX-005, LOW)

- **Patterns & Standards:**
  - Hook shape mirrors `usePatientProfile` exactly (idle → loading → success/error/forbidden) ✅
  - `ConflictsSection` uses early returns for each non-success state ✅
  - `ConflictRow` is pure presentational — receives callbacks, owns no side-effects ✅
  - `role="region"` + `aria-label` on section; `role="radiogroup"` + `aria-labelledby` on form; `role="alert"` on errors ✅

---

## Test Review

- **Existing Tests:** `BaselineDemo.test.tsx` only — no tests for any of the 4 new files.
- **Missing Tests (must add):**
  - [ ] Unit: `ConflictsSection` renders `null` when `conflicts = []` (AC-004)
  - [ ] Unit: `ConflictsSection` renders one `ConflictRow` per conflict in list
  - [ ] Unit: `ConflictRow` renders `badgeNew` when `isNew = true`; absent when `false`
  - [ ] Unit: `ConflictRow` renders `.reviewedMeta` label when `status = 'ReviewedUnresolved'` (post FIX-002)
  - [ ] Unit: `ConflictRow` shows "Mark Reviewed" for both High and Medium severity (post FIX-003)
  - [ ] Integration: `useConflicts` — 409 on resolve sets `conflictErrors[id]` and triggers refetch
  - [ ] Integration: `useConflicts` — 403 sets `loadState = 'forbidden'`; no error surfaced to user
  - [ ] Negative: `ConflictsSection` with empty `patientId` does not fire network request

---

## Validation Results

- **Commands Executed:** `node node_modules/typescript/bin/tsc --noEmit` (PowerShell execution policy blocked `npm run build`)
- **Outcomes:** 0 TypeScript errors across all 5 modified/created `.ts`/`.tsx` files ✅
- **Implementation Validation Strategy (from task file):** 4 manual checks listed — all unverified (no automated test run or browser session confirmed)

---

## Fix Plan (Prioritized)

1. **FIX-001** — Add profile refetch after resolve: destructure `refetch as refetchProfile` from `usePatientProfile` in `StaffPatientProfilePage.tsx`; add `onAfterResolve?: () => void` prop to `ConflictsSection` + `ConflictRow`; call after successful PATCH — **`StaffPatientProfilePage.tsx`, `ConflictsSection.tsx`, `ConflictRow.tsx`** — ETA 1 h — Risk: **H**
2. **FIX-002** — Rename `'Reviewed'` → `'ReviewedUnresolved'` in `ConflictStatus` type and all guards — **`conflict.ts` L16, `ConflictRow.tsx` L44 and L162** — ETA 0.25 h — Risk: **H**
3. **FIX-003** — Remove `conflict.severity === 'Medium' &&` guard from "Mark Reviewed" button — **`ConflictRow.tsx` L189** — ETA 0.1 h — Risk: **M**
4. **FIX-004** — Add "Other" radio option + conditional free-text input to resolve form — **`ConflictRow.tsx` L214–232, local state** — ETA 0.5 h — Risk: **M**
5. **FIX-005** — Extract inline AI badge style to `.aiBadge` CSS class — **`ConflictRow.tsx` L117–131, `ConflictRow.module.css`** — ETA 0.15 h — Risk: **L**
6. **Tests** — `ConflictsSection.test.tsx` + `ConflictRow.test.tsx` (Vitest + React Testing Library) covering 8 missing cases above — ETA 2 h — Risk: **M**

---

## Appendix

- **Rules applied:** `react-development-standards.md`, `typescript-styleguide.md`, `web-accessibility-standards.md`, `frontend-development-standards.md`, `ui-ux-design-standards.md`, `security-standards-owasp.md`, `dry-principle-guidelines.md`, `code-anti-patterns.md`
- **Search Evidence:**
  - `grep "severity === 'Medium'"` → `ConflictRow.tsx` L189 (FIX-003 confirmed)
  - `grep "ConflictStatus"` → `conflict.ts` L16: `'Reviewed'` vs spec `'ReviewedUnresolved'` (FIX-002 confirmed)
  - `grep "refetch" StaffPatientProfilePage.tsx` → 0 matches (FIX-001 confirmed)
  - `grep "Other" ConflictRow.tsx` → 0 matches (FIX-004 confirmed)
  - `file_search **/*.test.tsx` → only `BaselineDemo.test.tsx` (test gap confirmed)
