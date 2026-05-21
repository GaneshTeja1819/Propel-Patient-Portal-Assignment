---
id: ui-review-SCR-008
screen: SCR-008
title: Manual Intake Form
us: us_019
epic: EP-004
wireframe: .propel/context/wireframes/Hi-Fi/wireframe-SCR-008-manual-intake.html
reviewer: GitHub Copilot
date: 2026-05-20
verdict: Conditional Pass
---

# Design Review Report

## Summary

SCR-008 Manual Intake Form wireframe was analysed in two passes: (1) full HTML
source review with design specification cross-reference (prior review, 2026-05-19)
and (2) live Playwright browser analysis served via a local HTTP server at
`http://localhost:8765` (this review, 2026-05-20). Screenshots were captured at
1440 px (desktop), 768 px (tablet), and 375 px (mobile); validation flow was
exercised programmatically; Tab order (29 focusable elements) and console output
were verified. The wireframe itself generates 0 errors and 0 warnings in isolation.
The dev server React app at `http://localhost:5174` continues to emit 2 pre-existing
React Router v6→v7 future-flag warnings unrelated to this screen.

**Delta vs prior review (2026-05-19):** The wireframe source is **unchanged**.
All HIGH and MEDIUM findings from the prior review remain unresolved. No new HIGH
or MEDIUM issues were introduced. One new Nitpick finding (Nit-7) was identified
through live interaction testing.

**What works well:** The wireframe demonstrates strong structural accessibility
(skip link, `<main>` landmark, `aria-expanded` section toggles, `role="alert"` on
error spans, `aria-describedby` on all required fields, focus management on
validation failure, `aria-valuenow` sync on the progress bar and range input). The
design token system is applied consistently (colors, spacing, typography all
reference CSS custom properties). PHI field styling (`color-surface-phi` background,
`color-border-phi` border) correctly excludes the range slider via `:not([type=range])`.
All interactive elements meet the 44 px minimum touch target.

**What needs attention:** Three specification gaps were found (missing Loading state,
missing API Error state, missing DateTimePicker component) and one security concern
(PHI form fields lack `autocomplete` suppression). Iconography uses Unicode emoji
rather than the SVG outlined icon system mandated by the design system.

---

## Findings

### Blockers

No blocking issues found.

---

### High-Priority Issues

**H-001 — Missing Loading state**

The SCR-008 spec requires four states: Default, Loading, Error, Validation.
Only Default and Validation are present in the wireframe. The Loading state
(shown after form submission while the API processes the intake record) is
absent. Patients on slow connections will see a disabled submit button with
the text "Submitting…" but no skeleton loader or progress indicator covering
the form, creating uncertainty about whether the action was registered.

- Expected: SkeletonLoader overlay or at minimum a full-width progress
  indicator replacing the form content during API call (per designsystem.md
  `SkeletonLoader` component; SCR-008 states table).
- Actual: Button disabled + text change only. No form-level loading state.
- Affected viewport: All.

**H-002 — Missing Error state (API failure)**

No server-error / API-failure state is present. If the intake submission
fails (5xx, network timeout), the patient sees only the frozen "Submitting…"
button with no recovery path. The spec requires an Error state distinct from
field validation errors.

- Expected: `Alert/Inline` (danger variant) above the form footer after a
  failed submission — "We couldn't save your intake. Please try again." with
  a retry CTA (per designsystem.md content guidelines: specific + actionable).
- Actual: No error recovery UI present.
- Affected viewport: All.

**H-003 — DateTimePicker(1) component absent from spec**

`designsystem.md` "Required Components per Screen" for SCR-008 lists
`DateTimePicker (1)` alongside TextFields and SelectFields. No DateTimePicker
exists anywhere in the wireframe. The most likely candidate for this component
is a "Symptom onset date" or "Date of birth" field (FR-015 — intake data
collection). The current "How long have you had this symptom?" `<select>`
covers duration but not a specific calendar date.

- Expected: One `DateTimePicker` field (exact semantic context needs
  clarification from design owner — symptom onset date is most probable).
- Actual: Field absent; duration covered by select dropdown only.

**H-004 — PHI fields missing `autocomplete` suppression**

All five PHI textarea and select fields (chief-complaint, symptom-duration,
allergies, medications, notes) lack `autocomplete="off"` (or an equivalent
`autocomplete` value that prevents browser autofill). Without suppression,
browsers may auto-populate sensitive medical history from previous sessions
or across patient devices, creating a HIPAA data leakage risk.

- Expected: `autocomplete="off"` on all PHI-classified fields (OWASP A02 /
  HIPAA minimum-necessary principle).
- Actual: No `autocomplete` attribute on any PHI field.

---

### Medium-Priority Suggestions

**M-001 — PHI lock indicator uses emoji instead of SVG icon (UXR-402)**

