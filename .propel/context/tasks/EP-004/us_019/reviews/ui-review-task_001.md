---
review-type: ui-review
task: task_001_frontend-manual-intake
screen: SCR-008
wireframe: wireframe-SCR-008-manual-intake.html
server: http://localhost:5173/intake/demo-appointment-001
viewports-tested: "1440px, 768px, 375px"
date: "2026-05-20"
verdict: CONDITIONAL PASS
overall-score: 72
---

# Design Review Report (Re-run: 2026-05-20)

**Screen:** SCR-008 — Manual Intake Form
**Task:** task_001_frontend-manual-intake (US_019, EP-004)
**Wireframe:** wireframe-SCR-008-manual-intake.html (HTML Hi-Fi)
**Implementation Path:** `/intake/:appointmentId` → Manual mode
**Server URL:** http://localhost:5173/intake/demo-appointment-001
**Review Date:** 2026-05-20 (full re-run — `ManualIntakeForm` now implemented)
**Reviewer:** analyze-ux workflow
**Screenshots:** `wireframe_desktop_1440.png`, `wireframe_tablet_768.png`, `wireframe_mobile_375.png`, `impl_desktop_1440_full.png`, `impl_tablet_768.png`, `impl_mobile_375.png`, `impl_validation_state.png`, `impl_focus_skip_link.png`, `impl_focus_nav_btn.png`

> **Delta from previous review (2026-05-19):** All prior Blockers resolved — `ManualIntakeForm.tsx` is now implemented, skip link added (`#main-intake`), live region for mode switch added, page title and H1 updated dynamically, `--color-success-text: var(--color-green-700)` added. New issues identified in this re-run below.

---

## Summary

The `ManualIntakeForm` component delivers a fully-structured 5-section collapsible intake form with solid ARIA foundations: skip link, `aria-expanded` toggles, `role="alert"` error spans, `aria-describedby` error linkage, `role="progressbar"`, and an `aria-live="polite"` region for mode announcements. Focus management on validation failure and field preservation across mode switches both work correctly (AC-002, AC-004). Prior Blockers from the 2026-05-19 review are fully resolved.

**What works well:** Core accessibility semantics, PHI field distinction (`--color-surface-phi`), keyboard focus ring (`:focus-visible` with `--color-blue-700`), no horizontal scroll at any breakpoint, correct design tokens for colors and typography, dynamic page title and H1, and inline validation with focus routing.

**Key gaps:** The loading state (required per SCR-008 spec) is absent; the progress bar `aria-valuenow` stays at `0%` and uses "Question" terminology; touch targets at 375px fall below the 44×44px minimum for 4 elements; and multiple wireframe deviations persist — missing page subtitle, section emoji icons replaced with numbered badges.

---

## Findings

### Blockers

None.

---

### High-Priority Issues

#### H001 — Loading State Absent (SCR-008 Required State)

The `Submit intake →` button has no loading/disabled state while `submitManualIntake` is in flight. The form remains fully interactive and the button does not change appearance. SCR-008 specifies four required states in `designsystem.md`: Default, Loading, Error, Validation — Loading is unimplemented.

**Impact:** Patient can double-submit; no feedback that the request is processing; degrades perceived performance (UXR-501 — 200ms feedback expectation).

**Affected viewports:** All (1440px, 768px, 375px)

**Expected:** Submit button shows spinner or `aria-busy="true"` + disabled state with "Submitting…" label while the POST is in flight. Form is inert during submission.

**Actual:** Button remains fully enabled; no visual change during submission.

---

#### H002 — Progress Bar `aria-valuenow` Always Reports 0% (UXR-502)

The `IntakeProgressBar` component always shows `aria-valuenow="0"` and the label reads **"Question 0 of 5"** regardless of how many sections are complete. Sections are never counted as complete on initial load because `isSectionComplete()` requires non-empty field values, and the form initialises with empty `values`.

**Impact:** Screen reader users hear "0% complete" throughout the entire form. Sighted users see no progress feedback. Violates UXR-502 (progress indicator must advance per step).

**Affected viewports:** All

**Expected:** "Section 1 of 5" on load (first section open). Progress advances as required fields in each section are filled.

**Actual:** "Question 0 of 5", `aria-valuenow="0"`, no visual progress.

---

### Medium-Priority Suggestions

#### M001 — Section Icons Missing — Numbered Badges Used Instead of Emoji (Wireframe Deviation)

The wireframe uses section-specific emoji icons in section headers:
`🩺 Chief Complaint | 💊 Medications | ⚠ Allergies | 🏃 Lifestyle | 📝 Additional notes`

