# Component Inventory

> **Project:** UPACIP Hi-Fi Wireframe Set · 16 screens
> **Source:** `figma_spec.md` + `designsystem.md` tokens applied
> **Date:** 2026-05-20

---

## Summary Table

| Category | Component | Screens used | Reusable | ARIA role |
|---|---|---|---|---|
| Navigation | TopNav | All 16 | ✓ | `navigation` |
| Navigation | Breadcrumb | SCR-003–016 | ✓ | `navigation aria-label="Breadcrumb"` |
| Navigation | MethodSwitch | SCR-007, SCR-008 | ✓ | `group` |
| Layout | PageGrid (2-col) | SCR-003, SCR-004, SCR-009, SCR-016 | ✓ | `main` |
| Form | TextField | SCR-001–008, SCR-012 | ✓ | `textbox` |
| Form | TextField (PHI variant) | SCR-004, SCR-007, SCR-008 | ✓ | `textbox` + `aria-label` PHI note |
| Form | SelectField | SCR-004, SCR-008, SCR-012, SCR-015 | ✓ | `combobox` |
| Form | TextareaField | SCR-007, SCR-008, SCR-013 | ✓ | `textbox` |
| Form | RadioGroup | SCR-008, SCR-013 | ✓ | `radiogroup` |
| Form | CheckboxGroup | SCR-006, SCR-008, SCR-010, SCR-016 | ✓ | `group` |
| Form | PasswordField | SCR-001, SCR-002 | ✓ | `textbox` + toggle button |
| Form | PasswordStrengthMeter | SCR-002 | — | `progressbar` |
| Form | PainScaleSlider | SCR-008 | — | `slider` |
| Feedback | InlineFieldError | SCR-001–008 | ✓ | `alert` via `aria-describedby` |
| Feedback | Toast | SCR-003, SCR-004 | ✓ | `role="alert" aria-live="assertive"` |
| Feedback | AlertStrip | SCR-003 | ✓ | `role="alert"` |
| Feedback | InsuranceStatusBadge | SCR-004 | — | `role="status"` |
| Feedback | ProgressBar (linear) | SCR-007, SCR-008 | ✓ | `progressbar` |
| Feedback | ConfirmationProgressStrip | SCR-004 | — | `role="status"` |
| Feedback | SkeletonLoader | SCR-009 | ✓ | `aria-label` describing content loading |
| Feedback | TypingIndicator | SCR-007 | — | `role="status"` |
| Overlay | Modal (destructive confirm) | SCR-003, SCR-005, SCR-014, SCR-015 | ✓ | `role="dialog" aria-modal="true"` |
| Overlay | Drawer (right-slide) | SCR-005, SCR-013 | ✓ | `role="dialog" aria-modal="true"` |
| Data display | Card | All screens | ✓ | `article` or implicit |
| Data display | StatCard | SCR-003, SCR-011 | ✓ | — |
| Data display | AppointmentCard | SCR-003 | — | `article` |
| Data display | ProfileSection (collapsible) | SCR-009, SCR-013 | ✓ | `aria-expanded` button |
| Data display | DataRow | SCR-005, SCR-009, SCR-013 | ✓ | — |
| Data display | DataTable | SCR-010, SCR-011, SCR-015 | ✓ | `table` + `th scope` |
| Data display | WaitlistBadge | SCR-006 | — | `role="status"` |
| Booking | SlotGrid | SCR-004, SCR-012 | ✓ | `group aria-label` |
| Booking | SlotCell | SCR-004, SCR-012 | ✓ | `button` |
| Booking | BookingSummaryCard | SCR-004 | — | — |
| Intake | ChatBubble (AI) | SCR-007 | — | — |
| Intake | ChatBubble (user) | SCR-007 | — | — |
| Intake | QuickOptionPill | SCR-007 | — | `button` |
| Intake | IntakeSection (collapsible) | SCR-008 | ✓ | `aria-expanded` button |
| Upload | DropZone | SCR-010 | — | `role="button"` |
| Upload | UploadProgressItem | SCR-010 | ✓ | `progressbar` |
| Queue | QueueTable | SCR-011 | — | `table` |
| Queue | QueueFilterBar | SCR-011 | — | `group` |
| Queue | RowActionSet | SCR-011 | ✓ | — |
| Code review | CodeSuggestionRow | SCR-014 | — | — |
| Code review | ConfidenceBar | SCR-014 | — | `aria-label` confidence % |
| Code review | CodeEvidenceSnippet | SCR-014 | — | — |
| Profile | ProfileHeader | SCR-009, SCR-013 | ✓ | — |
| Profile | ConflictAlert | SCR-013 | — | `role="region"` |
| Calendar | ProviderCard | SCR-016 | ✓ | `role="button"` |
| Calendar | ConnectionStatusBar | SCR-016 | — | `role="status"` |
| Calendar | SyncAdvisory | SCR-016 | — | `role="alert"` |

