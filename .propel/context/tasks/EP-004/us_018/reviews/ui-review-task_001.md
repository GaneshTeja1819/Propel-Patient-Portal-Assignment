# Design Review Report — SCR-007 AI Intake Chat

<!-- propelIQ review | screen: SCR-007 | task: task_001 | us: us_018 | reviewer: GitHub Copilot -->

| Field | Value |
|---|---|
| Screen | SCR-007 — AI Conversational Intake |
| Task | task_001_frontend-ai-intake-chat |
| US | us_018 |
| Date | 2025-05-18 |
| Last Delta | 2025-05-18 (delta review — 5 fixes verified) |
| Verdict | **Conditional Pass** — 0 blockers, 0 high, 3 medium |
| Score | 93 / 100 |

---

## Summary

The SCR-007 AI Intake Chat implementation is structurally sound and closely matches the
wireframe. Navigation, method-switch toggle (UXR-103), PHI annotation (UXR-402), chat bubble
differentiation (UXR-403), and responsive layout (UXR-301) all pass. The progress bar
(UXR-502) and quick-option chips are implemented with correct ARIA markup.

**Delta (2025-05-18):** All 3 blockers and 2 high-priority issues from the initial review
have been resolved. Score moves from 84/100 → 93/100. Three medium-priority items remain
open.

**What works well:**
- Method-switch toggle with `aria-pressed` (AI / Manual) matches wireframe exactly.
- PHI surface colour (`--color-surface-phi`) applied in both AI bubble and input textarea.
- Retry button present in error state with `role="alert" aria-live="assertive"`.
- No horizontal scroll at 375px (UXR-301 pass).
- Quick-option chips with `role="group"`, `aria-label`, and keyboard accessible.
- Send button correctly disabled on session load failure.
- `document.title` updates to `"AI Intake | Patient Portal"` on mount (B-001 ✅).
- `<h1>` is first child of `<main>` (B-002 ✅).
- Focus glow uses `--focus-ring-glow` token (B-003 ✅).
- Progress bar renders unconditionally with `totalQuestions || 8` fallback (H-001 ✅).
- `--layout-content-max-width: 760px` token used across all 3 CSS files (H-002 ✅).

---

## Findings

### Blockers

> **Delta 2025-05-18: All 3 blockers resolved. ✅**

- **B-001 — `<title>` not updated at runtime (WCAG 2.4.2) — ✅ FIXED**

  `IntakePage.tsx` now uses `useEffect` with `[confirmed]` dependency to set
  `document.title`. Live DOM confirms `document.title = "AI Intake | Patient Portal"`.
  Cleanup resets to `"Patient Portal"` on unmount.
  - **Playwright evidence:** `documentTitle = "AI Intake | Patient Portal"` ✓

- **B-002 — `<h1>` rendered outside `<main>` (landmark structure / WCAG 1.3.1) — ✅ FIXED**

  The visually-hidden `<h1>` is now the first child of `<main>` with the comment
  `{/* B-002: h1 inside main for correct landmark association (WCAG 1.3.1) */}`.
  - **Playwright evidence:** `h1Parent = "MAIN"`, `h1InsideMain = true` ✓

- **B-003 — Raw `rgba` value in `.textarea:focus` focus glow (UXR-401) — ✅ FIXED**

  `variables.css` defines `--focus-ring-glow: rgba(26, 86, 219, 0.2)`.
  `AIIntakeChat.module.css` now uses `outline: 3px solid var(--focus-ring-glow)`.
  - **Playwright evidence:** `getPropertyValue('--focus-ring-glow') = "rgba(26, 86, 219, 0.2)"` ✓

---

### High-Priority Issues

> **Delta 2025-05-18: Both high-priority issues resolved. ✅**

- **H-001 — Progress bar hidden during error state (UXR-502 partial) — ✅ FIXED**

  `IntakePage.tsx` now renders `<IntakeProgressBar current={questionNumber || 0} total={totalQuestions || 8} />`
  unconditionally inside `<main>`.
  - **Playwright evidence:** `progressBarFound = true`, `progressBarVisible = true` ✓

- **H-002 — `760px` max-width magic number (UXR-401 adjacent) — ✅ FIXED**

  `variables.css` defines `--layout-content-max-width: 760px`.
  Both `IntakeProgressBar.module.css` and `AIIntakeChat.module.css` use
  `max-width: var(--layout-content-max-width)`.
  - **Playwright evidence:** `getPropertyValue('--layout-content-max-width') = "760px"` ✓

---

### Medium-Priority Suggestions