The implementation replaces these with plain numeric badges `"1" "2" "3" "4" "5"`.

**Impact:** Lower visual recognition speed; patients cannot scan sections as quickly. Deviates from Hi-Fi wireframe.

**Recommendation:** Restore wireframe emoji as `aria-hidden="true"` decorative icons alongside the section title.

---

#### M002 — Page Subtitle Missing (Wireframe Deviation)

Wireframe shows the instructional subtitle: _"Complete all sections. Required fields are marked \*. Your information is encrypted."_ directly below the `<h1>`. This copy is absent from the implementation.

**Impact:** Required-field convention and encryption assurance are not communicated to patients upfront.

**Recommendation:** Add `<p>` subtitle below the visually-hidden `<h1>`, positioned above the first section card.

---

#### M003 — Touch Targets Below 44px at 375px (UXR-301)

Four elements fall below the 44×44px minimum touch target at 375px viewport:

| Element | Measured Height | Measured Width | Minimum |
|---------|-----------------|----------------|---------|
| Skip link | 38px | 156px | 44px |
| Nav logo/back link | 32px | 32px | 44×44px |
| Symptom duration `<select>` | 38px | 278px | 44px |
| Pain scale range `<input>` | 18px | 234px | 44px |

The range slider is particularly problematic at 18px — effectively impossible to interact with on a touch device.

**Violates:** UXR-301 (touch targets ≥44×44px at 375px).

**Recommendation:** Set `min-height: 44px` on `.skipLink`, `.navLogo` wrapper, `select` elements, and increase the range input's thumb size and track hit area.

---

#### M004 — `aria-describedby` Error Targets Absent for Collapsed Sections (UXR-204)

Collapsed section bodies are conditionally rendered (not in DOM). When the user submits with required fields empty in Sections 2–5, the error span IDs (`medications-err`, `allergies-err`) do not exist in the DOM at validation time. The `aria-describedby` attribute references these IDs before the section re-renders.

**Impact:** Screen readers cannot announce the error description for fields in collapsed sections at the moment of submission — there is a brief ARIA reference gap. Only resolves after React re-renders and the 50ms `setTimeout` fires.

**Evidence:**
```
document.getElementById('medications-err') → null (when Section 2 collapsed)
document.getElementById('allergies-err')   → null (when Section 3 collapsed)
```

**Recommendation:** Either (a) always render section bodies in DOM using CSS visibility (`display: none` instead of conditional mounting), or (b) delay setting `aria-describedby` until after the section body is mounted.

---

#### M005 — Progress Label Uses "Question" Terminology (UXR-502)

The progress header reads **"Question 0 of 5"** — copied from the AI intake mode which uses questions. The manual form uses _sections_, not questions.

**Impact:** Confusing UX — "Question" implies AI Q&A mode; patients may think they're in the wrong mode.

**Recommendation:** Change label to "Section N of 5" in `IntakeProgressBar` when `mode === 'manual'`, or pass a `labelPrefix` prop.

---

### Nitpicks

- **Nit N001:** Section collapse chevron is a plain `"▼"` Unicode text character. Use a CSS `transform: rotate()` chevron icon or SVG for visual consistency with the wireframe (which has no explicit chevron).
- **Nit N002:** PHI note text reads "Protected health information" vs wireframe's "This section contains protected health information." — More explicit text helps patients understand the implication.
- **Nit N003:** Footer layout deviation — wireframe places `[Save & continue later]` on left and `[Submit intake →]` on right. Implementation adds a third `[← Switch to AI]` ghost button on the far left, pushing the two primary actions right. Minor layout drift from wireframe.
- **Nit N004:** Five raw pixel values in `ManualIntakeForm.module.css` violate UXR-401 (no hard-coded px outside the primitive token table): `28px` (section badge size), `96px` (textarea min-height), `6px` (range track), `44px` (option label height), `16px` (checkbox size). Map to spacing tokens where possible.

---

## Testing Coverage

### Tested Successfully

| Scenario | Viewport | Result |
|---|---|---|
| Manual form loads after mode switch | 1440px | PASS |
| All 5 section cards render with correct structure | 1440px | PASS |
| Section 1 accordion opens on load | 1440px | PASS |
| Section accordion expand/collapse via click | 1440px | PASS |
| Skip link `href="#main-intake"` target resolves | 1440px | PASS |
| `aria-expanded` reflects open/collapsed state | 1440px | PASS |
| Focus ring `:focus-visible` (3px solid blue) present | 1440px | PASS |
| Page title changes to "Manual Intake \| Patient Portal" | 1440px | PASS |
| Live region announces mode switch | 1440px | PASS |
| Submit with empty required fields → errors visible + first field focused | 1440px | PASS |
| `aria-describedby` links to error span for open section fields | 1440px | PASS |
| PHI fields use `--color-surface-phi` background | 1440px | PASS |
| PHI lock icon present with `aria-hidden="true"` | 1440px | PASS |
| Design token colors used (no raw hex in components) | 1440px | PASS |
| No horizontal scroll | 768px | PASS |
| No horizontal scroll | 375px | PASS |
| Method toggle shows "Manual" as active/pressed | 1440px | PASS |

