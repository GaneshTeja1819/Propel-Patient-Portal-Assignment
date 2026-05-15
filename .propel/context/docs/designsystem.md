---
title: "Design Reference – Unified Patient Access & Clinical Intelligence Platform"
version: "1.0"
date: "2026-05-14"
source: "spec.md v1.0, figma_spec.md v1.0"
status: "Draft"
---

# Design System Reference - Unified Patient Access & Clinical Intelligence Platform

## 1. UI Impact Assessment

### Project UI Context

| Attribute | Value | Source |
|-----------|-------|--------|
| Platform | Web (React 18 SPA) | spec.md BRD |
| Aesthetic Direction | Utilitarian | figma_spec.md §9 [INFERRED] |
| Primary Personas | Patient, Staff, Admin | spec.md §Actors |
| Accessibility Standard | WCAG 2.2 Level AA | UXR-201 through UXR-205 |
| Responsive Breakpoints | 1280px / 768px / 375px | UXR-301 |
| Mobile Phase | Not in Phase 1 scope (TR-009) | design.md |
| PHI Handling | AES-256-GCM at application layer; visual PHI distinction required | design.md DR-001, UXR-402 |
| AI Attribution | AI-generated data visually distinct from human-verified | design.md AIR-004, UXR-403 |

### AI Signal

| Signal | Value | Details |
|--------|-------|---------|
| $AI_SIGNAL | true | AI-generated content in SCR-007 (intake), SCR-013 (profile), SCR-014 (codes) |
| AI Provider | Gemini API (gemini-1.5-pro) | Tool Calling / Structured Output pattern |
| AI UI Requirements | Provenance badges, confidence scores, inline evidence, Accept/Modify/Reject controls | UXR-107, UXR-403 |

---

## 2. Design Source References

### Primary References

| Document | Path | Role |
|----------|------|------|
| Functional Specification | `.propel/context/docs/spec.md` | UC/FR source; screen derivation basis |
| Figma Specification | `.propel/context/docs/figma_spec.md` | SCR-XXX inventory, UXR-XXX, FL-XXX flows, component list |
| Architecture Design | `.propel/context/docs/design.md` | NFRs, DRs, TRs; tech constraints; RBAC requirements |
| UML Models | `.propel/context/docs/models.md` | Sequence diagrams for interaction flow validation |

---

## 3. Screen-to-Design Mappings

### SCR-001: Login

| Attribute | Value |
|-----------|-------|
| Derived From | UC-002, UC-003 |
| Personas | Patient, Staff, Admin |
| States | Default, Loading, Error, Validation |
| UC Design References | UC-002 (Login), UC-003 (Session Timeout) |
| Key FRs | FR-001, FR-002, FR-003, FR-040, FR-041 |
| Design Notes | Generic error message only — no field-level credential disclosure (OWASP A01); PHI not visible on this screen; Timeout modal overlay on all authenticated screens |

### SCR-002: Registration

| Attribute | Value |
|-----------|-------|
| Derived From | UC-001 |
| Personas | Patient |
| States | Default, Loading, Error, Validation |
| UC Design References | UC-001 |
| Key FRs | FR-001, FR-040 |
| Design Notes | Password strength indicator visible inline; email uniqueness check via debounced async validation; no PHI collected at registration |

### SCR-003: Patient Dashboard

| Attribute | Value |
|-----------|-------|
| Derived From | UC-004, UC-005, UC-006, UC-025 |
| Personas | Patient |
| States | Default, Loading, Empty |
| UC Design References | UC-004, UC-005, UC-006, UC-025 |
| Key FRs | FR-004, FR-005, FR-006, FR-007, FR-009 |
| Design Notes | Empty state must provide guided onboarding strip; PDF confirmation badge rendered after booking; calendar sync advisory badge if token expired |

### SCR-004: Appointment Booking

| Attribute | Value |
|-----------|-------|
| Derived From | UC-004, UC-007, UC-018, UC-025 |
| Personas | Patient |
| States | Default, Loading, Error, Validation |
| UC Design References | UC-004, UC-007, UC-018, UC-025 |
| Key FRs | FR-005, FR-006, FR-007, FR-008, FR-009, FR-010, FR-011, FR-029 |
| Design Notes | Slot grid uses Redis-backed real-time data (UXR-102); optimistic UI ≤200ms (UXR-501); insurance inline badge (UXR-602); preferred slot CTA in slot grid; slot race error recovery (UXR-601) |

