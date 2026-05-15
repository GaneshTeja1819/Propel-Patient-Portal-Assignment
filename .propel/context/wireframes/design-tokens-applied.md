# Design Tokens Applied

> **Project:** UPACIP Hi-Fi Wireframe Set · 16 screens
> **Source:** `designsystem.md` (canonical token definitions)
> **Date:** 2026-05-20

---

## Aesthetic Direction

**Direction:** Utilitarian — clinical precision, trust-first AI, information hierarchy over decoration.

This direction was selected because:
1. The primary users are patients managing health records and staff making clinical decisions. Decorative UI patterns create cognitive noise and undermine clinical trust.
2. AI-generated data requires clear visual attribution — a distinct AI accent colour that is serious, not playful.
3. PHI fields require immediate visual distinction to prevent accidental disclosure and reinforce HIPAA compliance expectations.

**Key constraints applied from this direction:**
- No gradient backgrounds, no large imagery, no hero sections.
- Every colour token serves a semantic purpose (status, role, interaction, PHI, AI).
- Whitespace is structural (grid-based), not decorative.
- Typography is functional: only 3 weights used (400 / 500 / 600 / 700 max).
- Motion limited to transitions ≤ 200ms (slot selection), drawer slide ≤ 300ms, spinner ≤ 600ms.

---

## Token Sources

All tokens are declared identically in every wireframe's `<style>` block inside `:root {}`. The source of truth is `designsystem.md`.

### Colour Tokens

| Token | Value | Semantic name | Usage |
|---|---|---|---|
| `--cb` | `#1A56DB` | brand-blue | Primary buttons, active nav items, focus rings, links |
| `--cb-h` | `#1446BE` | brand-blue-hover | Button hover state |
| `--cb-l` | `#EFF6FF` | brand-blue-light | PHI field backgrounds, selected state bg, nav active bg |
| `--cs` | `#FFFFFF` | surface | Cards, modals, drawer backgrounds |
| `--cs-m` | `#F8FAFC` | surface-muted | Page body background |
| `--cs-s` | `#F1F5F9` | surface-subtle | Hover backgrounds, zebra rows, disabled slot bg |
| `--cbord` | `#E2E8F0` | border | Default element borders, dividers |
| `--cbord-f` | `#1A56DB` | border-focus | Focus ring colour (same as brand-blue) |
| `--ct` | `#0F172A` | text-primary | All body text, headings |
| `--ct-s` | `#475569` | text-secondary | Labels, captions, helper text |
| `--ct-d` | `#94A3B8` | text-disabled | Placeholder text, disabled values |
| `--ct-on` | `#FFFFFF` | text-on-dark | Text on coloured backgrounds (buttons, badges) |
| `--c-ok` | `#16A34A` | success | "Verified" badge, success states, connected status |
| `--c-ok-bg` | `#DCFCE7` | success-bg | Success badge/alert backgrounds |
| `--c-warn` | `#D97706` | warning | Advisory banners, medium confidence bars |
| `--c-warn-bg` | `#FEF3C7` | warning-bg | Advisory backgrounds |
| `--c-err` | `#DC2626` | error | Field errors, conflict alerts, low confidence bars |
| `--c-err-bg` | `#FEE2E2` | error-bg | Error/conflict backgrounds |
| `--c-ai` | `#7C3AED` | ai-accent | AI badge, AI content borders, preferred slot candidate, AI left-border accent |
| `--c-ai-bg` | `#EDE9FE` | ai-accent-bg | AI badge backgrounds, preferred slot background |

### Spacing Scale

| Token | Value | Usage pattern |
|---|---|---|
| `--s1` | `4px` | Icon gaps, tight badge padding |
| `--s2` | `8px` | Inline element gaps, compact row padding |
| `--s3` | `12px` | Button padding (sm), list item gaps |
| `--s4` | `16px` | Standard button padding, card inner sections |
| `--s6` | `24px` | Card body padding, section gaps |
| `--s8` | `32px` | Page horizontal padding, major section gaps |
| `--s12` | `48px` | Large section separations (used sparingly) |