### State Coverage

| State | Required (SCR-008) | Implemented | Notes |
|-------|-------------------|-------------|-------|
| Default | ✓ | ✓ | Form loads correctly |
| Loading | ✓ | ✗ | No submit loading indicator |
| Error (API) | ✓ | Partial | Inline submit error banner exists but not visible in test |
| Validation | ✓ | ✓ | Inline errors with focus management |

**State coverage: 2.5 / 4 (63%)**

### UXR Requirements Validation

| UXR-ID | Requirement | Result | Notes |
|--------|-------------|--------|-------|
| UXR-103 | Field preservation on switch | ✓ PASS | Data preserved; live region confirms |
| UXR-201 | WCAG contrast | ✓ PASS | Token colours; `--color-success-text: green-700` (4.75:1) |
| UXR-202 | Keyboard navigation + focus ring | ✓ PASS | `:focus-visible` 3px solid blue, 2px offset |
| UXR-203 | Labels + ARIA for all fields | ✓ PASS | All inputs labeled; radios in fieldset/legend |
| UXR-204 | `aria-describedby` for error messages | ⚠ PARTIAL | Broken for collapsed sections (M004) |
| UXR-301 | Responsive layout ≥375px | ⚠ PARTIAL | Layout OK; touch targets fail (M003) |
| UXR-401 | Token governance (no raw px/hex) | ⚠ PARTIAL | 5 raw px values in CSS (N004) |
| UXR-402 | PHI field distinction | ✓ PASS | Lock icon + `--color-surface-phi` background |
| UXR-502 | Progress indicator per step | ✗ FAIL | `aria-valuenow` stuck at 0 (H002) |

### Metrics

| Metric | Value |
|--------|-------|
| Viewports tested | Desktop (1440px), Tablet (768px), Mobile (375px) |
| Console errors | 4 (API 401/404 — backend not connected in dev, expected) |
| Console warnings | 2 |
| Touch target failures at 375px | 4 elements |
| Raw px token violations | 5 instances |
| Horizontal scroll | None at any breakpoint |
| State coverage | 63% (2.5/4) |

---

## Deviation Report — Wireframe vs Implementation

| Element | Wireframe (SCR-008) | Implementation | Deviation | Severity |
|---------|---------------------|----------------|-----------|----------|
| Section icons | 🩺💊⚠🏃📝 emoji | "1" "2" "3" "4" "5" badges | Visual mismatch | Medium |
| Page subtitle | "Complete all sections. Required fields…" | Missing | Absent | Medium |
| Progress label | "Section 1 of 5" | "Question 0 of 5" | Wrong term + wrong count | Medium |
| Progress bar position | Outside `<form>` (separate div) | Inside `<form>` | Structural diff | Low |
| H1 visibility | Visible "Pre-appointment intake" | Visually hidden (sr-only) | Design deviation | Low |
| Section chevron | None | "▼" text | Extra UI element | Nitpick |
| PHI note text | "This section contains protected health information." | "Protected health information" | Minor text diff | Nitpick |
| Footer layout | Save \| Submit (2 buttons) | Switch AI \| Save \| Submit (3 buttons) | Extra button added | Nitpick |
| Nav bar | 64px, logo, method switch, avatar | 64px ✓ | None | — |
| PHI field background | `--color-surface-phi` | `--color-surface-phi` ✓ | None | — |
| Focus ring | 3px solid primary | 3px solid `--color-blue-700` ✓ | None | — |
| Responsive layout | No horizontal scroll | No horizontal scroll ✓ | None | — |
| Error spans | `role="alert"` per field | `role="alert"` ✓ | None | — |
| `lang="en"` | Present | Present ✓ | None | — |

---

## Recommendations

1. **Fix progress bar state logic** — The `completedSections` counter and `aria-valuenow` must reflect actual completion. Add a `useEffect` that re-evaluates `isSectionComplete()` against current `values` whenever the field state changes, so progress advances in real-time. Also replace "Question N of 5" with "Section N of 5".

