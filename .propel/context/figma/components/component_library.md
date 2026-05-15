# Component Library

> **Project:** Unified Patient Access & Clinical Intelligence Platform (UPACIP)
> **Source:** `figma_spec.md §10`, `designsystem.md §5`
> **Platform:** Responsive Web (React 18 SPA)
> **Version:** 1.0
> **Date:** 2026-05-14
> **Naming convention:** `C/<Category>/<Name>`

---

## Actions

### C/Actions/Button

**Figma Name:** `C/Actions/Button`
**Sizes:** S (32px) · M (40px) · L (48px)
**Variants:** Primary · Secondary · Ghost · Danger

#### Variant × State Matrix

| Variant | Default | Hover | Focus | Active | Disabled | Loading |
|---------|---------|-------|-------|--------|----------|---------|
| Primary | Blue bg, white text | Darker blue bg | 3px outline | Scale 0.98 | 40% opacity | Inline spinner |
| Secondary | White bg, border, dark text | Gray-50 bg | 3px outline | Scale 0.98 | 40% opacity | Inline spinner |
| Ghost | Transparent bg, blue text | Blue-50 bg | 3px outline | Scale 0.98 | 40% opacity | — |
| Danger | Red bg, white text | Darker red bg | 3px outline | Scale 0.98 | 40% opacity | Inline spinner |

#### Token Map — Button/Primary/M/Default

```yaml
bg:             color-brand-primary       # #1A56DB
bg-hover:       color-brand-primary-hover # #1446BE
text:           color-text-on-primary     # #FFFFFF
border:         transparent
border-radius:  radius-sm                 # 4px
padding-x:      space-4                   # 16px
padding-y:      space-2                   # 8px
font:           body-md / weight 600
min-height-M:   40px
touch-target:   ≥44px height at M+ (UXR-301)
focus-ring:     3px solid color-border-focus (#1A56DB), 2px offset
```

#### Token Map — Button/Danger/M/Default

```yaml
bg:             color-danger              # #DC2626
bg-hover:       red-700                   # #B91C1C
text:           color-text-on-primary     # #FFFFFF
border-radius:  radius-sm
```

**Screens:** All 16 screens
**Usage note:** Loading state renders `C/Feedback/Spinner` inline; preserves button dimensions.

---

### C/Actions/Link

**Variants:** Default · Visited · Disabled

| Property | Value |
|----------|-------|
| color | `color-brand-primary (#1A56DB)` |
| color-hover | `color-brand-primary-hover (#1446BE)` |
| text-decoration | `underline` on hover/focus |
| focus-ring | 3px solid `#1A56DB`, 2px offset |

**Screens:** SCR-001, SCR-002

---

### C/Actions/IconButton

**Variants:** Ghost · Subtle
**Sizes:** S (32px) · M (40px)

```yaml
bg:             transparent
bg-hover:       color-surface-subtle      # #F1F5F9
icon-size:      20px (outlined, 1.5px stroke)
border-radius:  radius-sm
```

**Screens:** SCR-003, SCR-011, SCR-014

---

## Inputs

### C/Inputs/TextField

**Figma Name:** `C/Inputs/TextField`
**Variants:** Default · Focused · Error · Disabled · PHI

#### Token Map — Default

```yaml
bg:             color-surface-default     # #FFFFFF
border:         color-border-default      # #E2E8F0
border-focus:   color-border-focus        # #1A56DB
border-error:   color-danger              # #DC2626
border-radius:  radius-sm                 # 4px
text:           color-text-primary        # #0F172A
placeholder:    color-text-disabled       # #94A3B8
label-font:     label                     # 12px uppercase 600
input-font:     body-md                   # 14px 400
height-M:       40px
padding-x:      space-3                   # 12px
```

#### Token Map — PHI Variant

```yaml
bg:             color-surface-phi         # #EFF6FF
border:         blue-100                  # #DBEAFE
prefix-icon:    🔒 (outlined, 20×20, 1.5px stroke)
prefix-color:   color-brand-primary       # #1A56DB
aria-label:     includes "Protected health information"
note:           UXR-402 — PHI field visual distinction
```

**Screens:** SCR-001, SCR-002, SCR-004, SCR-007 (PHI), SCR-008 (PHI), SCR-009 (read), SCR-012, SCR-015
**ARIA:** `<label htmlFor>` + `aria-describedby` for errors (UXR-203, UXR-204)

---

### C/Inputs/PasswordField