### SCR-005: Appointment Detail

| Attribute | Value |
|-----------|-------|
| Derived From | UC-005, UC-006 |
| Personas | Patient |
| States | Default, Loading, Error |
| UC Design References | UC-005, UC-006 |
| Key FRs | FR-006, FR-007 |
| Design Notes | Cancel modal requires explicit second confirmation step; reschedule flow in drawer (no full page navigate); atomic swap with optimistic lock |

### SCR-006: Preferred Slot Confirmation

| Attribute | Value |
|-----------|-------|
| Derived From | UC-007 |
| Personas | Patient |
| States | Default, Empty |
| UC Design References | UC-007 |
| Key FRs | FR-010, FR-011, FR-012, FR-013 |
| Design Notes | Waitlist position and slot preference shown; notification preference advisory |

### SCR-007: AI Conversational Intake

| Attribute | Value |
|-----------|-------|
| Derived From | UC-009, UC-011 |
| Personas | Patient |
| States | Default, Loading, Error |
| UC Design References | UC-009, UC-011 |
| Key FRs | FR-015, FR-016, FR-017 |
| Design Notes | Discrete question-step UI with ProgressBar (UXR-502); PHI fields annotated (UXR-402); "Switch to manual form" toggle persistent throughout session (UXR-103); field data must survive switch |

### SCR-008: Manual Intake Form

| Attribute | Value |
|-----------|-------|
| Derived From | UC-010, UC-011 |
| Personas | Patient |
| States | Default, Loading, Error, Validation |
| UC Design References | UC-010, UC-011 |
| Key FRs | FR-015, FR-016, FR-017 |
| Design Notes | All required fields marked; PHI fields annotated (UXR-402); "Switch to AI" toggle persistent; field data preserved on switch (UXR-103); ARIA labels on all fields (UXR-203) |

### SCR-009: 360° Patient Profile (Patient View)

| Attribute | Value |
|-----------|-------|
| Derived From | UC-020 |
| Personas | Patient |
| States | Default, Loading, Empty |
| UC Design References | UC-020 |
| Key FRs | FR-033, FR-034, FR-035, FR-036 |
| Design Notes | Read-only; collapsible ProfileSection components; AI-generated vs human-verified visual distinction (UXR-403); PHI lock-icon annotation (UXR-402); "Profile generation in progress" skeleton when extraction pending |

### SCR-010: Document Upload

| Attribute | Value |
|-----------|-------|
| Derived From | UC-019 |
| Personas | Patient |
| States | Default, Loading, Error |
| UC Design References | UC-019 |
| Key FRs | FR-030, FR-031 |
| Design Notes | FileUpload: PDF only; extraction status badge per document; "Retry extraction" CTA on failure (UXR-603); document list as DataTable |

### SCR-011: Staff Dashboard / Same-Day Queue

| Attribute | Value |
|-----------|-------|
| Derived From | UC-014, UC-015, UC-016 |
| Personas | Staff |
| States | Default, Loading, Empty |
| UC Design References | UC-014, UC-015, UC-016 |
| Key FRs | FR-022, FR-023, FR-024, FR-025, FR-026 |
| Design Notes | RBAC-protected (Staff role only); status badges per queue entry; "Mark Arrived" action; queue reorder + remove with audit logging; ≤1280px full queue visible without scroll |

### SCR-012: Staff Walk-in Booking

| Attribute | Value |
|-----------|-------|
| Derived From | UC-014 |
| Personas | Staff |
| States | Default, Loading, Error, Validation |
| UC Design References | UC-014 |
| Key FRs | FR-022, FR-023 |
| Design Notes | RBAC-protected (Staff role only); optional patient account creation inline; "Add to queue without slot" option when no slots available |

### SCR-013: 360° Patient Profile (Staff View)

| Attribute | Value |
|-----------|-------|
| Derived From | UC-020, UC-022 |
| Personas | Staff |
| States | Default, Loading, Empty, Error |
| UC Design References | UC-020, UC-022 |
| Key FRs | FR-033, FR-034, FR-035, FR-036 |
| Design Notes | Staff can view full clinical profile; ConflictAlert component for each conflict (UXR-404); severity-colour taxonomy (Critical=danger-red, High=warning-amber); Conflict Detail Drawer; AI-generated vs human-verified (UXR-403); PHI annotation (UXR-402) |