2. **Add submit loading state** — Disable the submit button and show a `aria-busy="true"` spinner during the `submitManualIntake` call. This satisfies the SCR-008 Loading state requirement, prevents double-submission, and meets UXR-501 (200ms feedback expectation). This is the highest-impact fix with least implementation effort.

---

## Findings

### Blockers

- **Manual intake form not implemented — full form body absent**
  The `ManualIntakeForm.tsx` component referenced in the task does not exist. Switching to
  manual mode renders `ManualIntakeFormPlaceholder` with the text "Manual intake form will be
  available in US_019." All five wireframe sections (Chief Complaint, Medications, Allergies,
  Lifestyle, Additional Notes), the per-section PHI notices, the pain scale, the "Submit intake"
  CTA, the "Save & continue later" ghost button, and all field-level validation (AC-001, AC-002)
  are missing.
  - **Screenshot:** `impl_manual_desktop_1440.png`
  - **Steps to reproduce:** Navigate to `/intake/:appointmentId` → click "📝 Manual" mode
    toggle.
  - **Expected:** Full multi-section intake form with collapsible sections, required-field
    markers, inline validation, and submit/save CTAs (wireframe-SCR-008-manual-intake.html).
  - **Actual:** Single placeholder paragraph; no form fields.
  - **Impact:** AC-001 (form displays all fields), AC-002 (inline validation), AC-004
    (pre-population from AI), AC-005 (mode switch back to AI) are all unverifiable.

- **axe-core violation: `#sec-1-status` colour-contrast (WCAG AA, serious)**
  The "✓ Complete" status badge in the wireframe's Chief Complaint section header renders
  `rgb(22, 163, 74)` (`--color-green-600`, `#16A34A`) on white. Computed contrast ratio = **3.30:1**.
  WCAG AA requires **4.5:1** for text < 18px / < 14px bold. This element is 12px / 600.
  - **axe-core report:** 1 violation (`color-contrast`, impact: `serious`), 25 passes, 2 incomplete.
  - **Fix for implementation:** Use `color: var(--color-green-700)` (`#15803D`, ≈ 4.75:1 on white)
    or add `--color-success-text: var(--color-green-700)` alias to `variables.css`.
    The existing `--color-success` token (`--color-green-600`) must not be used for text on white.
  - **Affected viewport:** All (colour issue).
  - **Screenshot:** `wireframe_validation_state.png` — badge visible in sec-1 header.

- **No skip navigation link (WCAG 2.4.1 — Level A)**
  The page has no `<a href="#main">Skip to main content</a>` link or equivalent mechanism.
  Keyboard-only and screen-reader users must Tab through 3 nav-level elements (back link, AI
  button, Manual button) before reaching page content. On a multi-section form this gap is
  amplified.
  - **Steps to reproduce:** Press Tab from page load; observe focus cycles through nav before
    reaching content.
  - **Expected:** First focusable element is a visually hidden skip link that jumps focus to
    `<main>`.
  - **Actual:** No skip link present; first focus stop is the "Patient Portal" nav link.
  - **WCAG reference:** WCAG 2.2 SC 2.4.1 (Bypass Blocks).

### High-Priority Issues

- **No ARIA live region for mode switch announcement (WCAG 4.1.3)**
  When the patient activates the "📝 Manual" button via keyboard or assistive technology, there is
  no `aria-live` or `role="status"` region that announces the mode change. Screen reader users
  activate the button (which is correct), but receive no feedback that the page content has
  changed to manual mode. The mode switch is a significant state change — equivalent to a
  page transition — and requires an audible announcement.
  - **Verified:** Fresh audit confirms `liveRegions: []` — no live regions present after mode switch.
  - **Expected:** A `role="status"` region (or `aria-live="polite"`) announces
    "Switched to manual intake form" after activation.
  - **WCAG reference:** WCAG 2.2 SC 4.1.3 (Status Messages).

- **Page `<title>` not updated when switching to manual mode (WCAG 2.4.2)**
  `document.title` remains `"AI Intake | Patient Portal"` after switching to manual mode.
  Screen reader users who rely on the page title for orientation will receive incorrect context.
  - **Verified:** `document.title === "AI Intake | Patient Portal"` after clicking "📝 Manual".
  - **Expected:** `"Manual Intake | Patient Portal"` when in manual mode.
  - **WCAG reference:** WCAG 2.2 SC 2.4.2 (Page Titled).

