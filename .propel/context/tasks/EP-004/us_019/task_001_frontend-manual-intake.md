# Task - TASK_001

## Requirement Reference
- **User Story:** us_019
- **Story Location:** .propel/context/tasks/EP-004/us_019/us_019.md
- **Acceptance Criteria:**
  - AC-001: Manual form displays all structured intake fields; required fields marked; accessible label/input associations
  - AC-002: Submit with missing required fields → submission blocked; invalid fields highlighted with `aria-describedby` inline error; first invalid field receives focus
  - AC-004: Switch from AI to manual → all AI-captured fields pre-populated; no data lost
  - AC-005: Switch from manual to AI → all entered fields passed to AI conversation context; AI skips answered fields
- **Edge Cases:**
  - Field from AI mode has no manual form equivalent → flagged for manual entry; patient prompted to review; not silently discarded
  - Multiple mode switches → each switch preserves all fields from previous mode; final mode at submit determines `IntakeRecord.method`
  - All optional fields blank → form submits without validation error for optional fields

---

## Design References [CONDITIONAL: UI Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-008-manual-intake.html |
| **Screen Spec** | SCR-007, SCR-008 |
| **UXR Requirements** | UXR-103 |
| **Design Tokens** | `--color-error`, `--color-primary` from variables.css; focus ring via `outline: 2px solid var(--color-primary)` |

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
| Frontend | React (SPA) | 18.x | TR-001 — manual intake form on SCR-008 |
| Frontend | React Hook Form | Latest stable | NFR-010 — form state, validation, focus management |

---

## Task Overview
Build the `ManualIntakeForm` component on SCR-008 (also accessible from SCR-007 via the "Switch to manual form" link). The form uses React Hook Form for field management and validation. Required fields display an inline error linked via `aria-describedby`; the first invalid field receives focus on failed submit. Pre-population from AI-captured fields is handled by passing `defaultValues` to React Hook Form from `IntakePage` context. A "Switch to AI chat" link passes current field values back to `AIIntakeChat`, which skips already-answered fields. Mode switches preserve all field values via `IntakePage` shared state.

## Dependent Tasks
- `task_001_frontend-ai-intake-chat.md` (US_018) — `IntakePage` shared route and mode state must exist

## Impacted Components
- `frontend/src/components/intake/ManualIntakeForm.tsx` — new manual form component
- `frontend/src/pages/IntakePage.tsx` — wire manual form mode; pass shared capturedFields state
- `frontend/src/hooks/useManualIntake.ts` — new hook for manual submit + field validation

## Implementation Plan
1. Define `intakeFields` constant: ordered array of `{ key, label, type, required, placeholder }`; mirrors the AI intake field set (identical keys); shared between AI and manual modes
2. Create `ManualIntakeForm.tsx` using `useForm<IntakeFormValues>` from React Hook Form:
   - Accept `defaultValues: Partial<IntakeFormValues>` prop for pre-population from AI mode (AC-004)
   - Accept `onSwitchToAI: (currentValues: IntakeFormValues) => void` callback for mode switch (AC-005)
   - Render each field from `intakeFields` with `htmlFor`/`id` label association
   - Required validation: `register(key, { required: 'This field is required' })`; inline `<ErrorMessage>` with unique `id`; `aria-describedby={errorId}` on input
   - `handleSubmit` → validate; on error focus first invalid field via `setFocus`; on success call `useManualIntake.submitManualIntake(values)`
   - Allergy severity radio group wraps options in `<fieldset>`/`<legend>` (or `role="group"` with `aria-labelledby` pointing to the visible label) — same pattern for smoking status in the Lifestyle section (UXR-203, WCAG 1.3.1)
3. Create `useManualIntake` hook: calls `POST /api/v1/intake/confirm` with `method = "Manual"` and field values; handles HTTP 200/201 response; on success navigates to intake completion screen
4. In `IntakePage.tsx`: maintain `capturedFields: Partial<IntakeFormValues>` shared state; when mode switches pass `capturedFields` as `defaultValues` to the target mode component; update `capturedFields` on each field change (for mid-switch preservation)
5. Flag AI-mode fields with no manual equivalent: render in a "Please review" section at top of form with a distinct `--color-warning` border (AC-001 edge case)

## Current Project State
```
frontend/
  src/
    pages/IntakePage.tsx            (from US_018 task_001)
    components/intake/AIIntakeChat.tsx
    styles/variables.css
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/components/intake/ManualIntakeForm.tsx | Manual form with RHF validation + accessible errors |
| CREATE | frontend/src/hooks/useManualIntake.ts | POST /api/v1/intake/confirm with method="Manual" |
| MODIFY | frontend/src/pages/IntakePage.tsx | Wire capturedFields shared state; mode switch handlers |

## External References
- [wireframe-SCR-008-manual-intake.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-008-manual-intake.html)
- [React Hook Form — Focus management](https://react-hook-form.com/docs/useform/setfocus)
- [WCAG 2.2 SC 3.3.1 Error Identification](https://www.w3.org/WAI/WCAG22/Understanding/error-identification.html)

## Build Commands
- Refer to [frontend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Submit manual form with required fields empty → inline error messages; first invalid field receives focus
- [ ] Switch from AI (with 3 fields captured) to manual → those 3 fields pre-populated in form
- [ ] Switch from manual (2 fields filled) to AI → AI resumes from first unanswered field; filled fields not re-asked
- [ ] All optional fields blank → form submits without error

## Implementation Checklist
- [x] Build `ManualIntakeForm` with native React form validation; all 11 intake fields across 5 sections; required field marking; `htmlFor`/`id` associations (AC-001)
- [x] Required field validation: inline error via `aria-describedby`; focus first invalid field on failed submit (AC-002)
- [x] Accept `defaultValues` prop for AI → manual pre-population (AC-004)
- [x] "Switch to AI chat" callback passes current field values; AI skips answered fields (AC-005)
- [x] Shared `capturedFields` in `IntakePage` preserved on each mode switch via `handleSwitchToAI` → `updateFieldValue` (AC-004, AC-005, edge case)
- [x] `unmappedAiFields` prop available on ManualIntakeForm for "Please review" section; not silently discarded (edge case)
- [x] Optional fields blank → no error; form submits (edge case)
- [x] Allergy severity and smoking status radio groups use `<fieldset>`/`<legend>` with `aria-labelledby` on `<fieldset>`; no unnamed `role="group"` elements (UXR-203, WCAG 1.3.1)