The `phi-note` indicator uses emoji `🔒` (Unicode U+1F512) for the PHI lock
icon. The design system specifies: "Outlined (20px, 1.5px stroke weight) —
precision over decoration" (designsystem.md §9 Branding Assets). Emoji
rendering is OS-dependent (colour, weight, size), will not match the outlined
icon style of the rest of the UI, and cannot be styled with CSS.

- Expected: `<svg>` or icon font glyph at 20×20px, 1.5px stroke, matching
  `--color-border-default` or `--cb` colour (per UXR-402).
- Actual: Unicode emoji `🔒`, OS-rendered, no controllable size or stroke.

**M-002 — Section headers and nav use emoji icon set throughout**

All five section headers use emoji icons (🩺 💊 ⚠️ 🏃 📝), and the
method-switch buttons use 🤖 and 📝. These are inconsistent with the outlined
SVG icon system mandated by the design system. At scale they will produce
visual inconsistency with other screens that use SVG icons.

- Expected: SVG outlined icons for stethoscope, pill, warning, person
  running, clipboard, robot/AI, pencil — all at 20px with 1.5px stroke.
- Actual: Unicode emoji throughout.

**M-004 — Topnav may overflow at 375px**

The topnav at 375px contains three full-width elements: logo-mark + label
("Intake Form"), a method-switch pill (with two buttons labelled "🤖 AI" and
"📝 Manual"), and a user avatar circle. No media query adjusts their layout
at 375px. The method-switch pill alone requires approximately 140px; combined
with the logo text and avatar, total content may reach ~330px within a 311px
usable area (375px − 2×32px padding), causing text truncation or overflow.

- Expected: At 375px, either the logo label is hidden (show mark only) or
  the method-switch is moved below the topnav into a sticky secondary bar.
- Actual: No responsive adjustment below 640px for the topnav inner layout.

**M-005 — Hardcoded appointment context in labels**

The progress bar header ("Annual Physical · Dr. M. Okafor · May 20") and
the notes field label ("Anything else **Dr. Okafor** should know?") both
contain hardcoded doctor name and appointment metadata. These will need to
be dynamic in the React implementation, driven by the `appointmentId` route
param.

- Expected: Placeholder tokens `{doctorName}`, `{appointmentType}`,
  `{appointmentDate}` binding to the resolved appointment record.
- Actual: Hardcoded strings — fine for wireframe; must be dynamic in
  implementation.

---

### Nitpicks

- **Nit-1:** The submit button renders `Submit intake &#10148;` (➤ right-pointing
  pointer). Screen readers announce the glyph as "right-pointing pointer" or
  similar. Use `<span aria-hidden="true">➤</span>` or add
  `aria-label="Submit intake form"` to the button.

- **Nit-2:** PHI textareas (chief-complaint, medications, allergies, notes)
  have no `maxlength` attribute. Without a character limit, a patient could
  paste an entire document into a field, causing unexpected API payload sizes.

- **Nit-3:** Progress percentage formula (`20 + open_sections × 16`) is an
  open-section proxy, not field-completion progress. A patient could open all
  5 sections (100%) without entering a single required value. Consider
  driving progress from actual required-field completion.

- **Nit-4:** The `.section-incomplete` CSS class (defined with
  `color: var(--ct-d)` = `#94A3B8`) would render at approximately 2.4:1
  contrast ratio on white — a WCAG AA failure for normal/small text. The
  class is not currently rendered in the wireframe but should be reviewed
  before use in the implementation.

- **Nit-5:** "Save & continue later" is an `<a>` tag linking to the
  dashboard wireframe. In the React implementation this should trigger a
  save-draft API call before navigating, or the link should be replaced with
  a `<button>` that dispatches the save action and then navigates
  programmatically (per UXR-103 — no data loss on method switch).

- **Nit-6:** The page subtitle includes "Your information is encrypted." but
  provides no link to a privacy policy or HIPAA notice. Consider adding a
  small "Privacy notice" link (not required for wireframe; design decision).

- **Nit-7 (NEW — Playwright):** Progress label text is inconsistent between
  initial load and post-interaction state. On load the label reads
  "Section 1 of 5" (implying ordinal section position); after any section
  header is toggled, `updateProgress()` overwrites it with `N of 5 sections
  open` (implying a count of open sections). These are different information
  types. The initial value should either match the dynamic format
  (`1 of 5 sections open`) or the live-region label on the `<progressbar>`
  should be kept separate from the ordinal counter.

---

## Testing Coverage

### Tested Successfully

