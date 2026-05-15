# Task - TASK_001

## Requirement Reference
- **User Story:** us_009
- **Story Location:** .propel/context/tasks/EP-001/us_009/us_009.md
- **Acceptance Criteria:**
  - AC-003: Specific failing password complexity rule displayed inline beside the password field; form not submitted; focus remains on password field
  - AC-005: Every input has a visible label via `htmlFor`/`id`; every validation error linked via `aria-describedby`; zero axe label violations
- **Edge Cases:**
  - Registration page at 375 px → no horizontal scroll; all inputs and CTA fully visible; touch targets ≥ 44 × 44 px

---

## Design References [CONDITIONAL: UI Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-002-registration.html |
| **Screen Spec** | SCR-002 |
| **UXR Requirements** | UXR-203, UXR-204 |
| **Design Tokens** | `--color-*`, `--spacing-*`, `--font-*` from variables.css |

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
| Frontend | React (SPA) | 18.x | TR-001, NFR-010 — SPA component for registration form |
| Frontend | React Hook Form | Latest stable | NFR-010 — free/open-source; client-side validation with inline error state |
| Frontend | axe-core | Latest stable | UXR-203, UXR-204 — WCAG 2.2 AA automated scan for label/aria violations |

---

## Task Overview
Build the `RegistrationPage` component for SCR-002. The form captures email, password (with real-time complexity feedback), and required profile fields. All inputs are accessible: visible `<label>` elements linked via `htmlFor`/`id`, and inline validation messages linked via `aria-describedby`. Password complexity rules are checked on blur and on submit, with the specific failing rule surfaced inline. The form is fully responsive down to 375 px with ≥ 44 px touch targets. On successful submission the page delegates to the backend task endpoint.

## Dependent Tasks
- `task_001_frontend-scaffold.md` (US_001) — design token system and axe-core baseline must exist

## Impacted Components
- `frontend/src/pages/RegistrationPage.tsx` — new page component
- `frontend/src/components/auth/RegistrationForm.tsx` — new form component
- `frontend/src/utils/passwordValidator.ts` — new password complexity rules utility
- `frontend/src/hooks/useRegistration.ts` — new hook wrapping the registration API call

## Implementation Plan
1. Create `RegistrationPage.tsx` at `/register` route; import design tokens from `variables.css`
2. Build `RegistrationForm.tsx` using React Hook Form; fields: `email` (type=email, required), `password` (type=password, required), `confirmPassword`, `firstName`, `lastName`
3. Implement `passwordValidator.ts`: rules — minLength 8, upperCase, lowerCase, digit; each rule returns a descriptive label (e.g., "Must contain at least one uppercase letter")
4. Wire `aria-describedby` on the password input pointing to an `<ul id="password-errors">` that lists only the currently failing rules; render rules as `<li>` with `role="alert"` on first failure
5. Wire `aria-describedby` on every input to its associated error paragraph `id`; ensure `htmlFor`/`id` pairing on every `<label>`/`<input>`
6. Implement responsive layout: single-column at 375 px; apply `min-height: 44px; min-width: 44px` on all interactive elements
7. Create `useRegistration` hook: calls `POST /api/v1/auth/register`; on 409 → display "Email address already in use"; on 500 → display "Registration failed — please try again"
8. Wire axe-core scan in dev mode; confirm zero label and aria violations on the registration page

## Current Project State
```
frontend/
  src/
    styles/variables.css  (from US_001)
    styles/global.css     (from US_001)
    App.tsx               (from US_001)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/pages/RegistrationPage.tsx | SCR-002 registration page route |
| CREATE | frontend/src/components/auth/RegistrationForm.tsx | Accessible form with React Hook Form |
| CREATE | frontend/src/utils/passwordValidator.ts | Password complexity rule evaluator |
| CREATE | frontend/src/hooks/useRegistration.ts | POST /api/v1/auth/register hook |
| MODIFY | frontend/src/App.tsx | Add `/register` route |

## External References
- [React Hook Form Docs](https://react-hook-form.com/)
- [WCAG 2.2 SC 1.3.1 Info and Relationships](https://www.w3.org/WAI/WCAG22/Understanding/info-and-relationships.html)
- [WCAG 2.2 SC 4.1.3 Status Messages](https://www.w3.org/WAI/WCAG22/Understanding/status-messages.html)
- [axe-core Rules Reference](https://dequeuniversity.com/rules/axe/4.9)
- [wireframe-SCR-002-registration.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-002-registration.html)

## Build Commands
- Refer to [frontend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] axe-core dev scan on `/register` reports zero label, aria-describedby, or colour-contrast violations
- [ ] Tab navigation reaches all inputs and the submit CTA in DOM order
- [ ] At 375 px viewport: no horizontal scroll; all touch targets ≥ 44 px
- [ ] Entering a short password shows inline rule list; correcting each rule removes it from the list

## Implementation Checklist
- [ ] Create `RegistrationPage.tsx` at `/register`; wire to `App.tsx` router (AC-005)
- [ ] Build `RegistrationForm.tsx` with React Hook Form; all inputs have `<label htmlFor>` + matching `id`; required fields marked with `aria-required="true"` (AC-005)
- [ ] Implement `passwordValidator.ts` with 4 named rules; return failing rule labels on each evaluation (AC-003)
- [ ] Wire failing rules to `aria-describedby` error list; show specific failing rule inline beside password field; focus remains on password field on failed submit (AC-003)
- [ ] Wire all other validation errors to `aria-describedby` paragraph per input (AC-005)
- [ ] Apply responsive single-column layout at 375 px; touch targets ≥ 44 × 44 px (edge case)
- [ ] Implement `useRegistration` hook; handle 409 ("Email already in use") and 500 ("Registration failed") responses (feeds AC-002, AC-004 — verified in task_002)
- [ ] Run axe-core scan in dev mode; confirm zero violations on the registration page (AC-005)
