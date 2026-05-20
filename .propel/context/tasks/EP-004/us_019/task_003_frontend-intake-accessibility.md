# Task - TASK_003

## Requirement Reference
- **User Story:** us_019
- **Story Location:** .propel/context/tasks/EP-004/us_019/us_019.md
- **Acceptance Criteria:**
  - AC-001 (indirect): Accessible form page shell — skip link, live region, and correct heading must exist before ManualIntakeForm renders (UXR-203, WCAG 2.4.1, WCAG 4.1.3)
  - UXR-203: All form fields and page navigation must be accessible with correct ARIA attributes; axe-core reports zero label/contrast violations on SCR-008
- **Edge Cases:**
  - Patient switches modes multiple times → live region must fire on each switch; title/heading must update each time
  - User with high-contrast mode → `--color-success-text` token must satisfy WCAG AA 4.5:1 against white in all colour modes

---

## Design References [CONDITIONAL: UI Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-008-manual-intake.html |
| **Screen Spec** | SCR-008 |
| **UXR Requirements** | UXR-203 |
| **Design Tokens** | `--color-success-text` (new alias → `--color-green-700` / `#15803D`); `--color-success` must not be used for text on white |

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
| Frontend | React (SPA) | 18.x | TR-001 — IntakePage.tsx modifications |
| Frontend | TypeScript | 5.x | TR-001 — typed component props |
| Library | CSS Custom Properties | Native | Design system token extension — `--color-success-text` |

---

## Task Overview

⚠️ **High-Risk File:** `frontend/src/pages/IntakePage.tsx` — 3 findings in registry (F015, F016, F017). Apply all changes in a single focused pass.

Resolve all WCAG and DRY violations identified in the `analyze-ux SCR-008` review and `review-code b89fd5d` code review that are scoped to `IntakePage.tsx` and `variables.css`. The `ManualIntakeForm.tsx` component (task_001) is the primary deliverable of US_019; this task removes the accessibility blockers in the shared page shell that would fail WCAG AA compliance even after task_001 ships.

Changes are confined to four files:
- `IntakePage.tsx` — skip link, ARIA live region for mode switch, dynamic `document.title`, dynamic `<h1>`, and `srOnly` → `className="sr-only"` replacement
- `variables.css` — add `--color-success-text` alias token at `#15803D` (contrast ratio 4.75:1 on white)

## Dependent Tasks
- None — this task can be merged ahead of or in parallel with task_001

## Impacted Components
- `frontend/src/pages/IntakePage.tsx` — skip link, live region, title/h1 fix, srOnly cleanup
- `frontend/src/styles/variables.css` — new `--color-success-text` semantic token

## Implementation Plan

1. **Skip link** — At the top of the JSX returned by `IntakePage`, before the `<nav>`, insert a visually hidden skip link:
   ```tsx
   <a href="#main-intake" className="sr-only skip-link">
     Skip to main content
   </a>
   ```
   Add `id="main-intake"` to the `<main>` element (both in the confirmed-state branch and the default branch). The skip link becomes visible on focus via existing `.sr-only` styles; add a `:focus` override if not already present in `global.css`.

2. **ARIA live region** — Add a `role="status"` element just below the skip link. Update its text via a `modeAnnouncement` state variable whenever `mode` changes:
   ```tsx
   const [modeAnnouncement, setModeAnnouncement] = useState('');
   // In handleModeSwitch, after setMode(next):
   setModeAnnouncement(
     next === 'manual' ? 'Switched to manual intake form' : 'Switched to AI-assisted intake'
   );
   ```
   Render: `<div role="status" aria-live="polite" className="sr-only">{modeAnnouncement}</div>`. The region must be in the DOM on first render (not conditionally mounted) for live-region behaviour to work correctly.

3. **Dynamic `document.title`** — Extend the existing `useEffect` to depend on `[confirmed, mode]`:
   ```tsx
   useEffect(() => {
     if (confirmed) document.title = 'Intake Submitted | Patient Portal';
     else if (mode === 'manual') document.title = 'Manual Intake | Patient Portal';
     else document.title = 'AI Intake | Patient Portal';
     return () => { document.title = 'Patient Portal'; };
   }, [confirmed, mode]);
   ```
   This resolves F016 and WCAG 2.4.2.