- **H1 heading not updated on mode switch**
  The page `<h1>` reads "AI Conversational Intake — complete your pre-visit questionnaire" in
  both AI and manual modes. After the mode switch, the heading no longer describes the current
  screen state, which breaks orientation for screen readers.
  - **Verified:** Snapshot shows `heading "AI Conversational Intake…"` with `mode === Manual`.
  - **Expected:** H1 updates to "Pre-appointment intake — manual form" (or similar) when in
    manual mode, consistent with the wireframe's `<h1>Pre-appointment intake</h1>`.

- **Validation state (SCR-008/Validation) not implemented**
  The wireframe defines a Validation state (figma_spec.md SCR-008 required states: Default,
  Loading, Error, Validation). Without `ManualIntakeForm.tsx`, the Validation state — inline
  errors with `aria-describedby`, focus-on-first-error (AC-002), required field highlights — is
  entirely absent.

- **Console errors: 401 Unauthorized on intake start**
  `POST /api/v1/intake/start` returns HTTP 401 and `GET /api/v1/intake/session/:id` returns 404.
  The error banner ("Intake start failed: 401") appears immediately on page load. While the retry
  mechanism exists, a 401 without a redirect-to-login response creates a poor first impression
  and a UX dead-end for unauthenticated sessions.
  - **Console evidence:**
    - `[ERROR] 401 Unauthorized @ http://localhost:5174/api/v1/intake/start`
    - `[ERROR] 404 Not Found @ http://localhost:5174/api/v1/intake/session/appt-demo-001`

### Medium-Priority Suggestions

- **Wireframe defines `role="group"` without accessible names on two radio groups**
  The Hi-Fi wireframe (SCR-008) contains two `role="group"` elements that lack an `aria-label`
  or `aria-labelledby` attribute:
  - Section 3 (Allergies): `<div class="radio-group" role="group">` for severity options — no
    accessible name. Screen readers will announce an unnamed group.
  - Section 4 (Lifestyle): `<div class="radio-group" role="group">` for smoking status — no
    accessible name.
  The medications group (`aria-label="Medication categories"`) is correctly annotated.
  When `ManualIntakeForm.tsx` is built, these groups must use `aria-label` or
  `aria-labelledby` pointing to the visible label element.
  - **WCAG reference:** WCAG 2.2 SC 1.3.1 (Info and Relationships).

- **Progress bar not contextualised for manual mode**
  In manual mode, the progress bar shows "0% complete" and "Question 0 of 8" — labels that
  belong to the AI conversational context. The wireframe shows "Section 1 of 5" and a progress
  bar that advances per section. Once `ManualIntakeForm` is built, the progress indicator should
  reflect section completion, not AI question count.

- **Unmapped AI field annotation missing**
  The figma_spec.md edge case specifies: "AI intake field unmapped on switch → shown blank with
  `[!] Please fill in this field` annotation." The current placeholder does not preserve
  `capturedFields` from AI mode into the manual form UI. When `ManualIntakeForm` is built, this
  annotation pattern must be implemented.

- **"Save & continue later" affordance absent**
  The wireframe shows a "Save & continue later" ghost button in the form footer. The
  `handleSaveLater` callback exists in `IntakePage.tsx` but is not connected to any button in
  manual mode (the placeholder renders no footer).

- **`document.title` and H1 not scoped per intake method in `useEffect`**
  The existing `useEffect` in `IntakePage.tsx` updates `document.title` only on `confirmed`
  state change, not on `mode` change. The fix is to add `mode` to the dependency array and
  update the title/heading accordingly.

### Nitpicks

- Nit: The wireframe uses `"Method:"` as the method-switch label. Implementation renders
  `"Method:"` at `font-size-body-sm` with `color-text-secondary` — matches ✓. No change needed.
- Nit: The user avatar renders initials ("TP" for "Test Patient") as an `<img>` element with
  `alt="Test Patient"` in snapshot. This is correct for accessibility but the `<img>` renders a
  text string via CSS — verify it is not a real `<img>` that would require an actual `src` in
  production.
- Nit: Wireframe's `nav-back` link label is "Back to dashboard" while implementation labels it
  "Patient Portal — back to dashboard". The added brand context is acceptable; not a defect.

---

## Deviation Report — Wireframe vs Implementation

| Element | Wireframe (SCR-008) | Implementation | Deviation | Severity |
|---------|---------------------|---------------|-----------|----------|
| Nav bar | 64px, logo, method switch, avatar | 64px ✓ | None | — |
| Method switch | Pill shape, aria-pressed, "Method:" label | Matches ✓ | None | — |
| Progress bar | "Section 1 of 5", 20% fill | "0 of 8 questions", 0% fill | Mismatch (AI context) | Medium |
| Section 1: Chief Complaint | Collapsible, PHI note, textarea, select, range | NOT RENDERED | Full absence | Blocker |

