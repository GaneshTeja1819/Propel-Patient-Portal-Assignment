# Task - TASK_001

## Requirement Reference
- **User Story:** us_030
- **Story Location:** .propel/context/tasks/EP-010/us_030/us_030.md
- **Acceptance Criteria:**
  - AC-001: SCR-014 renders all MedicalCodeSuggestion rows in rank order; confidenceScore < 0.5 → amber "Low confidence" badge
  - AC-002: "Accept" → VerifiedMedicalCode with decision="Accepted"; row marked "Accepted"; irreversible
  - AC-003: "Modify" → inline edit opens; Staff enters custom code; codeset validation on blur/submit; if valid → VerifiedMedicalCode with decision="Modified"; if invalid → inline error; no record
  - AC-004: "Reject" → VerifiedMedicalCode with decision="Rejected"; row marked "Rejected"
  - AC-005: All rejected → banner "All suggestions rejected — manual coding required"; encounter flagged; no billing
- **Edge Cases:**
  - Post-billing modification → actions disabled; tooltip "This encounter has been finalised"
  - Patient navigates to /staff/coding/:encounterId → HTTP 403 → redirect to patient dashboard
  - Code suggestion failure (US_029) → table empty; "No codes suggested — manual coding required" + "Regenerate" CTA

---

## Design References [CONDITIONAL: UI Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-014-code-verification.html |
| **Screen Spec** | SCR-014 |
| **UXR Requirements** | UXR-107 |
| **Design Tokens** | `--color-warning`, `--color-success`, `--color-error` from variables.css |

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
| Frontend | React (SPA) | 18.x | TR-001 — SCR-014 code verification table |

---

## Task Overview
Build `CodeVerificationPage` at `/staff/coding/:encounterId` (SCR-014). The page fetches all `MedicalCodeSuggestion` records for the encounter ordered by rank. Each row shows codeType badge, suggestedCode, rank, confidenceScore (< 0.5 → amber "Low confidence" badge per UXR-107), and three action buttons: Accept, Modify, Reject. Each action fires `POST /api/v1/codes/verify` immediately (no batch Submit button). On success the row transitions to the corresponding outcome badge. The "Modify" action opens an inline edit field with real-time codeset validation (validated via `GET /api/v1/codes/validate?code=X&type=ICD10`). When all rows are actioned as "Rejected", the all-rejected banner renders automatically. If `encounter.status === "Finalized"` all actions are disabled with a tooltip.

## Dependent Tasks
- `task_001_ai-code-suggestion.md` (US_029) — `MedicalCodeSuggestion` records must exist; `GET /api/v1/code-suggestions?encounterId=` endpoint needed

## Impacted Components
- `frontend/src/pages/CodeVerificationPage.tsx` — new SCR-014 page
- `frontend/src/components/coding/CodeVerificationTable.tsx` — new table with suggestion rows
- `frontend/src/components/coding/CodeVerificationRow.tsx` — new row with per-row actions + modify inline edit
- `frontend/src/hooks/useCodeVerification.ts` — new hook: GET suggestions + POST verify

## Implementation Plan
1. Create `useCodeVerification(encounterId)` hook:
   - `GET /api/v1/code-suggestions?encounterId={id}` → `MedicalCodeSuggestionDto[]` sorted by rank
   - `verify(suggestionId, decision, verifiedCode?)` → `POST /api/v1/codes/verify`; on HTTP 403 → navigate to `/`; on HTTP 422 → set inline validation error for row
   - `validateCode(code, codeType)` → `GET /api/v1/codes/validate?code=X&type=ICD10` → `{ valid: boolean, description?: string }`
   - Tracks `verifiedRows: Record<string, 'Accepted' | 'Modified' | 'Rejected'>` in state; triggers all-rejected banner when all rows in set
2. Create `CodeVerificationRow.tsx`:
   - Renders codeType badge, code, rank
   - `confidenceScore < 0.5` → amber "Low confidence" badge (`--color-warning`) per UXR-107
   - If `verifiedRows[id]` is set → shows outcome badge (Accepted/Modified/Rejected); action buttons hidden
   - If encounter is "Finalized" → all buttons disabled + tooltip "This encounter has been finalised"
   - "Accept" button → `verify(id, 'Accepted')` immediately
   - "Modify" button → opens inline `<input>` field; on blur call `validateCode`; if invalid → inline error "Code not found in codeset"; if valid → "Confirm" button fires `verify(id, 'Modified', enteredCode)`
   - "Reject" button → `verify(id, 'Rejected')` immediately
3. Create `CodeVerificationTable.tsx`:
   - Renders rows in rank order
   - If `suggestions.length === 0` → show "No codes suggested — manual coding required" + "Regenerate" CTA button (fires `POST /api/v1/code-suggestions/generate`)
   - If all rows rejected → render amber `--color-warning` banner "All suggestions rejected — manual coding required"
4. Create `CodeVerificationPage.tsx` at `/staff/coding/:encounterId`:
   - On mount: if HTTP 403 → navigate to `/`
   - Renders `<CodeVerificationTable />`
5. Add route `/staff/coding/:encounterId` to `App.tsx`

## Current Project State
```
frontend/
  src/
    context/AuthContext.tsx
    styles/variables.css
    App.tsx
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/pages/CodeVerificationPage.tsx | SCR-014 code verification page |
| CREATE | frontend/src/components/coding/CodeVerificationTable.tsx | Suggestion table with empty/all-rejected states |
| CREATE | frontend/src/components/coding/CodeVerificationRow.tsx | Per-row actions with inline modify edit + confidence badge |
| CREATE | frontend/src/hooks/useCodeVerification.ts | GET suggestions + POST verify + GET validate |
| MODIFY | frontend/src/App.tsx | Add /staff/coding/:encounterId route |

## External References
- [wireframe-SCR-014-code-verification.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-014-code-verification.html)
- [WCAG 2.2 SC 3.3.3 Error Suggestion](https://www.w3.org/WAI/WCAG22/Understanding/error-suggestion.html)

## Build Commands
- Refer to [frontend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Page loads with suggestions sorted by rank; confidenceScore < 0.5 row shows amber badge
- [ ] Accept → row shows "Accepted" badge; no further actions possible on that row
- [ ] Modify with invalid code → inline error; no verify call made; row remains actioned
- [ ] Reject all rows → all-rejected banner auto-renders
- [ ] encounter.status = "Finalized" → all buttons disabled; tooltip shown
- [ ] HTTP 403 → redirect to /

## Implementation Checklist
- [ ] Suggestions sorted by rank; "Low confidence" amber badge for < 0.5 (AC-001, UXR-107)
- [ ] Accept / Reject fire immediately; row transitions to badge; irreversible (AC-002, AC-004)
- [ ] Modify opens inline edit; `validateCode` call on blur; inline error if invalid; confirm fires verify (AC-003)
- [ ] All-rejected banner renders when all rows = "Rejected" (AC-005)
- [ ] Finalized guard: actions disabled with tooltip (edge case)
- [ ] HTTP 403 → navigate to `/` (edge case, OWASP A01)
- [ ] Empty suggestions state: "No codes suggested" + Regenerate CTA (edge case — US_029 failure)