**Variants:** Default · Revealed · Error

| Feature | Spec |
|---------|------|
| Show/hide toggle | Button; toggles `input type` between `password` / `text` |
| Strength meter (SCR-002) | 4-level: Weak (red) → Fair (amber) → Good (blue) → Strong (green) |
| Strength font | `caption` / `color-text-secondary` |
| Strength bar tokens | `color-danger`, `color-warning`, `color-brand-primary`, `color-success` |
| aria-label on toggle | `"Show password"` / `"Hide password"` |

**Screens:** SCR-001, SCR-002

---

### C/Inputs/SelectField

**Variants:** Default · Focused · Error · Disabled

```yaml
bg:             color-surface-default
border:         color-border-default
border-focus:   color-border-focus
border-radius:  radius-sm
height-M:       40px
chevron-icon:   color-text-secondary
```

**Screens:** SCR-002, SCR-004, SCR-008, SCR-012, SCR-015

---

### C/Inputs/DateTimePicker

**Variants:** Default · Focused · Error

```yaml
bg:             color-surface-default
border:         color-border-default
calendar-icon:  color-text-secondary
date-selected:  color-brand-primary (bg) / color-text-on-primary (text)
```

**Screens:** SCR-008, SCR-012

---

### C/Inputs/FileUpload

**Variants:** Idle · Hover · Uploading · Success · Error

| State | Visual |
|-------|--------|
| Idle | Dashed border `color-border-default`; upload icon + "Drag PDF here or browse" |
| Hover | `color-brand-primary` dashed border; `color-surface-phi` bg |
| Uploading | Per-file progress bar; cancel button |
| Success | `color-success-bg` bg; ✓ icon |
| Error | `color-danger-bg` bg; error message + inline retry |

**Notes:** PDF only; extraction status badge per document; `UXR-603` Retry extraction CTA on failure.
**Screens:** SCR-010

---

### C/Inputs/Checkbox

**Variants:** Unchecked · Checked · Indeterminate · Disabled

```yaml
border:           color-border-default
checked-bg:       color-brand-primary
check-icon:       color-text-on-primary
border-radius:    radius-xs
size:             16×16px
```

**Screens:** SCR-008

---

### C/Inputs/RadioGroup

**Variants:** Default · Selected · Disabled

```yaml
border:         color-border-default
selected-bg:    color-brand-primary
selected-dot:   color-text-on-primary
size:           16×16px
gap:            space-2 between button and label
```

**Screens:** SCR-008

---

### C/Inputs/Toggle

**Variants:** Off · On

```yaml
track-off-bg:   color-surface-subtle      # #F1F5F9
track-on-bg:    color-brand-primary       # #1A56DB
thumb:          color-text-on-primary / white
border-radius:  radius-full
transition:     duration-fast (150ms)
label:          "Switch to AI" / "Switch to manual form" (UXR-103)
note:           Field data preserved on toggle (UXR-103)
```

**Screens:** SCR-007, SCR-008, SCR-012

---

## Navigation

### C/Navigation/TopNav

**Variants:** PatientPortal · StaffPortal · AdminPortal

| Property | Patient | Staff | Admin |
|----------|---------|-------|-------|
| Logo mark bg | `color-brand-primary (#1A56DB)` | `color-success (#16A34A)` | `color-danger (#DC2626)` |
| Nav items | Dashboard, Book, My Profile, Documents, Calendar | Queue, New Walk-in | Users |
| Role badge | Hidden | "Staff" (success bg) | "Admin" (danger bg) |
| Height | 64px | 64px | 64px |
| Bg | `color-surface-default` | `color-surface-default` | `color-surface-default` |
| Border-bottom | `color-border-default` | `color-border-default` | `color-border-default` |
| Shadow | `shadow-sm` | `shadow-sm` | `shadow-sm` |
| User avatar | 32×32px, circular, `radius-full` | same | same |

**Responsive:** Nav links hidden at ≤640px (hamburger menu).
**Screens:** All 16 screens

---

### C/Navigation/Sidebar

**Variants:** Desktop (persistent) · Tablet (collapsible) · Mobile (hamburger)

```yaml
width-desktop:  240px
bg:             color-surface-default
border-right:   color-border-default
active-item-bg: color-brand-primary-light  # #EFF6FF
active-item-text: color-brand-primary
inactive-text:  color-text-secondary
font:           body-md / weight 500
```

