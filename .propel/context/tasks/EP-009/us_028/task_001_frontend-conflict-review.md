# Task - TASK_001

## Requirement Reference
- **User Story:** us_028
- **Story Location:** .propel/context/tasks/EP-009/us_028/us_028.md
- **Acceptance Criteria:**
  - AC-001: Conflicts section on SCR-013 lists each conflict with severity badge, conflicting values, and source document references
  - AC-002: "Resolve" button → Staff picks authoritative value → conflict status = "Resolved"; canonical value appears in clinical section immediately (optimistic update)
  - AC-003: "Mark Reviewed — Unresolved" button → conflict status = "ReviewedUnresolved"; row remains with label
  - AC-004: No conflicts → Conflicts section not rendered at all (not an empty state)
- **Edge Cases:**
  - HTTP 409 on resolve (already resolved) → inline "Conflict already resolved by another staff member" message; section re-fetches
  - New document uploaded after previous resolution → new conflicts appear with "New" badge; resolved conflicts remain resolved

---

## Design References [CONDITIONAL: UI Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-013-staff-patient-profile.html |
| **Screen Spec** | SCR-013 |
| **UXR Requirements** | UXR-404 |
| **Design Tokens** | `--color-severity-high`, `--color-severity-medium` from variables.css |

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
| Frontend | React (SPA) | 18.x | TR-001 — Conflicts section within SCR-013 |

---

## Task Overview
Implement the `ConflictsSection` component rendered inside `StaffPatientProfilePage` (US_027) only when `conflicts.length > 0`. Each row shows severity badge, conflicting values (with source document reference links), and two action buttons: "Resolve" (opens authoritative-value selector) and "Mark Reviewed". Actions fire `PATCH` requests immediately; the row optimistically transitions to the outcome badge. HTTP 409 triggers an inline error and a refetch. The "New" badge (amber) marks conflicts created after the last staff visit or after the previous resolution.

## Dependent Tasks
- `task_001_frontend-patient-profile.md` (US_027) — `StaffPatientProfilePage` must have a `<ConflictsSection>` slot

## Impacted Components
- `frontend/src/components/profile/ConflictsSection.tsx` — new component; rendered only when conflicts exist
- `frontend/src/components/profile/ConflictRow.tsx` — new row with severity badge + resolve/mark-reviewed actions
- `frontend/src/hooks/useConflicts.ts` — new hook: GET conflicts + PATCH resolve/mark-reviewed

## Implementation Plan
1. Create `useConflicts(patientId)` hook:
   - `GET /api/v1/patients/{patientId}/conflicts` → `DataConflict[]`
   - `resolveConflict(conflictId, authoritativeValue, sourceDocumentId)` → `PATCH /api/v1/conflicts/{id}/resolve`; on 409 → set `conflictError[id] = "Conflict already resolved"` and refetch
   - `markReviewed(conflictId)` → `PATCH /api/v1/conflicts/{id}/mark-reviewed`
2. Create `ConflictRow.tsx`:
   - Severity badge: "High" → `--color-severity-high` (red); "Medium" → `--color-severity-medium` (amber)
   - "New" badge: amber; shown when `conflict.isNew === true`
   - Conflicting values: two panels showing value + source document ref link
   - "Resolve" button: opens inline selector with the two conflicting values + "Other" free-text option; confirm fires `resolveConflict`
   - "Mark Reviewed" button: fires `markReviewed`; row shows "Reviewed — Unresolved" label
   - On HTTP 409 → inline error message under the row; trigger `GET /conflicts` refetch
3. Create `ConflictsSection.tsx`:
   - Renders nothing if `conflicts.length === 0` (AC-004)
   - Section header "Conflicts"; lists `<ConflictRow>` for each conflict
4. Plug `<ConflictsSection>` into `StaffPatientProfilePage` — replace slot added in US_027 task_001
5. Add CSS tokens `--color-severity-high` and `--color-severity-medium` to `variables.css`

## Current Project State
```
frontend/
  src/
    pages/StaffPatientProfilePage.tsx  (ConflictsSection slot from US_027)
    styles/variables.css
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/components/profile/ConflictsSection.tsx | Conditional conflict list; hidden when empty |
| CREATE | frontend/src/components/profile/ConflictRow.tsx | Conflict row with severity + actions |
| CREATE | frontend/src/hooks/useConflicts.ts | GET conflicts + PATCH resolve/mark-reviewed |
| MODIFY | frontend/src/pages/StaffPatientProfilePage.tsx | Plug in ConflictsSection component |
| MODIFY | frontend/src/styles/variables.css | Add severity color tokens |

## External References
- [wireframe-SCR-013-staff-patient-profile.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-013-staff-patient-profile.html)

## Build Commands
- Refer to [frontend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] No conflicts → Conflicts section not rendered in DOM (AC-004)
- [ ] Resolve with authoritative value → row transitions to "Resolved" badge; clinical section updated
- [ ] Concurrent resolve → HTTP 409 → inline "Conflict already resolved" error; conflicts refetch
- [ ] New document uploaded after resolution → new conflict row with "New" badge appears

## Implementation Checklist
- [ ] `ConflictsSection` not rendered when `conflicts.length === 0` (AC-004)
- [ ] Severity badge with design tokens; "New" badge on `isNew` (AC-001, edge case, UXR-404)
- [ ] Resolve action with authoritative value selector; optimistic row update (AC-002)
- [ ] Mark Reviewed action; row shows "ReviewedUnresolved" label (AC-003)
- [ ] HTTP 409 handling: inline error + refetch (edge case)
- [ ] Source document reference links in each conflict row (AC-001)