### SCR-014: Medical Code Verification

| Attribute | Value |
|-----------|-------|
| Derived From | UC-023, UC-024 |
| Personas | Staff |
| States | Default, Loading, Empty, Error |
| UC Design References | UC-023, UC-024 |
| Key FRs | FR-037, FR-038, FR-039 |
| Design Notes | RBAC-protected (Staff only); CodeSuggestionRow per code; confidence % + evidence snippet visible inline (UXR-107); AI provenance badge required (UXR-403); Accept/Modify/Reject per row; all decisions audit-logged |

### SCR-015: Admin User Management

| Attribute | Value |
|-----------|-------|
| Derived From | UC-017 |
| Personas | Admin |
| States | Default, Loading, Empty, Error, Validation |
| UC Design References | UC-017 |
| Key FRs | FR-027, FR-028, FR-040, FR-041 |
| Design Notes | RBAC-protected (Admin only); inline DataTable; search/filter; role change confirm modal; deactivation confirm modal; self-deactivation blocked with inline error |

### SCR-016: Calendar OAuth Consent

| Attribute | Value |
|-----------|-------|
| Derived From | UC-013 |
| Personas | Patient |
| States | Default, Loading, Error |
| UC Design References | UC-013 |
| Key FRs | FR-020, FR-021 |
| Design Notes | Provider selector (Google / Outlook); OAuth redirect to external provider; non-blocking failure advisory (UXR-604); retry link on error |

---

## 4. Design Tokens

### 4.1 Color Tokens

#### Primitive Palette

```yaml
color:
  # Brand
  blue-900: "#0C2F6F"
  blue-700: "#1A56DB"
  blue-600: "#1446BE"
  blue-100: "#DBEAFE"
  blue-050: "#EFF6FF"

  # Greens
  green-700: "#15803D"
  green-600: "#16A34A"
  green-100: "#DCFCE7"

  # Ambers
  amber-700: "#B45309"
  amber-600: "#D97706"
  amber-100: "#FEF3C7"

  # Reds
  red-700: "#B91C1C"
  red-600: "#DC2626"
  red-100: "#FEE2E2"

  # Purples (AI-reserved)
  purple-700: "#6D28D9"
  purple-600: "#7C3AED"
  purple-100: "#EDE9FE"

  # Grays
  gray-950: "#0F172A"
  gray-800: "#1E293B"
  gray-700: "#334155"
  gray-600: "#475569"
  gray-400: "#94A3B8"
  gray-200: "#E2E8F0"
  gray-100: "#F1F5F9"
  gray-050: "#F8FAFC"

  # Base
  white: "#FFFFFF"
  black: "#000000"
```

#### Semantic Aliases

```yaml
color-alias:
  # Brand
  color-brand-primary:          "color.blue-700"        # #1A56DB
  color-brand-primary-hover:    "color.blue-600"        # #1446BE
  color-brand-primary-light:    "color.blue-050"        # #EFF6FF

  # Surfaces
  color-surface-default:        "color.white"           # #FFFFFF
  color-surface-muted:          "color.gray-050"        # #F8FAFC
  color-surface-subtle:         "color.gray-100"        # #F1F5F9
  color-surface-phi:            "color.blue-050"        # #EFF6FF  — PHI field bg
  color-surface-ai:             "color.purple-100"      # #EDE9FE  — AI content bg

  # Borders
  color-border-default:         "color.gray-200"        # #E2E8F0
  color-border-focus:           "color.blue-700"        # #1A56DB
  color-border-ai:              "color.purple-600"      # #7C3AED

  # Text
  color-text-primary:           "color.gray-950"        # #0F172A
  color-text-secondary:         "color.gray-600"        # #475569
  color-text-disabled:          "color.gray-400"        # #94A3B8
  color-text-on-primary:        "color.white"           # #FFFFFF  — text on brand buttons
  color-text-inverse:           "color.white"

  # Semantic: Status
  color-success:                "color.green-600"       # #16A34A
  color-success-bg:             "color.green-100"       # #DCFCE7
  color-warning:                "color.amber-600"       # #D97706
  color-warning-bg:             "color.amber-100"       # #FEF3C7
  color-danger:                 "color.red-600"         # #DC2626
  color-danger-bg:              "color.red-100"         # #FEE2E2

  # Semantic: AI & Conflicts
  color-ai-accent:              "color.purple-600"      # #7C3AED  — AI-generated content accent
  color-ai-accent-bg:           "color.purple-100"      # #EDE9FE
  color-conflict-critical:      "color.red-600"         # #DC2626
  color-conflict-high:          "color.amber-600"       # #D97706

  # Interactive
  color-interactive-default:    "color.blue-700"
  color-interactive-hover:      "color.blue-600"
  color-interactive-disabled:   "color.gray-400"
```