---

## Detailed Component Specs

### TopNav

| Property | Value |
|---|---|
| Height | 64px (`--nav-h`) |
| Background | `#FFFFFF` |
| Border | `1px solid #E2E8F0` |
| Shadow | `0 1px 2px rgba(0,0,0,.05)` |
| Position | `sticky top:0; z-index:100` |
| Logo mark | 32×32px, rounded-md, role-coloured |
| Patient nav items | Dashboard, Book, My Profile, Documents, Calendar |
| Staff nav items | Queue, New Walk-in |
| Admin nav items | Users |
| Signed-in indicator | User avatar (initials), 32×32px circular |
| Role badge | Staff: green; Admin: red |
| Responsive | Nav links hidden at ≤640px |

### SlotGrid / SlotCell

| Property | Value |
|---|---|
| Grid columns | 4-col at ≥640px; 2-col at <640px; 1-col at <375px (UXR-302) |
| Cell min-height | 64px |
| States | `available`, `taken`, `blocked`, `selected`, `preferred-candidate` |
| `available` | White background, `#E2E8F0` border |
| `selected` | `#1A56DB` background, white text, scale(1.02) transform (UXR-501) |
| `taken` | `#F1F5F9` background, disabled, `cursor:not-allowed` |
| `blocked` | Same as taken, opacity 0.6 |
| `preferred-candidate` | `#EDE9FE` background, `#7C3AED` dashed border |
| Selection timing | <200ms visual feedback (UXR-501) |
| Focus | 3px solid `#1A56DB` outline |
| ARIA | `role="group"` on grid, `aria-label` per cell with time + state |

### PHI TextField (UXR-402)

| Property | Value |
|---|---|
| Background | `#EFF6FF` (`--cb-l`) |
| Border | `#BFDBFE` (blue-200) |
| Prefix icon | 🔒 (visual only, `aria-label="Contains protected health information"`) |
| Label prefix | 🔒 in label element |
| Textarea PHI | Same background/border as input |

### AI Badge / Verified Badge (UXR-403)

| Variant | Background | Text color | Border | Content |
|---|---|---|---|---|
| AI | `#EDE9FE` | `#7C3AED` | `1px solid #DDD6FE` | 🤖 AI |
| Verified | `#DCFCE7` | `#16A34A` | none | ✓ Verified |
| Pending | `#FEF3C7` | `#D97706` | none | ⏳ Pending |

### ConflictAlert (UXR-404)

| Severity | Left border | Background | Icon |
|---|---|---|---|
| CRITICAL | 4px solid `#DC2626` | `#FEE2E2` | ⛔ |
| HIGH | 4px solid `#D97706` | `#FEF3C7` | ⚠ |

### Modal

| Property | Value |
|---|---|
| Overlay | `rgba(15,23,42,.5)` |
| Max-width | 480px |
| Padding | 32px |
| Border-radius | 8px (`--r-lg`) |
| ARIA | `role="dialog" aria-modal="true" aria-labelledby` |
| Close triggers | Escape key, Cancel button, overlay click (drawers only) |

### Drawer

| Property | Value |
|---|---|
| Width | 480px (SCR-005), 520px (SCR-013) |
| Mobile | 100vw |
| Transition | `transform .3s cubic-bezier(0,0,.2,1)` |
| Structure | Header + scrollable body + footer (actions) |

---

## Component States Matrix