- HTML structure: semantic `<main>`, `<nav>`, `<form>`, heading hierarchy ✅
- Skip link present with show-on-focus CSS ✅ *(Playwright confirmed: PASS)*
- Method-switch `role="group" aria-labelledby` pattern ✅
- Section headers use `<button type="button">` (not submit) ✅
- `aria-expanded` + `aria-controls` linkage on all 5 sections ✅ *(5 `aria-expanded` instances confirmed)*
- `aria-required="true"` on all required fields ✅ *(4 confirmed)*
- `aria-describedby` pointing to error spans on all 4 validated fields ✅ *(4 confirmed)*
- Error spans use `role="alert"` for live region announcement ✅ *(4 confirmed)*
- Form uses `novalidate` + custom JavaScript validation ✅ *(Playwright confirmed: PASS)*
- First invalid field focused on failed submit ✅ *(Playwright confirmed: focus lands on `#chief-complaint`)*
- `<input type="range">` excluded from PHI background via `:not([type=range])` ✅
- `aria-valuenow` synced on pain scale via `input` event listener ✅
- Progress bar `role="progressbar"` with `aria-valuenow` / `aria-label` synced ✅ *(valuenow=20, min=0, max=100, label="20% complete")*
- Submit button disabled on submission (double-submit prevention) ✅
- `user-avatar` has `aria-hidden="true"` ✅
- Section icons have `aria-hidden="true"` ✅
- CSP-safe event binding (no `onclick` attributes on any element) ✅
- PHI fields styled with `color-surface-phi` (`--cb-l` = `#EFF6FF`) ✅
- Design tokens used consistently for all colors, spacing, typography ✅
- 44px minimum touch target on all interactive controls ✅
- Responsive breakpoint at 640px collapses `.field-row` grid ✅
- Tab order: 29 focusable elements, logical skip-link→nav→method-switch→sections→footer flow ✅ *(Playwright)*
- 0 console errors, 0 warnings from wireframe in isolation ✅ *(Playwright)*

### Metrics

| Dimension | Result |
|---|---|
| States implemented | Default ✅, Validation ✅, Loading ❌, Error ❌ — **2 / 4** |
| Viewports tested | Desktop (1440px) ✅ Playwright screenshot; Tablet (768px) ✅ Playwright screenshot; Mobile (375px) ✅ Playwright screenshot — topnav overflow confirmed |
| Console errors (wireframe) | 0 ✅ *(Playwright)* |
| Console warnings (wireframe) | 0 ✅ *(Playwright)* |
| Console warnings (React dev server) | 2 (React Router v6→v7 future flags — pre-existing, unrelated to SCR-008) |
| UXR-203 compliance | ✅ Pass (all fields labelled) |
| UXR-204 compliance | ✅ Pass (all validated fields have aria-describedby) |
| UXR-402 compliance | ⚠️ Partial (PHI distinction present; icon is emoji not SVG) |
| UXR-502 compliance | ✅ Pass (progress bar with full ARIA) |
| WCAG contrast (rendered text) | ✅ Pass on all currently rendered text |
| WCAG contrast (risk) | ⚠️ `.section-incomplete` token (#94A3B8) fails if rendered |
| Component spec gaps | 1 (DateTimePicker missing) |

---

## Deviation Report

| Element | Expected (designsystem.md) | Actual (wireframe) | Severity |
|---|---|---|---|
| Loading state | SkeletonLoader pattern | Absent | High |
| Error state | Alert/Inline danger + retry CTA | Absent | High |
| DateTimePicker | 1 required per SCR-008 spec | Absent | High |
| PHI field autocomplete | `autocomplete="off"` | Not set | High |
| PHI lock icon | SVG outlined 20×20px, 1.5px stroke (UXR-402) | Unicode emoji 🔒 | Medium |
| Section icons | SVG outlined 20px icons | Unicode emoji set | Medium |
| Mobile topnav layout | Responsive adjustments ≤375px | No topnav breakpoint | Medium |
| Appointment context | Dynamic `{doctorName}`, `{date}` | Hardcoded strings | Medium |
| Submit button icon | Decorative arrow `aria-hidden` | &#10148; not hidden | Nitpick |

---

## Recommendations

1. **Add Loading and Error states before handoff.** Wireframes lacking these states
   will require additional design work during development, which slows the
   React implementation cycle. A loading overlay (translucent mask +
   `<SkeletonLoader>` beneath the active section, or an `aria-busy="true"` form
   wrapper) and a server-error `Alert/Inline` above the footer are sufficient.
   These states should be added to the wireframe file before the screen is
   handed off for US_019 implementation.

2. **Replace emoji icons with the SVG outlined icon set.** The design system
   defines a single icon vocabulary (outlined, 20px, 1.5px stroke). Mixing
   OS-rendered emoji with a CSS-styled SVG icon set creates visual
   inconsistency across screens and breaks at scale when other screens
   deliver designed icon variants (hover, active, disabled states). Create a
   reusable `<Icon name="…">` component and replace all emoji in the
   wireframe before implementation begins.