### 4.2 Typography

```yaml
typography:
  font-family-default: >
    system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif

  scale:
    heading-xl:
      size: "24px"
      line-height: "1.3"
      weight: "600"
      tracking: "-0.01em"
    heading-lg:
      size: "20px"
      line-height: "1.3"
      weight: "600"
      tracking: "-0.01em"
    heading-md:
      size: "18px"
      line-height: "1.4"
      weight: "600"
      tracking: "0"
    heading-sm:
      size: "16px"
      line-height: "1.4"
      weight: "600"
      tracking: "0"
    body-md:
      size: "14px"
      line-height: "1.6"
      weight: "400"
      tracking: "0"
    body-sm:
      size: "13px"
      line-height: "1.6"
      weight: "400"
      tracking: "0"
    caption:
      size: "12px"
      line-height: "1.5"
      weight: "400"
      tracking: "0.01em"
    label:
      size: "12px"
      line-height: "1.4"
      weight: "600"
      tracking: "0.05em"
      text-transform: "uppercase"
```

### 4.3 Spacing

```yaml
spacing:
  space-1:  "4px"
  space-2:  "8px"
  space-3:  "12px"
  space-4:  "16px"
  space-6:  "24px"
  space-8:  "32px"
  space-12: "48px"
  space-16: "64px"
```

### 4.4 Border Radius

```yaml
radius:
  radius-xs:  "2px"   # badges
  radius-sm:  "4px"   # inputs, buttons
  radius-md:  "6px"   # cards, panels
  radius-lg:  "8px"   # modals, drawers
  radius-full: "9999px" # pill badges, toggle
```

### 4.5 Elevation / Shadows

```yaml
elevation:
  shadow-none: "none"
  shadow-sm: "0 1px 2px 0 rgba(0, 0, 0, 0.05)"
  shadow-md: "0 4px 6px -1px rgba(0, 0, 0, 0.07), 0 2px 4px -2px rgba(0, 0, 0, 0.05)"
  shadow-lg: "0 10px 15px -3px rgba(0, 0, 0, 0.10), 0 4px 6px -4px rgba(0, 0, 0, 0.05)"
```

### 4.6 Grid & Breakpoints

```yaml
grid:
  breakpoint-xs:    "375px"   # mobile (secondary scope, Phase 1)
  breakpoint-sm:    "640px"   # slot-grid reflow threshold (UXR-302)
  breakpoint-md:    "768px"   # tablet (primary scope)
  breakpoint-lg:    "1280px"  # desktop (primary scope)

  desktop:
    columns: 12
    gutter:  "24px"
    margin:  "32px"
    max-width: "1280px"
  tablet:
    columns: 8
    gutter:  "16px"
    margin:  "24px"
  mobile:
    columns: 4
    gutter:  "12px"
    margin:  "16px"
```

### 4.7 Motion / Transitions

```yaml
motion:
  duration-instant: "100ms"
  duration-fast:    "150ms"    # Button state transitions
  duration-normal:  "200ms"    # Optimistic slot selection (UXR-501)
  duration-slow:    "300ms"    # Modal/drawer open
  easing-default:   "cubic-bezier(0.4, 0, 0.2, 1)"    # ease-in-out
  easing-enter:     "cubic-bezier(0.0, 0, 0.2, 1)"    # ease-out
  easing-exit:      "cubic-bezier(0.4, 0, 1, 1)"      # ease-in
```

### 4.8 Focus Ring (Accessibility)

```yaml
focus:
  outline-width:  "3px"
  outline-offset: "2px"
  outline-color:  "color.blue-700"    # #1A56DB — WCAG 2.2 SC 2.4.11 compliant
  outline-style:  "solid"
```