### Typography Scale

| Token | Value | Usage |
|---|---|---|
| `--t-xs` | `11px` | Meta labels (card subtitles, badge text, AI confidence %) |
| `--t-sm` | `12px` | Helper text, secondary captions, pill text |
| `--t-base` | `14px` | Body text, form fields, table cells |
| `--t-md` | `16px` | Card titles, section headings, important labels |
| `--t-lg` | `18px` | Sub-page titles |
| `--t-xl` | `20px` | Stat card values, secondary page titles |
| `--t-2xl` | `24px` | Primary page H1 |

**Font family:** `system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif`
**No custom fonts loaded** — zero network requests, instant rendering.

### Shape Tokens

| Token | Value | Usage |
|---|---|---|
| `--r-sm` | `4px` | Buttons (sm), input fields, badges |
| `--r-md` | `6px` | Cards, provider cards, alert strips |
| `--r-lg` | `8px` | Modals, drawers |
| `--r-full` | `9999px` | Pill badges, user avatar circles, step dots |

### Shadow Tokens

| Token | Value | Usage |
|---|---|---|
| `--sh-sm` | `0 1px 2px rgba(0,0,0,.05)` | TopNav, rows on hover |
| `--sh-md` | `0 4px 6px -1px rgba(0,0,0,.07)` | Cards, stat cards |
| `--sh-lg` | `0 10px 15px -3px rgba(0,0,0,.1)` | Modals, drawers |

### Layout Tokens

| Token | Value | Usage |
|---|---|---|
| `--nav-h` | `64px` | TopNav fixed height |
| `--sidebar-w` | `240px` | Declared but not used (future sidebar pattern) |
| `--max-w` | `1280px` | Max content width for all screen layouts |

---

## Token Application by Screen

| Screen | Notable token usages |
|---|---|
| SCR-001 | `--cb` primary button; `--c-err-bg` error alert; `--cs-m` body bg |
| SCR-002 | `--cb` progress bar fill; `--c-ok` strength level 4; `--c-err` level 1 |
| SCR-003 | `--c-warn-bg` intake alert; `--c-err` session timeout border; `--c-ai-bg` AI tile |
| SCR-004 | `--cb-l` selected slot bg; `--c-ai-bg` preferred slot; `--c-warn` insurance badge; `--cb` insurance PHI fields |
| SCR-005 | `--cb-l` PHI fields; `--c-err` cancel modal destructive button |
| SCR-006 | `--c-ai-bg` waitlist card header; `--c-ok-bg` confirmation badge |
| SCR-007 | `--c-ai-bg` AI chat bubble; `--cb` user chat bubble; `--c-ai` typing indicator dots |
| SCR-008 | `--cb-l` PHI textarea bg; `--c-ok` section completed dot; `--c-warn` step in-progress |
| SCR-009 | `--c-ai` AI row left border (3px); `--c-ok` Verified badge; skeleton loader `--cs-s` bg |
| SCR-010 | `--cb-l` drop zone hover; `--c-err-bg` failed upload row; `--c-ok-bg` complete row |
| SCR-011 | `--c-ok-bg` Arrived rows; `--c-warn-bg` Walk-in rows; stats bar all colour tokens |
| SCR-012 | `--c-warn-bg` "no slot" fallback amber; `--cb-l` search results |
| SCR-013 | `--c-err` conflict CRITICAL border (4px); `--c-ai` AI rows in profile; conflict drawer `--sh-lg` |
| SCR-014 | `--c-ok` high-confidence bar; `--c-warn` medium confidence; `--c-err` low confidence; `--c-ai` evidence border |
| SCR-015 | Role badge colours: admin=`--c-err`, staff=`--c-ok`, patient=`--cb`; inactive row opacity 0.6 |
| SCR-016 | `--c-ok-bg` connected status; `--c-warn-bg` sync advisory; `--c-ai-bg` selected provider card hover |