4. **Dynamic `<h1>` text** — Replace the static `srOnly` h1 string with a mode-conditional expression:
   ```tsx
   <h1 className="sr-only">
     {mode === 'manual'
       ? 'Manual intake form — complete your pre-visit questionnaire'
       : 'AI Conversational Intake — complete your pre-visit questionnaire'}
   </h1>
   ```
   This resolves F017 and WCAG 1.3.1.

5. **`--color-success-text` design token** — Add to `variables.css` in the Semantic Status block:
   ```css
   --color-success-text: var(--color-green-700); /* #15803D — 4.75:1 on white; use for text only */
   ```
   Add a code comment that `--color-success` (`#16A34A`) must not be used for text on white backgrounds (3.30:1 fails WCAG AA). Apply `--color-success-text` to `ManualIntakeForm.tsx` section-header status badges (`✓ Complete`) when that component is built in task_001.

6. **Replace `srOnly` inline style with `className="sr-only"`** — Delete the `const srOnly: React.CSSProperties = { ... }` object at IntakePage.tsx lines 26–36. Replace `style={srOnly}` with `className="sr-only"` at all two usage sites (headings in confirmed-state and default-state branches). This resolves F015.

## Current Project State
```
frontend/
  src/
    pages/IntakePage.tsx          (TASK_001 output — mode switch, AI chat, placeholder)
    styles/
      global.css                  (.sr-only class added in commit b89fd5d)
      variables.css               (--color-success: var(--color-green-600))
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | frontend/src/pages/IntakePage.tsx | Add skip link; add mode-switch live region; fix `document.title` + `<h1>` conditional; replace `srOnly` const with `className="sr-only"` |
| MODIFY | frontend/src/styles/variables.css | Add `--color-success-text: var(--color-green-700)` with contrast-safety comment |

## External References
- [WCAG 2.2 SC 2.4.1 Bypass Blocks](https://www.w3.org/WAI/WCAG22/Understanding/bypass-blocks.html)
- [WCAG 2.2 SC 2.4.2 Page Titled](https://www.w3.org/WAI/WCAG22/Understanding/page-titled.html)
- [WCAG 2.2 SC 4.1.3 Status Messages](https://www.w3.org/WAI/WCAG22/Understanding/status-messages.html)
- [WCAG 2.2 SC 1.3.1 Info and Relationships](https://www.w3.org/WAI/WCAG22/Understanding/info-and-relationships.html)
- [MDN — aria-live regions](https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/ARIA_Live_Regions)

## Build Commands
- Refer to [frontend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] axe-core scan on `/intake/:id` in manual mode → zero `color-contrast` violations; zero `bypass` violations (skip link present); zero unnamed live region violations
- [ ] Tab key from page load → first focus stop is skip link; activating it moves focus to `<main id="main-intake">`
- [ ] Switch to manual mode → `document.title` reads "Manual Intake | Patient Portal"; `<h1>` reads "Manual intake form — complete…"; live region announces "Switched to manual intake form"
- [ ] Switch back to AI mode → `document.title` reads "AI Intake | Patient Portal"; `<h1>` reverts; live region announces "Switched to AI-assisted intake"
- [ ] No `const srOnly` object in IntakePage.tsx; all `<h1>` headings use `className="sr-only"`

## Implementation Checklist
- [x] Add skip link `<a href="#main-intake" className={styles.skipLink}>Skip to main content</a>` before `<nav>`; added `id="main-intake"` to `<main>` in both JSX branches; `.skipLink` in IntakePage.module.css (WCAG 2.4.1, UXR-203)
- [x] Add `role="status" aria-live="polite"` live region with `modeAnnouncement` state; announcement updated in `handleModeSwitch` (WCAG 4.1.3, UXR-203)
- [x] Fixed `document.title` `useEffect` — depends on `[confirmed, mode]`; sets `'Manual Intake | Patient Portal'` when manual (F016, WCAG 2.4.2)
- [x] Fixed `<h1>` — conditional text based on `mode`: `'Manual Intake — ...'` vs `'AI Conversational Intake — ...'` (F017, WCAG 1.3.1)
- [x] Added `--color-success-text: var(--color-green-700)` to `variables.css` with contrast comment; `ManualIntakeForm` uses `--color-success-text` for section complete badges (axe-core contrast fix, UXR-203)
- [x] Deleted `const srOnly` object; replaced `style={srOnly}` → `className="sr-only"` at both heading sites (F015)
- [x] Progress bar in manual mode: `ManualIntakeForm` renders `<IntakeProgressBar current={completedSections} total={5}>` internally (UXR-502)