| Component | Default | Hover | Focus | Active/Selected | Disabled | Error |
|---|---|---|---|---|---|---|
| Button (primary) | Blue bg | Darker blue | 3px outline | Scale + darken | Grey bg, not-allowed | — |
| Button (ghost) | Transparent | Blue-50 bg | 3px outline | — | — | — |
| Button (danger) | Transparent | Red-100 bg | 3px outline | — | — | — |
| TextField | White | — | Blue border + ring | — | Grey bg | Red border |
| PHI TextField | Blue-50 | — | Blue border + ring | — | — | — |
| SlotCell | White | Blue-50 | 3px outline | Blue bg (selected) | Grey bg, not-allowed | — |
| ProfileSection | White | Grey-50 | 3px outline | Open (aria-expanded) | — | — |
| Toggle (method switch) | Grey pill | — | 3px outline | Blue active pill | — | — |

---

## Reusability Analysis

**High reuse across all portals:**
- TopNav (adapted per portal — logo colour, nav items)
- Card + CardHeader shell
- Form fields (TextField, SelectField, TextareaField)
- Buttons (primary, secondary, ghost, danger-outline)
- Badge (AI, Verified, status variants)
- Modal overlay shell
- Breadcrumb

**Portal-specific only:**
- SlotGrid/SlotCell — SCR-004, SCR-012 only
- ConflictAlert — SCR-013 (staff) only
- CodeSuggestionRow — SCR-014 only
- DropZone — SCR-010 only
- ChatBubble — SCR-007 only
- WaitlistBadge — SCR-006 only

---

## Responsive Summary

| Component | Desktop (1440px) | Tablet (768px) | Mobile (640px) | Mobile (375px) |
|---|---|---|---|---|
| TopNav | Full nav | Full nav | Nav hidden | Nav hidden |
| SlotGrid | 4 columns | 4 columns | 2 columns | 1 column |
| PageGrid | 2 columns | 1 column | 1 column | 1 column |
| Drawer | 480–520px | Full width | Full width | Full width |
| FormFieldRow | 2 columns | 2 columns | 1 column | 1 column |
| StatsRow | 4 columns | 2 columns | 2 columns | 1 column |
| Breadcrumb | Full | Full | Full | Full |

---

## Priority Matrix

| Priority | Components |
|---|---|
| P1 (critical path) | TopNav, SlotGrid, BookingSummaryCard, PhiTextField, Modal, Toast |
| P2 (core features) | IntakeSection, ChatBubble, ProfileSection, ConflictAlert, CodeSuggestionRow |
| P3 (supporting) | DropZone, ConnectionStatusBar, SyncAdvisory, QueueFilterBar |
| P4 (enhancement) | SkeletonLoader, TypingIndicator, ConfidenceBar, PainScaleSlider |

---

## A11y Annotations

| Component | Annotation |
|---|---|
| SlotGrid | `role="group"` with `aria-label` specifying the date. Each cell: `aria-label="[time] — [state]"` |
| Modal | `aria-modal="true"`, `aria-labelledby` → h2 id, focus trapped inside |
| PHI fields | Label includes 🔒 text; `aria-label` on icon notes "Contains protected health information" |
| Toast | `role="alert" aria-live="assertive"` — renders to all screen readers immediately |
| SkeletonLoader | `aria-label="[content] loading"` on parent container |
| TypingIndicator | `role="status" aria-label="AI is responding"` |
| ProfileSection toggle | `aria-expanded` updated on every toggle |
| ProgressBar | `role="progressbar" aria-valuenow aria-valuemin aria-valuemax aria-label` |
| DataTable | `<table>` with `<th>` and scope attributes; no divs |

---

## Design System Integration

All components consume design tokens via CSS custom properties sourced from `designsystem.md`:

```css
/* Colour */
--cb: #1A56DB  /* brand-blue */
--c-ai: #7C3AED  /* AI accent purple */
--c-ok: #16A34A  /* success green */
--c-warn: #D97706  /* warning amber */
--c-err: #DC2626  /* error red */

/* Spacing */
--s1: 4px  --s2: 8px  --s3: 12px  --s4: 16px
--s6: 24px  --s8: 32px  --s12: 48px

/* Typography */
--t-xs: 11px  --t-sm: 12px  --t-base: 14px
--t-md: 16px  --t-lg: 18px  --t-xl: 20px  --t-2xl: 24px

/* Shape */
--r-sm: 4px  --r-md: 6px  --r-lg: 8px  --r-full: 9999px

/* Shadow */
--sh-sm  --sh-md  --sh-lg
```

**No external stylesheets.** All tokens are inlined in each HTML file's `<style>` block for zero-dependency standalone deployment.