**Screens:** SCR-003, SCR-009, SCR-011, SCR-015

---

### C/Navigation/Tabs

**Variants:** Horizontal · Mobile (select)

```yaml
active-tab:     border-bottom 2px solid color-brand-primary; color-brand-primary
inactive-tab:   color-text-secondary
tab-height:     44px
font:           body-md / weight 600
transition:     duration-fast (150ms)
```

**Screens:** SCR-005, SCR-009

---

### C/Navigation/Breadcrumb

**Variants:** Default

```yaml
font:           caption
link-color:     color-text-secondary
active-color:   color-text-primary
separator:      "/" or chevron icon; color-text-disabled
gap:            space-2
```

**Screens:** SCR-004–010, SCR-012–014

---

## Content

### C/Content/AppointmentCard

**Variants:** Default · Compact · Preview
**States:** Default · Loading (skeleton) · Empty

```yaml
bg:             color-surface-default
border:         color-border-default
border-radius:  radius-md
shadow:         shadow-sm
padding:        space-4
font-heading:   heading-sm
font-body:      body-md
status-badge:   C/Content/Badge (variant per appointment status)
```

**Screens:** SCR-003, SCR-004, SCR-005, SCR-006

---

### C/Content/SlotGrid

**Figma Name:** `C/Content/SlotGrid`
**Variants:** Available · Selected · Unavailable · Preferred
**States:** Default · Loading · Error · Empty

#### Cell Token Maps

| Variant | bg | border | text | cursor | Notes |
|---------|-----|--------|------|--------|-------|
| Available | `color-surface-default` | `color-border-default` | `color-text-primary` | default | Base available state |
| Selected | `color-brand-primary` | `color-brand-primary` | `color-text-on-primary` | pointer | Transition ≤200ms (UXR-501) |
| Unavailable | `color-surface-subtle` | `color-border-default` | `color-text-disabled` | not-allowed | Taken or blocked |
| Preferred | `color-ai-accent-bg` | `color-border-ai` (dashed) | `color-text-primary` | pointer | Waitlist preferred slot |

**Cell height:** 48px · **Border-radius:** `radius-md`
**Responsive columns:** 4 (≥1280px) · 2 (768px) · 1 (<640px) — UXR-302
**Screens:** SCR-004, SCR-012

---

### C/Content/DataTable

**Variants:** Default · Loading · Empty
**Features:** Sortable columns · Sticky header · Row hover · Pagination (>20 rows)

```yaml
thead-bg:       color-surface-muted
thead-font:     label (uppercase 12px 600)
tbody-font:     body-md
row-hover-bg:   color-surface-subtle
border:         color-border-default between rows
sort-icon:      color-text-secondary → color-brand-primary (active)
pagination-font: caption
```

**ARIA:** `<table>` with `<th scope>` headings; no div tables.
**Screens:** SCR-010, SCR-011, SCR-014, SCR-015

---

### C/Content/ProfileSection

**Figma Name:** `C/Content/ProfileSection`
**Variants:** Default · AI-attributed · Collapsed
**States:** Default · Expanded · Collapsed · Loading

#### Token Map — AI-attributed (UXR-403)

```yaml
border-left:        color-border-ai       # #7C3AED
border-left-width:  4px
header-bg:          color-ai-accent-bg    # #EDE9FE
badge:              C/Content/Badge/AI
chevron:            color-text-secondary; rotates on toggle (duration-fast)
aria:               aria-expanded on toggle button
```

**Screens:** SCR-009, SCR-013

---

### C/Content/ConflictAlert

**Figma Name:** `C/Content/ConflictAlert`
**Variants:** Critical · High
**States:** Default · Resolved · Reviewed

| Variant | bg | border-left-color | heading-text |
|---------|----|-------------------|--------------|
| Critical | `color-danger-bg (#FEE2E2)` | `color-conflict-critical (#DC2626)` | `color-danger` |
| High | `color-warning-bg (#FEF3C7)` | `color-conflict-high (#D97706)` | `color-warning` |

```yaml
border-left-width:  4px
border-radius:      radius-md
padding:            space-4
body-text:          color-text-primary
source-doc-font:    caption / color-text-secondary
cta:                C/Actions/Button/Ghost
uxr:                UXR-404
```

**Screens:** SCR-013

---

### C/Content/CodeSuggestionRow

**Figma Name:** `C/Content/CodeSuggestionRow`
**Variants:** Pending · Accepted · Modified · Rejected
**States:** Default · Hover · Accepted · Modified · Rejected