---

## 5. Component References

### 5.1 Button

| Property | Value |
|----------|-------|
| Figma Name | `C/Actions/Button` |
| Variants | Primary, Secondary, Ghost, Danger |
| Sizes | S (32px), M (40px), L (48px) |
| States | Default, Hover, Focus, Active, Disabled, Loading |
| Design Notes | Loading state renders inline Spinner; touch target ≥44px (all sizes at M+); Danger variant: `color-danger` bg, `color-text-on-primary` text |

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
```

#### Token Map — Button/Danger/M/Default

```yaml
bg:             color-danger              # #DC2626
bg-hover:       color.red-700            # #B91C1C
text:           color-text-on-primary    # #FFFFFF
border:         transparent
border-radius:  radius-sm
```

---

### 5.2 TextField

| Property | Value |
|----------|-------|
| Figma Name | `C/Inputs/TextField` |
| Variants | Default, Focused, Error, Disabled, PHI |
| States | Default, Focused, Error, Disabled |
| Design Notes | PHI variant adds `🔒` prefix icon and `color-surface-phi` background |

#### Token Map — TextField/Default

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
height-M:       "40px"
padding-x:      space-3                   # 12px
```

#### Token Map — TextField/PHI variant

```yaml
bg:             color-surface-phi         # #EFF6FF
border:         color.blue-100            # #DBEAFE
prefix-icon:    "🔒"
prefix-color:   color-brand-primary       # #1A56DB
```

---

### 5.3 SlotGrid

| Property | Value |
|----------|-------|
| Figma Name | `C/Content/SlotGrid` |
| Variants | Available, Selected, Unavailable, Preferred |
| States | Default, Loading, Error, Empty |
| Design Notes | Grid columns: 4 (≥1280px), 2 (768px), 1 (<640px) per UXR-302; Selected state animates within 200ms (UXR-501) |

#### Token Map — SlotGrid/Cell/Available

```yaml
bg:             color-surface-default
border:         color-border-default      # #E2E8F0
border-radius:  radius-md                 # 6px
text:           color-text-primary
height:         "48px"
font:           body-md
```

#### Token Map — SlotGrid/Cell/Selected

```yaml
bg:             color-brand-primary       # #1A56DB
border:         color-brand-primary
text:           color-text-on-primary     # #FFFFFF
transition:     motion.duration-normal    # 200ms
```

#### Token Map — SlotGrid/Cell/Unavailable

```yaml
bg:             color-surface-subtle      # #F1F5F9
border:         color-border-default
text:           color-text-disabled       # #94A3B8
cursor:         "not-allowed"
```

---

### 5.4 ConflictAlert

| Property | Value |
|----------|-------|
| Figma Name | `C/Content/ConflictAlert` |
| Variants | Critical, High |
| States | Default, Resolved, Reviewed |
| Design Notes | Left border width 4px; severity determined by conflict severity field; source doc reference displayed in card body (UXR-404) |

#### Token Map — ConflictAlert/Critical

```yaml
bg:             color-danger-bg           # #FEE2E2
border-left:    color-conflict-critical   # #DC2626
border-left-width: "4px"
heading-text:   color-danger              # #DC2626
body-text:      color-text-primary
border-radius:  radius-md
padding:        space-4                   # 16px
```

#### Token Map — ConflictAlert/High

```yaml
bg:             color-warning-bg          # #FEF3C7
border-left:    color-conflict-high       # #D97706
border-left-width: "4px"
heading-text:   color-warning
```

---

### 5.5 CodeSuggestionRow

| Property | Value |
|----------|-------|
| Figma Name | `C/Content/CodeSuggestionRow` |
| Variants | Pending, Accepted, Modified, Rejected |
| States | Default, Hover, Accepted, Modified, Rejected |
| Design Notes | Row shows: code, description, confidence % pill, evidence snippet, AI badge, Accept/Modify/Reject controls (UXR-107, UXR-403) |

#### Token Map — CodeSuggestionRow/Pending/Default

```yaml
bg:             color-surface-default
border-bottom:  color-border-default      # #E2E8F0
ai-badge-bg:    color-ai-accent-bg        # #EDE9FE
ai-badge-text:  color-ai-accent           # #7C3AED
confidence-pill-bg: color-surface-muted
confidence-pill-text: color-text-secondary
evidence-text:  color-text-secondary
font:           body-md
padding-y:      space-3                   # 12px
```