- **M-001 — axe-core false-positive on `--color-danger` border colour**

  The `errorBanner` border uses `--color-danger` (#dc2626). axe-core interprets the border
  as a foreground colour and flags contrast 3.95:1 against `#fee2e2`. The actual text uses
  `--color-danger-text` (#b91c1c, 6.22:1 — correct). The border-only usage is not a real
  WCAG violation, but it creates noise in CI accessibility audits.

  - **Recommendation:** Change the border token to `--color-danger-text` (#b91c1c) which
    eliminates the false-positive and maintains visual consistency.

- **M-002 — Quick-option chips lack `:active` press feedback**

  `.quickOption` defines `:hover` and `:focus-visible` but no `:active` state. Touch users
  tapping a chip receive no immediate tactile feedback.

  - **Recommendation:** Add
    `.quickOption:active { background: var(--color-brand-primary-light); transform: scale(0.97); }`.

- **M-003 — `ManualIntakeFormPlaceholder` uses inline styles**

  The placeholder component uses inline `style={{ ... }}` with raw `var()` references.
  Minor style inconsistency — not a token violation (values are still custom properties)
  but inconsistent with the CSS Modules pattern used everywhere else.

  - **Recommendation:** Extract to `IntakePage.module.css` class in a follow-up cleanup.

---

### Nitpicks

- **Nit-001:** `userAvatar` uses `role="img"` + `aria-label` on a `<div>`. Valid per spec,
  but an `<abbr title="Alex Patient">AP</abbr>` pattern is semantically cleaner.
- **Nit-002:** The `btnRetry` in the error banner uses `--color-danger-text` with a
  transparent background. The contrast of red on white (transparent over red-100 parent)
  is visually correct, but a dedicated `.btnRetry` background token would be cleaner.

---

## Testing Coverage

### Tested Successfully

| Test | Result |
|---|---|
| Route renders at `/intake/:appointmentId` | ✓ Pass |
| Method switch `aria-pressed` state correct | ✓ Pass |
| Chat area `role="log" aria-label="Intake conversation"` | ✓ Pass |
| Nav height 64px (matches wireframe) | ✓ Pass |
| No horizontal scroll at 375px | ✓ Pass |
| Error banner `role="alert" aria-live="assertive"` | ✓ Pass |
| Error text contrast — `--color-danger-text` 6.22:1 ✓ | ✓ Pass |
| Retry button present in error state | ✓ Pass |
| `<h1>` in DOM (visually hidden) | ✓ Pass |
| PHI note in input area | ✓ Pass |
| Send button disabled in error/load state | ✓ Pass |
| Focus tokens on send button (`:focus-visible`) | ✓ Pass |
| Responsive layout at 375px, 768px, 1440px | ✓ Pass |
| UXR-103: capturedFields preserved on mode switch | ✓ Pass (code review) |
| PHI values stripped before sessionStorage write | ✓ Pass (code review) |
| **B-001: `document.title` updated at runtime** | ✓ Pass (delta fix) |
| **B-002: `<h1>` inside `<main>`** | ✓ Pass (delta fix) |
| **B-003: focus glow uses `--focus-ring-glow` token** | ✓ Pass (delta fix) |
| **H-001: progress bar visible in error state** | ✓ Pass (delta fix) |
| **H-002: `--layout-content-max-width` token in all CSS** | ✓ Pass (delta fix) |
| All buttons have accessible names | ✓ Pass |
| Landmark structure (1 nav, 1 main) | ✓ Pass |
| Single `<h1>` per page | ✓ Pass |

### Metrics

| Metric | Value |
|---|---|
| Viewports tested | 1440px, 768px, 375px |
| axe-core violations (live DOM) | 0 — region violation resolved by B-002 fix |
| axe-core warnings (console) | 1 — `color-contrast` border false-positive (M-001 open) |
| Console errors | 2 (API 404 — backend not running, expected) |
| UXR-401 token violations | 0 — B-003 and H-002 resolved |
| `document.title` updated at runtime | ✓ Yes (B-001 fixed) |
| Progress bar visible in error state | ✓ Yes (H-001 fixed) |
| `<h1>` inside `<main>` | ✓ Yes (B-002 fixed) |

---

## UXR Compliance Matrix

| Requirement | Status | Notes |
|---|---|---|
| UXR-103: data preserved on method switch | ✓ Pass | `capturedFields` held in hook state |
| UXR-201: colour contrast ≥ 4.5:1 | ⚠ Partial | Text passes; border false-positive (M-001) |
| UXR-202: keyboard nav, visible focus ≥ 3px | ✓ Pass | All elements use `var(--focus-outline-*)` |
| UXR-203: ARIA labels on form fields | ✓ Pass | Textarea, send, method buttons, log labelled |
| UXR-204: `aria-describedby` for validation | N/A | Error state = full banner, no inline validation |
| UXR-301: usable at 1280/768/375px | ✓ Pass | No horizontal scroll confirmed |
| UXR-401: only design tokens | ✓ Pass | B-003 + H-002 resolved; 0 raw values remain |
| UXR-402: PHI fields visually annotated | ✓ Pass | `--color-surface-phi` + 🔒 note |
| UXR-403: AI content visually distinct | ✓ Pass | `--color-ai-accent-bg` bubble + header |
| UXR-502: progress bar / step counter | ✓ Pass | Renders unconditionally via `totalQuestions \|\| 8` (H-001 fixed) |

---

## Required Fixes Before Merge

> **Delta 2025-05-18:** All 3 original blockers are resolved. No merge-blocking issues remain.

Open medium items (non-blocking, recommended before next sprint):

1. **M-001** — Change `errorBanner` border from `--color-danger` to `--color-danger-text` in
   `IntakePage.module.css` line 135 to eliminate axe-core false-positive.
2. **M-002** — Add `.quickOption:active { background: var(--color-brand-primary-light); transform: scale(0.97); }`
   to `AIIntakeChat.module.css`.
3. **M-003** — Extract `ManualIntakeFormPlaceholder` inline styles to `IntakePage.module.css`
   (follow-up cleanup, low priority — component is a placeholder for US_019).

---

## Delta History

| Date | Delta | Score | Issues Fixed | Open |
|---|---|---|---|---|
| 2025-05-18 | Initial review | 84/100 | — | 3 blockers, 2 high, 3 medium |
| 2025-05-18 | Delta — 5 fixes verified | 93/100 | B-001, B-002, B-003, H-001, H-002 | 3 medium |