| Variant | bg | border-left | Decoration |
|---------|----|-------------|------------|
| Pending | `color-surface-default` | none | — |
| Accepted | `color-success-bg (#DCFCE7)` | `3px solid color-success` | Accepted badge |
| Modified | `color-brand-primary-light` | `3px solid color-brand-primary` | Modified badge |
| Rejected | `color-danger-bg` | `3px solid color-danger` | `text-decoration: line-through; opacity: 0.7` |

```yaml
ai-badge:           C/Content/Badge/AI (always shown on suggestions)
confidence-pill:    caption / color-text-secondary bg color-surface-muted
evidence-text:      body-sm / color-text-secondary; left border color-border-ai
actions:            Accept (Button/Ghost), Modify (Button/Secondary), Reject (Button/Danger)
action-gap:         space-2
padding-y:          space-3
uxr:                UXR-107, UXR-403
```

**Screens:** SCR-014

---

### C/Content/Badge

**Figma Name:** `C/Content/Badge`
**Variants:** Primary · Success · Warning · Danger · AI · Default
**Sizes:** S · M

| Variant | bg | text | Notes |
|---------|----|------|-------|
| AI | `color-ai-accent-bg (#EDE9FE)` | `color-ai-accent (#7C3AED)` | UXR-403 — AI-generated content |
| Success (Verified) | `color-success-bg (#DCFCE7)` | `color-success (#16A34A)` | Human-verified content |
| Warning | `color-warning-bg (#FEF3C7)` | `color-warning (#D97706)` | Soft advisory |
| Danger | `color-danger-bg (#FEE2E2)` | `color-danger (#DC2626)` | Error / critical |
| Primary | `color-brand-primary (#1A56DB)` | `color-text-on-primary (#FFFFFF)` | Branding |

```yaml
font:       label (12px uppercase 600)
radius:     radius-full
padding-x:  space-2
padding-y:  2px
```

**Screens:** All screens (context-dependent variants)

---

## Feedback

### C/Feedback/Modal

**Figma Name:** `C/Feedback/Modal`
**Variants:** Confirmation · Info · Error · SessionTimeout · CodeEdit · RoleChange · DeactivateAccount
**States:** Default · Loading · Error

#### Token Map — Confirmation

```yaml
overlay-bg:         rgba(15, 23, 42, 0.50)
bg:                 color-surface-default
border-radius:      radius-lg               # 8px
shadow:             shadow-lg
max-width:          480px
padding:            space-8
heading-font:       heading-md
body-font:          body-md
header-border:      color-border-default
footer-gap:         space-3
animation:          fade + scale; duration-slow (300ms); easing-enter
aria:               role="dialog" aria-modal="true" aria-labelledby
```

#### SessionTimeout Variant — Special Requirements (UXR-205, UXR-503)

```yaml
aria-live:          assertive
countdown-font:     heading-xl / color-danger
stay-cta:           C/Actions/Button/Primary/M
logout-cta:         C/Actions/Button/Ghost/M
trigger:            13-minute inactivity mark (120s countdown)
expiry-action:      redirect to SCR-001
```

**Screens:** SCR-005 (Cancel Confirm), SCR-014 (Code Edit), SCR-015 (Role Change, Deactivate), all-authenticated (Session Timeout)

---

### C/Feedback/Drawer

**Variants:** Reschedule · ConflictDetail · DocumentPreview

```yaml
position:           right edge
width:              480px (desktop) / 100vw (mobile)
bg:                 color-surface-default
border-left:        color-border-default
shadow:             shadow-lg
animation:          slide from right; duration-slow (300ms); easing-enter
z-index:            above content, below modal
overlay:            rgba(15, 23, 42, 0.30) (lighter than modal)
close:              Escape key + ✕ button + overlay click
aria:               role="dialog" aria-modal="true" aria-labelledby
```

**Screens:** SCR-005, SCR-010, SCR-013

---

### C/Feedback/Toast

**Variants:** Success · Warning · Error · Info
**States:** Default · Dismissed

| Variant | bg | border-left-color |
|---------|----|-------------------|
| Success | `color-success-bg (#DCFCE7)` | `color-success (#16A34A)` |
| Warning | `color-warning-bg (#FEF3C7)` | `color-warning (#D97706)` |
| Error | `color-danger-bg (#FEE2E2)` | `color-danger (#DC2626)` |
| Info | `color-brand-primary-light` | `color-brand-primary` |