#### Token Map — CodeSuggestionRow/Accepted

```yaml
bg:             color-success-bg          # #DCFCE7
border-left:    color-success             # #16A34A
border-left-width: "3px"
badge-bg:       color-success-bg
badge-text:     color-success
```

#### Token Map — CodeSuggestionRow/Rejected

```yaml
bg:             color-danger-bg           # #FEE2E2
border-left:    color-danger              # #DC2626
border-left-width: "3px"
text-decoration: "line-through"
opacity:        "0.7"
```

---

### 5.6 Badge

| Property | Value |
|----------|-------|
| Figma Name | `C/Content/Badge` |
| Variants | Primary, Success, Warning, Danger, AI, Default |
| Sizes | S, M |
| Design Notes | Pill shape (radius-full); uppercase label font; inline use in DataTable rows, ProfileSection, queue entries |

#### Token Map — Badge/AI

```yaml
bg:     color-ai-accent-bg               # #EDE9FE
text:   color-ai-accent                  # #7C3AED
font:   label                            # 12px uppercase 600
radius: radius-full
px:     space-2                          # 8px
py:     "2px"
```

#### Token Map — Badge/Success (human-verified)

```yaml
bg:     color-success-bg                 # #DCFCE7
text:   color-success                    # #16A34A
```

#### Token Map — Badge/Warning

```yaml
bg:     color-warning-bg                 # #FEF3C7
text:   color-warning                    # #D97706
```

#### Token Map — Badge/Danger

```yaml
bg:     color-danger-bg                  # #FEE2E2
text:   color-danger                     # #DC2626
```

---

### 5.7 Modal

| Property | Value |
|----------|-------|
| Figma Name | `C/Feedback/Modal` |
| Variants | Confirmation, Info, Error, SessionTimeout |
| States | Default, Loading, Error |
| Design Notes | Overlay: rgba(15, 23, 42, 0.5) — gray-950 at 50%; modal max-width 480px desktop; 100vw mobile; z-index above all content; ARIA role="dialog" aria-modal="true" aria-labelledby |

#### Token Map — Modal/Confirmation

```yaml
overlay-bg:         "rgba(15, 23, 42, 0.50)"
bg:                 color-surface-default     # #FFFFFF
border-radius:      radius-lg                 # 8px
shadow:             shadow-lg
max-width:          "480px"
padding:            space-8                   # 32px
heading-font:       heading-md
body-font:          body-md
header-border:      color-border-default
footer-gap:         space-3                   # 12px between buttons
```

#### SessionTimeout Modal — Special Requirements

```yaml
# Must announce via aria-live="assertive" (UXR-205)
aria-live:          "assertive"
countdown-font:     heading-xl / color-danger
stay-cta:           Button/Primary/M
```

---

### 5.8 Toast

| Property | Value |
|----------|-------|
| Figma Name | `C/Feedback/Toast` |
| Variants | Success, Warning, Error, Info |
| States | Default, Dismissed |
| Design Notes | Position: top-right, 16px margin; auto-dismiss after 5s (error toasts persist until dismissed); max-width 360px; z-index above content, below modal |

#### Token Map — Toast/Success

```yaml
bg:         color-success-bg             # #DCFCE7
border-left: color-success               # #16A34A
border-left-width: "4px"
text:       color-text-primary
icon-color: color-success
border-radius: radius-md
shadow:     shadow-md
padding:    space-4                      # 16px
```

---

### 5.9 Alert / Inline

| Property | Value |
|----------|-------|
| Figma Name | `C/Feedback/Alert` |
| Variants | Success, Warning, Error, Info |
| States | Default |
| Design Notes | Used for inline field-level or section-level feedback; not dismissible (persists until condition resolved); ARIA role="alert" |

#### Token Map — Alert/Error

```yaml
bg:         color-danger-bg              # #FEE2E2
border:     color-danger                 # #DC2626
text:       color-text-primary
icon-color: color-danger
border-radius: radius-sm
padding:    space-3 space-4              # 12px 16px
```

---

### 5.10 SkeletonLoader

| Property | Value |
|----------|-------|
| Figma Name | `C/Feedback/SkeletonLoader` |
| Variants | Card, Row, Text |
| States | Loading |
| Design Notes | Animated shimmer gradient using `color-surface-subtle` → `color-surface-muted`; ARIA hidden="true"; actual content element retains aria-label |