---

## Focus Ring (Applied Globally)

```css
:focus-visible {
  outline: 3px solid var(--cbord-f);   /* #1A56DB */
  outline-offset: 2px;
  border-radius: var(--r-sm);           /* 4px */
}
```

Applied uniformly across all 16 wireframes. Uses `:focus-visible` (keyboard/assistive only — not shown on mouse click). Complies with WCAG 2.2 SC 2.4.11 (Focus Appearance).

---

## AI + PHI Visual Language Summary

### PHI Distinction (UXR-402)

| Property | Value |
|---|---|
| Background | `var(--cb-l)` → `#EFF6FF` |
| Border | `#BFDBFE` (blue-200) |
| Icon | 🔒 (visual-only prefix) |
| `aria-label` | "Contains protected health information" on icon |
| Screens | SCR-004 (insurance fields), SCR-007 (textarea), SCR-008 (sensitive sections), SCR-009/013 (DOB, phone) |

### AI Attribution (UXR-403)

| Property | Value |
|---|---|
| Badge background | `var(--c-ai-bg)` → `#EDE9FE` |
| Badge text | `var(--c-ai)` → `#7C3AED` |
| Row left border | `3px solid var(--c-ai)` |
| Icon | 🤖 |
| Confidence bar | Green ≥90%, Amber 70–89%, Red <70% |

---

## Drift Notes

> Drift notes document any per-screen intentional or emergent deviations from the canonical design token set.

| Screen | Token drift | Reason |
|---|---|---|
| SCR-002 | Password strength meter uses 4-step gradient (`--c-err` → amber → `--c-warn` → `--c-ok`) | Strength meter requires intermediate colours not in the base scale; amber intermediary is `#F59E0B` (amber-500 from standard Tailwind ramp, adjacent to `--c-warn`) |
| SCR-004 | "Taken" slot uses custom inline `opacity: 0.65` | Slot disabled state needs visual differentiation beyond `--ct-d` alone |
| SCR-006 | Waitlist card header uses a soft `--c-ai-bg` gradient sweep | Intended to visually connect waitlist concept to AI slot matching; matches AI palette |
| SCR-014 | Code suggestion row borders: accepted=`--c-ok`, modified=`--cb`, rejected=transparent + opacity | Three-state row distinction requires three different border signals not fully covered by the two-state default |
| SCR-015 | Role badges: `--c-err` for Admin, `--c-ok` for Staff, `--cb` for Patient | Functional distinction only; chosen to match semantic meaning of each role's elevation/access level |

---

## Checklist: Token Coverage

| Token | Used in wireframes | Documented above |
|---|---|---|
| `--cb`, `--cb-h`, `--cb-l` | ✓ (all 16) | ✓ |
| `--cs`, `--cs-m`, `--cs-s` | ✓ (all 16) | ✓ |
| `--cbord`, `--cbord-f` | ✓ (all 16) | ✓ |
| `--ct`, `--ct-s`, `--ct-d`, `--ct-on` | ✓ (all 16) | ✓ |
| `--c-ok`, `--c-ok-bg` | ✓ (SCR-002,006,009,011,014,015,016) | ✓ |
| `--c-warn`, `--c-warn-bg` | ✓ (SCR-004,011,012,016) | ✓ |
| `--c-err`, `--c-err-bg` | ✓ (SCR-001,004,010,013,014,015) | ✓ |
| `--c-ai`, `--c-ai-bg` | ✓ (SCR-003,006,007,008,009,013,014) | ✓ |
| `--s1` through `--s12` | ✓ (all 16) | ✓ |
| `--t-xs` through `--t-2xl` | ✓ (all 16) | ✓ |
| `--r-sm` through `--r-full` | ✓ (all 16) | ✓ |
| `--sh-sm` through `--sh-lg` | ✓ (all 16) | ✓ |
| `--nav-h`, `--max-w` | ✓ (all 16) | ✓ |
| `--sidebar-w` | Declared but unused | Noted (future) |