```yaml
position:           top-right; 16px margin
border-left-width:  4px
border-radius:      radius-md
shadow:             shadow-md
padding:            space-4
max-width:          360px
auto-dismiss:       5s (non-error) / manual dismiss only (error)
z-index:            above content, below modal
animation:          slide + fade; duration-fast (150ms)
aria:               role="alert" aria-live="assertive" (UXR-601)
```

**Screens:** SCR-003, SCR-004, SCR-016

---

### C/Feedback/Alert

**Variants:** Success · Warning · Error · Info

```yaml
bg (Error):     color-danger-bg           # #FEE2E2
border (Error): color-danger              # #DC2626
text:           color-text-primary
icon-color:     matches variant semantic color
border-radius:  radius-sm
padding:        space-3 space-4
display:        non-dismissible (persists until condition clears)
aria:           role="alert"
uxr:            UXR-204 (field-level errors via aria-describedby)
```

**Screens:** SCR-001, SCR-002, SCR-004, SCR-006, SCR-008, SCR-012, SCR-013, SCR-015

---

### C/Feedback/SkeletonLoader

**Variants:** Card · Row · Text

```yaml
base-bg:        color-surface-subtle      # #F1F5F9
shimmer:        linear-gradient(90deg, gray-100 0%, gray-050 50%, gray-100 100%)
animation:      shimmer 1.5s infinite ease-in-out
border-radius:  radius-md
aria:           aria-hidden="true" on skeleton elements; content wrapper retains aria-label
```

**Screens:** SCR-003, SCR-009, SCR-010, SCR-013

---

### C/Feedback/Spinner

**Variants:** Default · Small · Inline

```yaml
size-default:   24px
size-small:     16px
size-inline:    16px
color:          color-brand-primary
animation:      rotate 600ms linear infinite
aria:           aria-label="Loading"
```

**Screens:** SCR-004, SCR-007, all button Loading states

---

### C/Feedback/ProgressBar

**Variants:** Linear · Step

| Variant | Usage | Notes |
|---------|-------|-------|
| Linear | File upload progress; extraction progress | `color-brand-primary` fill on `color-surface-subtle` track |
| Step | AI intake question steps (UXR-502) | Discrete steps; shows "Question N of 8"; fill advances per step |

```yaml
track-bg:       color-surface-subtle      # #F1F5F9
fill-bg:        color-brand-primary       # #1A56DB
height:         4px (linear) / 8px (step)
border-radius:  radius-full
aria:           role="progressbar" aria-valuenow aria-valuemin aria-valuemax aria-label
```

**Screens:** SCR-007, SCR-008, SCR-010

---

## Special Visual Assets

| Asset | Spec | Screens |
|-------|------|---------|
| PHI Lock Icon | 20×20px, outlined, 1.5px stroke, `color-brand-primary` | SCR-004, SCR-007, SCR-008, SCR-009, SCR-013 |
| AI Badge | `C/Content/Badge/AI` — purple pill | SCR-007, SCR-009, SCR-013, SCR-014 |
| Verified Badge | `C/Content/Badge/Success` — "Verified" green pill | SCR-009, SCR-013, SCR-014 |
| Queue Status Icons | 20×20px outlined: Booked, Arrived, Walk-in, Cancelled | SCR-011 |
| Calendar Provider Logos | Google Calendar & Outlook (official 3rd-party assets) | SCR-016 |

---

## Component Usage Rules

1. **PHI exclusivity:** Only `C/Inputs/TextField/PHI` may be used for fields containing PHI. Never use Default variant for PHI fields.
2. **AI/Verified mutual exclusion:** Never render `Badge/AI` and `Badge/Success (Verified)` simultaneously on the same data field (designsystem.md §9 AI Attribution scenario).
3. **ConflictAlert severity taxonomy:** Use `Critical` variant only for `conflict.severity = "CRITICAL"` entries. Use `High` for `severity = "HIGH"`. No cross-use.
4. **Button sizing:** All interactive buttons on mobile (375px) must have effective touch target ≥44×44px. Use M or L size; never S on mobile primary actions.
5. **No hard-coded values:** All component stylesheets must reference tokens exclusively. Zero raw hex, px, or rgba literals outside the primitive table.
6. **Focus rings:** Every interactive element must implement the focus ring token: `3px solid #1A56DB, 2px offset`. No exceptions.