```yaml
base-bg:    color-surface-subtle          # #F1F5F9
shimmer-bg: "linear-gradient(90deg, color.gray-100 0%, color.gray-050 50%, color.gray-100 100%)"
animation:  "shimmer 1.5s infinite"
border-radius: radius-md
```

---

### 5.11 ProfileSection

| Property | Value |
|----------|-------|
| Figma Name | `C/Content/ProfileSection` |
| Variants | Default, AI-attributed, Collapsed |
| States | Default, Expanded, Collapsed, Loading |
| Design Notes | Collapsible via chevron toggle; AI-attributed variant: `color-border-ai` left border; PHI fields inside section use PHI TextField variant |

#### Token Map — ProfileSection/AI-attributed

```yaml
border-left:        color-border-ai           # #7C3AED
border-left-width:  "4px"
header-bg:          color-ai-accent-bg        # #EDE9FE
badge:              Badge/AI
```

---

## 6. New Visual Assets

| Asset | Description | Usage Screen(s) |
|-------|-------------|-----------------|
| PHI Lock Icon | 20×20px outlined lock icon (1.5px stroke) | SCR-004, SCR-007, SCR-008, SCR-009, SCR-013 |
| AI Badge | `C/Content/Badge/AI` — "AI" pill in purple | SCR-007, SCR-009, SCR-013, SCR-014 |
| Verified Badge | `C/Content/Badge/Success` — "Verified" pill in green | SCR-009, SCR-013, SCR-014 |
| Queue Status Icons | 20×20px: Booked, Arrived, Walk-in, Cancelled | SCR-011 |
| Calendar Provider Logos | Google Calendar and Outlook logos (3rd-party, official assets) | SCR-016 |
| Empty State Illustrations | Minimal line illustrations for no-data states (optional; defer to content-only empty states if not available in Phase 1) | SCR-003, SCR-006, SCR-009, SCR-011 |

---

## 7. Task Design Mapping

| Task Area | Target Screens | Required Tokens | Component Dependencies |
|-----------|---------------|-----------------|------------------------|
| Authentication UI | SCR-001, SCR-002 | color-brand-primary, color-danger, focus ring | Button, TextField, PasswordField, Alert |
| Booking & Slot UI | SCR-004, SCR-005, SCR-006 | color-brand-primary, color-surface-phi, SlotGrid tokens | SlotGrid, AppointmentCard, Badge, Modal, Drawer, Toast |
| Intake Forms | SCR-007, SCR-008 | color-surface-phi, color-ai-accent, ProgressBar | TextField/PHI, Toggle, ProgressBar, AI/Verified Badge |
| Clinical Profile | SCR-009, SCR-013 | color-ai-accent, color-border-ai, conflict tokens | ProfileSection, ConflictAlert, Badge/AI, Badge/Verified |
| Code Verification | SCR-014 | color-ai-accent, color-success, color-danger | CodeSuggestionRow, Badge/AI, Modal/CodeEdit |
| Staff Queue | SCR-011, SCR-012 | color-surface-default, status badge tokens | DataTable, Badge/Status, Button/Primary, Button/Secondary |
| Admin Users | SCR-015 | color-danger, color-warning | DataTable, Badge, Modal/Confirmation, Button/Danger |
| Calendar OAuth | SCR-016 | color-warning, color-border-default | Button/Primary, Alert/Warning, Badge |

---

## 8. Visual Validation Criteria

### Per-Screen Validation

| Screen | Key Visual Assertion | Validator |
|--------|---------------------|-----------|
| All screens | No hard-coded hex/px values in any component | Token audit |
| All screens | Focus ring ≥3px, ≥3:1 contrast vs adjacent surface | axe-core + manual |
| All screens | Text contrast ≥4.5:1 | axe-core |
| SCR-004 | Slot selection transition ≤200ms | Performance timer |
| SCR-007 | ProgressBar advances per question step | Functional test |
| SCR-007, SCR-008 | PHI lock icon present on all PHI fields | Visual inspection |
| SCR-009, SCR-013, SCR-014 | AI Badge present on all AI-generated content | Visual inspection |
| SCR-013 | ConflictAlert uses severity colour taxonomy | Visual inspection |
| SCR-014 | All CodeSuggestionRow entries show confidence %, evidence, AI badge | Visual inspection |

---

## 9. Implementation Scenarios

### Scenario: AI Attribution Rendering

When rendering any data field sourced from Gemini AI extraction (ExtractedClinicalData entity):

1. Apply `color-border-ai` left border to parent ProfileSection or CodeSuggestionRow.
2. Render `Badge/AI` ("AI") component adjacent to the field label.
3. If the field has been manually verified by Staff: replace AI badge with `Badge/Success` ("Verified") and remove AI border.
4. Never mix AI and Verified badges on the same field.

### Scenario: PHI Field Rendering

When rendering a field containing PHI (as defined in design.md DR-001):

1. Apply `color-surface-phi` background token.
2. Render `🔒` lock icon prefix (PHI TextField variant).
3. Apply `color.blue-100` border color.
4. Ensure that field label text remains `color-text-primary` for accessibility contrast.

### Scenario: Session Timeout Modal

When inactivity timer reaches 120 seconds remaining:

1. Render `C/Feedback/Modal/SessionTimeout` with `aria-live="assertive"`.
2. Countdown timer renders in `heading-xl` / `color-danger`.
3. "Stay logged in" CTA = `C/Actions/Button/Primary/M`.
4. "Log out" CTA = `C/Actions/Button/Ghost/M`.
5. On countdown expiry: clear session, redirect to SCR-001.

### Scenario: Slot Race Condition

On receiving 409 Conflict response during slot booking:

1. Render `Toast/Error` — "That slot is no longer available."
2. Remove optimistic "Selected" state from clicked slot cell.
3. Trigger slot grid refresh (re-fetch from API).
4. Re-render grid with updated availability; all cells return to default state.

---

## 10. Accessibility Requirements

### Keyboard Navigation

| Component | Expected Keyboard Behaviour |
|-----------|----------------------------|
| Button | Tab focuses; Enter/Space activates |
| TextField | Tab focuses; type to enter value |
| SlotGrid cells | Tab to cell; Space/Enter selects |
| DataTable rows | Tab to row; Enter expands inline action |
| Modal | On open: focus traps inside; Escape closes; Tab cycles within modal |
| Drawer | On open: focus traps inside; Escape closes; Tab cycles within drawer |
| Tabs | Tab to tab bar; Left/Right arrow navigates tabs |

### ARIA Requirements

| Screen | ARIA Notes |
|--------|-----------|
| All forms | `<label htmlFor="...">` for all inputs; `aria-describedby` for error messages (UXR-204) |
| All loading states | SkeletonLoader: `aria-hidden="true"`; container: `aria-busy="true"` |
| Modal | `role="dialog"`, `aria-modal="true"`, `aria-labelledby` (modal title ID) |
| Toast | `role="status"` (Success/Info/Warning); `role="alert"` (Error) |
| Session Timeout Modal | `aria-live="assertive"` on countdown element (UXR-205) |
| ConflictAlert | `role="alert"` on Critical severity alerts |
| ProgressBar | `role="progressbar"`, `aria-valuenow`, `aria-valuemin="0"`, `aria-valuemax="100"` |

---

## 11. Design Review Checklist

### Pre-Handoff

- [ ] All 16 screens delivered with required states per figma_spec.md §6
- [ ] All 26 UXRs (UXR-101 through UXR-604) verified against screen designs
- [ ] All 11 prototype flows (FL-001 through FL-011) navigable
- [ ] Color tokens match YAML definition in §4.1 (no deviations)
- [ ] PHI TextField variant applied on all PHI fields (UXR-402)
- [ ] AI Badge / Verified Badge applied correctly on SCR-009, SCR-013, SCR-014 (UXR-403)
- [ ] ConflictAlert severity taxonomy applied on SCR-013 (UXR-404)
- [ ] CodeSuggestionRow renders confidence %, evidence, and AI badge on SCR-014 (UXR-107)
- [ ] Session Timeout Modal includes aria-live="assertive" (UXR-205)
- [ ] All interactive elements have focus ring (3px, ≥3:1 contrast)
- [ ] Export manifest (figma_spec.md §12) complete — 53 JPGs
- [ ] `UPACIP__<Platform>__<Screen>__<State>__v1.jpg` naming applied to all exports
