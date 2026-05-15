---
title: "Figma Design Specification – Unified Patient Access & Clinical Intelligence Platform"
version: "1.0"
date: "2026-05-14"
source: "spec.md v1.0"
status: "Draft"
---

# Figma Design Specification - Unified Patient Access & Clinical Intelligence Platform

## 1. Figma Specification

**Platform**: Responsive Web (React 18 SPA; primary breakpoints: 1280px desktop, 768px tablet, 375px mobile)

---

## 2. Source References

### Primary Source

| Document | Path | Purpose |
|----------|------|---------|
| Requirements Specification | `.propel/context/docs/spec.md` | Personas, use cases (UC-001–UC-025), functional requirements (FR-001–FR-041) with UI impact |

### Optional Sources

| Document | Path | Purpose |
|----------|------|---------|
| Architecture Design | `.propel/context/docs/design.md` | Technology constraints (React 18, .NET 8, HIPAA, RBAC), domain entities |
| UML Models | `.propel/context/docs/models.md` | Sequence diagrams for flow validation |

### Related Documents

| Document | Path | Purpose |
|----------|------|---------|
| Design System | `.propel/context/docs/designsystem.md` | Tokens, branding, component specifications |

---

## 3. UX Requirements

### UXR Requirements Table

| UXR-ID | Category | Requirement | Acceptance Criteria | Screens Affected | Basis |
|--------|----------|-------------|---------------------|------------------|-------|
| UXR-101 | Usability | [SOURCE:INFERRED] System MUST allow authenticated Patients to complete an appointment booking in no more than 3 primary interactions from their dashboard. | Booking flow start-to-confirm requires ≤3 screen transitions; tested via task analysis. | SCR-003, SCR-004 | Derived from UC-004 success scenario; 3-click booking is a clinically-scheduling UX benchmark. |
| UXR-102 | Usability | [SOURCE:INPUT] System MUST display real-time slot availability that updates without a full page reload when slot state changes. | Slot grid reflects server state within ≤5s (per Redis TTL); no full page navigation required. | SCR-004 | Directly from FR-008 — real-time slot display requirement. |
| UXR-103 | Usability | [SOURCE:INPUT] System MUST preserve all intake fields entered in the current method when the patient switches between AI and manual intake. | After switching, all previously entered fields are pre-populated in the target form/chat; no data loss observable. | SCR-007, SCR-008 | Directly from FR-017, UC-011 — switch without data loss. |
| UXR-104 | Usability | [SOURCE:INPUT] System MUST provide Staff with a single-page same-day queue dashboard showing all appointments in chronological order with arrival status. | All today's appointments visible without scroll on 1280px viewport; status badges visible for each entry. | SCR-011 | Directly from FR-024, UC-015 — manage same-day queue. |
| UXR-105 | Usability | [SOURCE:INPUT] System MUST allow Admin to search, view, edit, deactivate, and change the role of any user account within a single cohesive workflow. | User search → select → action requires ≤2 screen transitions; no separate page per action. | SCR-015 | Directly from FR-027, FR-028, UC-017. |
| UXR-106 | Usability | [SOURCE:INFERRED] System MUST render the 360° patient profile in a scannable information hierarchy with collapsible sections for vitals, medications, history, and conflicts. | Users locate any patient data category within 5 seconds in usability test; conflict section visible above fold when conflicts > 0. | SCR-009, SCR-013 | Derived from FR-036, UC-020 — unified profile view; clinical data scan speed benchmark. |
| UXR-107 | Usability | [SOURCE:INPUT] System MUST display AI code suggestions alongside confidence scores and supporting clinical evidence for each ICD-10 and CPT entry on the verification screen. | Every suggestion row shows: code, description, confidence percentage, and evidence snippet; Accept/Modify/Reject controls visible inline. | SCR-014 | Directly from FR-037, FR-038, UC-024. |
| UXR-201 | Accessibility | [SOURCE:EXTERNAL] System MUST maintain WCAG 2.2 Level AA color contrast ratios across all text and interactive UI elements. | Text contrast ≥4.5:1 on normal text, ≥3:1 on large text and UI components; verified by automated axe scan. | All screens | WCAG 2.2 Success Criterion 1.4.3 and 1.4.11. |
| UXR-202 | Accessibility | [SOURCE:EXTERNAL] System MUST ensure all interactive elements are operable via keyboard navigation with a visible focus indicator. | Tab order is logical; focus ring is ≥3px offset, ≥3:1 contrast against adjacent surface; all CTAs reachable without mouse. | All screens | WCAG 2.2 SC 2.1.1, 2.4.7, 2.4.11 (Focus Appearance). |
| UXR-203 | Accessibility | [SOURCE:EXTERNAL] System MUST provide visible labels and ARIA attributes for all form fields, with label–input association via htmlFor/id pairs. | axe-core reports zero "label" violations; screen reader announces field label and type on focus. | SCR-001, SCR-002, SCR-004, SCR-007, SCR-008, SCR-010, SCR-012, SCR-015 | WCAG 2.2 SC 1.3.1, 3.3.2. |
| UXR-204 | Accessibility | [SOURCE:EXTERNAL] System MUST associate inline validation error messages with their parent form fields via aria-describedby. | Screen reader announces field error message on focus after failed validation; axe scan reports zero aria-describedby violations. | SCR-001, SCR-002, SCR-004, SCR-008, SCR-012, SCR-015 | WCAG 2.2 SC 3.3.1, 3.3.3. |
| UXR-205 | Accessibility | [SOURCE:INFERRED] System MUST announce the session-timeout countdown modal to screen readers via a live region before the session expires. | aria-live="assertive" on timeout modal; screen reader announces "Session expiring in 2 minutes" without requiring keyboard focus. | All authenticated screens | Derived from FR-003, UC-003; WCAG 2.2 SC 4.1.3 (Status Messages). |
| UXR-301 | Responsiveness | [SOURCE:INFERRED] System MUST deliver a usable layout at 1280px (desktop), 768px (tablet), and 375px (mobile) breakpoints. | No horizontal scroll at any defined breakpoint; touch targets ≥44×44px at 375px; content readable without zoom. | All screens | Derived from platform specification (React 18 SPA targeting web browsers); responsive design best practice. |
| UXR-302 | Responsiveness | [SOURCE:INFERRED] System MUST reflow the slot picker grid from multi-column to a single-column scrollable list at viewport widths below 640px. | At 375px: slot grid renders as single-column; at 768px: renders as 2-column; at 1280px: renders as 4-column. | SCR-004 | Derived from FR-005, UC-004 — slot selection on mobile devices; responsive grid pattern. |
| UXR-401 | Visual Design | [SOURCE:INFERRED] System MUST apply only design token values (from designsystem.md) for all colours, spacing, typography, radius, and elevation; no hard-coded CSS values are permitted. | Automated token audit reports zero raw hex/pixel values outside the primitive table in any component stylesheet. | All screens | Derived from design system governance; rules/figma-design-standards.md. |
| UXR-402 | Visual Design | [SOURCE:INFERRED] System MUST visually distinguish PHI-containing fields from non-PHI fields using a consistent lock-icon annotation and muted background token. | PHI fields render with `🔒` icon prefix and `surface-phi` background token; Staff and Admin can identify PHI fields without referencing documentation. | SCR-004, SCR-007, SCR-008, SCR-009, SCR-013 | Derived from FR-040, DR-001 — PHI encryption and HIPAA minimum-necessary. |
| UXR-403 | Visual Design | [SOURCE:INFERRED] System MUST visually differentiate AI-generated data from human-verified data using the `ai-accent` token and a provenance badge. | AI-generated content renders with left border in `color-ai-accent` (#7C3AED) and "AI" badge; human-verified content renders with `color-success` border and "Verified" badge. | SCR-009, SCR-013, SCR-014 | Derived from AIR-004 (Trust-First AI, human-in-the-loop) and design.md AI architecture goals. |
| UXR-404 | Visual Design | [SOURCE:INPUT] System MUST render data conflict alerts using a severity-colour taxonomy (Critical=danger-red, High=warning-amber) with source document references visible. | Conflict cards use `color-danger` for Critical severity and `color-warning` for High; source document name and date displayed in conflict body. | SCR-013 | Directly from FR-035, UC-022 — conflict highlighting. |
| UXR-501 | Interaction | [SOURCE:INFERRED] System MUST provide optimistic UI feedback within 200ms of slot selection, displaying a "Selected" state before the server booking response arrives. | Slot card transitions to "Selected" visual state within 200ms of click; spinner appears within 200ms of form submission. | SCR-004 | Derived from FR-005, FR-008, UC-004; interaction response time standard (200ms threshold). |
| UXR-502 | Interaction | [SOURCE:INFERRED] System MUST render each AI intake question as a discrete step with a visible progress indicator showing completion percentage. | Progress bar advances per question; estimated completion displayed; current question number shown (e.g., "Question 3 of 8"). | SCR-007 | Derived from UC-009 — AI conversational intake; step-based UX pattern for multi-step forms. |
| UXR-503 | Interaction | [SOURCE:INPUT] System MUST display a session-timeout countdown modal at 2 minutes before automatic logout, allowing the patient to extend the session. | Modal appears at exactly 13-minute inactivity mark; countdown from 120 seconds visible; "Stay logged in" CTA resets session timer; modal closes on confirmation. | All authenticated screens | Directly from FR-003, UC-003 — 15-minute session timeout. |
| UXR-504 | Interaction | [SOURCE:INFERRED] System MUST display an inline progress indicator during PDF appointment confirmation generation and email dispatch. | "Generating confirmation..." spinner appears immediately after booking confirmation; replaced by "Confirmation emailed ✓" within the Hangfire job completion window. | SCR-003, SCR-004 | Derived from FR-009, UC-025 — PDF generation latency expectation. |
| UXR-601 | Error Handling | [SOURCE:INPUT] System MUST display a slot conflict error message with an automatically refreshed slot grid when a double-booking race condition occurs. | On 409 response: error toast "Slot no longer available" appears; slot grid reloads within 2s; previously selected slot shown as unavailable. | SCR-004 | Directly from UC-004 Extension 3a — slot race condition recovery. |
| UXR-602 | Error Handling | [SOURCE:INPUT] System MUST display insurance soft-validation results inline beside the insurance fields without blocking form submission. | Validated = green badge; Not Recognised = amber advisory; booking CTA remains enabled in all validation states. | SCR-004 | Directly from FR-029, UC-018 — non-blocking soft validation. |
| UXR-603 | Error Handling | [SOURCE:INFERRED] System MUST surface an actionable fallback CTA when AI clinical data extraction fails after maximum retry attempts. | Document upload screen or profile screen displays "Extraction failed — contact staff or retry" with retry button; document remains stored. | SCR-010, SCR-013 | Derived from UC-019 Extension 5a, UC-021 Extension — AI extraction failure. |
| UXR-604 | Error Handling | [SOURCE:INFERRED] System MUST communicate calendar sync failures as non-intrusive notifications that do not block or obscure the booking confirmation. | Calendar sync failure renders as dismissible amber Toast (not modal); booking confirmation remains fully visible. | SCR-004, SCR-003 | Derived from UC-013 Extension 2a–4a — calendar API failure non-blocking. |

### UXR Categories

- **Usability (UXR-1XX)**: Navigation depth, task efficiency, discoverability (UXR-101–UXR-107)
- **Accessibility (UXR-2XX)**: WCAG 2.2 AA compliance, keyboard navigation, ARIA (UXR-201–UXR-205)
- **Responsiveness (UXR-3XX)**: Breakpoint behavior, viewport adaptation (UXR-301–UXR-302)
- **Visual Design (UXR-4XX)**: Token governance, PHI distinction, AI vs human provenance (UXR-401–UXR-404)
- **Interaction (UXR-5XX)**: Feedback timing, loading states, session management (UXR-501–UXR-504)
- **Error Handling (UXR-6XX)**: Recovery paths, inline feedback, non-blocking errors (UXR-601–UXR-604)

---

## 4. Personas Summary

| Persona | Role | Primary Goals | Key Screens |
|---------|------|---------------|-------------|
| Patient | Authenticated end-user | Book / cancel / reschedule appointments; complete intake; upload documents; view own profile | SCR-001, SCR-002, SCR-003, SCR-004, SCR-005, SCR-006, SCR-007, SCR-008, SCR-009, SCR-010, SCR-016 |
| Staff | Front-desk / clinical staff | Manage walk-ins and queue; mark arrivals; review patient profiles; verify medical codes | SCR-001, SCR-011, SCR-012, SCR-013, SCR-014 |
| Admin | Administrative user | Create, update, deactivate, and re-role user accounts | SCR-001, SCR-015 |

---

## 5. Information Architecture

### Site Map

```text
Unified Patient Access & Clinical Intelligence Platform
+-- Public
|   +-- Login (SCR-001)
|   +-- Registration (SCR-002)
+-- Patient Portal (authenticated — role: Patient)
|   +-- Dashboard (SCR-003)
|   +-- Book Appointment (SCR-004)
|   |   +-- Preferred Slot Confirmation (SCR-006)
|   +-- Appointment Detail (SCR-005)
|   +-- Intake
|   |   +-- AI Intake (SCR-007)
|   |   +-- Manual Intake (SCR-008)
|   +-- My Profile — 360° View (SCR-009)
|   +-- Upload Document (SCR-010)
|   +-- Calendar Setup (SCR-016)
+-- Staff Portal (authenticated — role: Staff)
|   +-- Staff Dashboard / Queue (SCR-011)
|   +-- Walk-in Booking (SCR-012)
|   +-- Patient Profile (Staff View) (SCR-013)
|   +-- Medical Code Verification (SCR-014)
+-- Admin Portal (authenticated — role: Admin)
    +-- User Management (SCR-015)
```

### Navigation Patterns

| Pattern | Type | Platform Behavior |
|---------|------|-------------------|
| Primary Nav | Top Header + role-specific sidebar | Desktop (≥1280px): persistent left sidebar; Tablet (768px): collapsible sidebar; Mobile: hamburger menu |
| Secondary Nav | Tabs (within Profile, Booking) | Horizontal tab bar — collapses to select on mobile |
| Utility Nav | User menu (top-right) | Dropdown: account settings, session info, logout |
| Breadcrumb | Hierarchical wayfinding | Visible on all screens ≥2 levels deep; hidden on dashboard |

---

## 6. Screen Inventory

### Screen List

| Screen ID | Screen Name | Derived From | Personas Covered | States Required |
|-----------|-------------|--------------|------------------|-----------------|
| SCR-001 | Login | UC-002, UC-003 | Patient, Staff, Admin | Default, Loading, Error, Validation |
| SCR-002 | Registration | UC-001 | Patient | Default, Loading, Error, Validation |
| SCR-003 | Patient Dashboard | UC-004, UC-005, UC-006, UC-025 | Patient | Default, Loading, Empty |
| SCR-004 | Appointment Booking | UC-004, UC-007, UC-018, UC-025 | Patient | Default, Loading, Error, Validation |
| SCR-005 | Appointment Detail (Cancel / Reschedule) | UC-005, UC-006 | Patient | Default, Loading, Error |
| SCR-006 | Preferred Slot Confirmation | UC-007 | Patient | Default, Empty |
| SCR-007 | AI Conversational Intake | UC-009, UC-011 | Patient | Default, Loading, Error |
| SCR-008 | Manual Intake Form | UC-010, UC-011 | Patient | Default, Loading, Error, Validation |
| SCR-009 | 360° Patient Profile (Patient View) | UC-020 | Patient | Default, Loading, Empty |
| SCR-010 | Document Upload | UC-019 | Patient | Default, Loading, Error |
| SCR-011 | Staff Dashboard / Same-Day Queue | UC-014, UC-015, UC-016 | Staff | Default, Loading, Empty |
| SCR-012 | Staff Walk-in Booking | UC-014 | Staff | Default, Loading, Error, Validation |
| SCR-013 | 360° Patient Profile (Staff View) | UC-020, UC-022 | Staff | Default, Loading, Empty, Error |
| SCR-014 | Medical Code Verification | UC-023, UC-024 | Staff | Default, Loading, Empty, Error |
| SCR-015 | Admin User Management | UC-017 | Admin | Default, Loading, Empty, Error, Validation |
| SCR-016 | Calendar OAuth Consent | UC-013 | Patient | Default, Loading, Error |

### Screen-to-Persona Coverage Matrix

| Screen | Patient | Staff | Admin | Notes |
|--------|---------|-------|-------|-------|
| SCR-001 Login | Primary | Primary | Primary | Entry point for all users |
| SCR-002 Registration | Primary | — | — | Patient self-registration |
| SCR-003 Patient Dashboard | Primary | — | — | Patient landing post-login |
| SCR-004 Appointment Booking | Primary | — | — | Core patient booking flow |
| SCR-005 Appointment Detail | Primary | — | — | Cancel / reschedule |
| SCR-006 Preferred Slot Confirmation | Primary | — | — | Post-booking waitlist |
| SCR-007 AI Intake | Primary | — | — | AI-path intake |
| SCR-008 Manual Intake | Primary | — | — | Manual-path intake |
| SCR-009 360° Profile (Patient) | Primary | — | — | Read-only self profile |
| SCR-010 Document Upload | Primary | — | — | PDF clinical document upload |
| SCR-011 Staff Queue Dashboard | — | Primary | — | Same-day queue management |
| SCR-012 Walk-in Booking | — | Primary | — | Staff-created appointments |
| SCR-013 360° Profile (Staff) | — | Primary | — | Clinical review + conflicts |
| SCR-014 Code Verification | — | Primary | — | AI code review gate |
| SCR-015 Admin User Management | — | — | Primary | User account management |
| SCR-016 Calendar OAuth | Primary | — | — | Google/Outlook consent |

### Modal / Overlay Inventory

| Name | Type | Trigger | Parent Screen(s) |
|------|------|---------|------------------|
| Cancel Confirmation Dialog | Modal | "Cancel appointment" click | SCR-005 |
| Reschedule Slot Picker | Drawer | "Reschedule" click | SCR-005 |
| Session Timeout Countdown | Modal | 13-minute inactivity | All authenticated |
| Code Modification Input | Modal | "Modify" click on code row | SCR-014 |
| User Role Change Confirm | Modal | Role change action | SCR-015 |
| Deactivate Account Confirm | Modal | "Deactivate" action | SCR-015 |
| Conflict Detail Drawer | Drawer | Conflict card click | SCR-013 |
| Document Preview | Drawer | Document row click | SCR-010, SCR-013 |

---

## 7. Content & Tone

### Voice & Tone

- **Overall Tone**: Professional and clear with clinical precision; warm but not playful.
- **Error Messages**: Specific and actionable — "That slot is no longer available. Here are the next available times." Not: "Error 409."
- **Empty States**: Guiding with a clear next step — "No appointments yet. Book your first appointment." With a visible CTA.
- **Success Messages**: Brief and confirmatory — "Appointment booked. Confirmation emailed." Not verbose celebration.
- **AI Attribution**: Transparent — "Suggested by AI (Gemini). Review before accepting."

### Content Guidelines

- **Headings**: Sentence case throughout; title case reserved for proper nouns only.
- **CTAs**: Specific verbs — "Book appointment", "Upload document", "Verify codes". Not generic "Submit" or "OK".
- **Labels**: Concise and descriptive — "Date of birth" not "DOB". "Insurance provider name" not "Insurance".
- **Placeholder Text**: Helpful examples — e.g., `e.g. BlueCross BlueShield` for insurance name field.

---

## 8. Data & Edge Cases

### Data Scenarios

| Scenario | Description | Handling |
|----------|-------------|----------|
| No appointments | Patient has no upcoming bookings | SCR-003: Empty state with "Book appointment" CTA |
| New patient | Registered but no intake/documents | SCR-003: Guided onboarding strip with incomplete tasks |
| No documents | Patient profile has no clinical documents | SCR-009: Empty state — "Upload your first document to generate your profile" with upload CTA |
| Profile not generated | Documents uploaded but extraction not complete | SCR-009: "Profile generation in progress" skeleton + timestamp |
| Large queue | >20 same-day patients | SCR-011: Paginated or virtualised list; sticky queue summary bar |
| Slow connection | API latency >3s | All data screens: SkeletonLoader replaces content; retry button after 10s |
| Offline | No network | Toast: "You appear to be offline. Changes will sync when reconnected." |

### Edge Cases

| Case | Screen(s) Affected | Solution |
|------|-------------------|----------|
| Long patient name | SCR-011, SCR-013 | Truncate at 30 chars with full name tooltip on hover/focus |
| Long insurance provider name | SCR-004 | Truncate in field badge; full value in tooltip |
| PDF upload > size limit | SCR-010 | Inline error before upload attempt: "File must be under [X]MB" |
| AI intake field unmapped on switch | SCR-007, SCR-008 | Unmapped fields shown blank with `[!] Please fill in this field` annotation |
| Session timeout mid-booking form | SCR-004 | Non-PHI form state saved to sessionStorage; restored after re-login |
| Zero AI code suggestions | SCR-014 | Empty state: "No AI suggestions — please code manually" with manual entry CTA |
| Calendar OAuth token expired | SCR-016, SCR-003 | Amber badge "Calendar sync requires reconnection" with re-auth link |

---

## 9. Branding & Visual Direction

*See `designsystem.md` for all design tokens (colors, typography, spacing, shadows, etc.)*

### Aesthetic Direction

- **Direction**: Utilitarian `[SOURCE:INFERRED]`
- **Rationale**: Healthcare scheduling platforms serve anxious patients and efficiency-driven clinical staff simultaneously. Decoration competes with clinical information hierarchy. The "Trust-First AI" design principle requires that AI-generated content be visibly attributed and distinguishable from human-verified data — this demands a precise, information-dense layout that utilitarian aesthetics excel at. Warmth is introduced through typography and subtle surface-muted colour rather than decorative motifs or gradients.
- **Precedents**: Epic Systems MyChart, One Medical patient portal, Linear (productivity SaaS)
- **Anti-brief**: This product must NOT resemble consumer wellness apps (pastel gradients, rounded illustration styles, playful micro-copy). It must NOT look like a generic bootstrap admin template (generic blue primary, flat grey sidebar). It must NOT use a sole-typeface system lock-in or purple-to-blue linear gradients.
- **Basis**: Direction derived from clinical domain, Trust-First AI principle (design.md Architecture Goal 3), and three-persona analysis; no explicit brand guidelines exist in input, so direction is inferred.

### Branding Assets

- **Logo**: Placeholder — project asset (not defined in spec); use initial logotype during design phase.
- **Icon Style**: Outlined (20px, 1.5px stroke weight) — precision over decoration.
- **Illustration Style**: None — data-dense clinical UI; no spot illustrations.
- **Photography Style**: Not applicable — no photography in core UI.

---

## 10. Component Specifications

*Full component tokens and specifications defined in `designsystem.md`. Screen-level requirements listed below.*

### Component Library Reference

**Source**: `.propel/context/docs/designsystem.md` — Component Specifications section

### Required Components per Screen

| Screen ID | Components Required | Notes |
|-----------|---------------------|-------|
| SCR-001 | TextField (1), PasswordField (1), Button/Primary (1), Button/Ghost (1), Link (1), Alert/Inline (1) | Login form with error state |
| SCR-002 | TextField (2), PasswordField (1), SelectField (1), Button/Primary (1), Button/Ghost (1), Alert/Inline (1) | Registration form |
| SCR-003 | AppointmentCard (N), Badge (N), Button/Primary (1), Button/Secondary (N), Toast (1), SkeletonLoader | Patient dashboard; N = dynamic |
| SCR-004 | SlotGrid (1), AppointmentCard-preview (1), TextField (2), Badge (1), Button/Primary (1), Button/Secondary (1), Alert/Inline (1), Toast (1), Spinner (1) | Booking + insurance soft-validate |
| SCR-005 | AppointmentCard (1), Button/Danger (1), Button/Secondary (1), Modal/Confirmation (1), Drawer/Reschedule (1) | Detail + cancel/reschedule |
| SCR-006 | AppointmentCard (1), Badge (1), Alert/Inline (1) | Waitlist confirmation |
| SCR-007 | ChatBubble pattern, TextField (1), Button/Primary (1), ProgressBar (1), Toggle (1), Spinner (1) | AI intake conversation UI |
| SCR-008 | TextField (N), SelectField (N), DateTimePicker (1), Checkbox (N), Button/Primary (1), Button/Secondary (1), Alert/Inline (N) | Manual intake form |
| SCR-009 | ProfileSection (4), Badge (N), Alert/Inline (1), Button/Secondary (1), SkeletonLoader | Patient self-view (read-only) |
| SCR-010 | FileUpload (1), DataTable (1), Badge (N), Button/Primary (1), Alert/Inline (1), SkeletonLoader | Document upload + list |
| SCR-011 | DataTable (1), Badge (N), Button/Primary (1), Button/Secondary (N), Badge/Status (N), Breadcrumb (1) | Staff queue table |
| SCR-012 | TextField (N), SelectField (2), DateTimePicker (1), SlotGrid (1), Button/Primary (1), Alert/Inline (1), Toggle (1) | Walk-in booking form |
| SCR-013 | ProfileSection (4), ConflictAlert (N), Drawer/ConflictDetail (1), Badge (N), Button/Primary (N), Alert/Inline (1) | Staff profile + conflicts |
| SCR-014 | CodeSuggestionRow (N), DataTable (1), Badge (N), Button/Primary (N), Button/Secondary (N), Button/Danger (N), Modal/CodeEdit (1) | Code verification list |
| SCR-015 | DataTable (1), TextField (1), SelectField (1), Badge (N), Button/Primary (1), Button/Danger (1), Modal/Confirm (2) | User management table + actions |
| SCR-016 | Button/Primary (1), Button/Ghost (1), Alert/Inline (1), Badge (1) | OAuth consent + provider select |

### Component Summary

| Category | Components | Variants |
|----------|------------|----------|
| Actions | Button, Link, IconButton | Primary / Secondary / Ghost / Danger × S/M/L × Default/Hover/Focus/Active/Disabled/Loading |
| Inputs | TextField, PasswordField, SelectField, DateTimePicker, FileUpload, Checkbox, RadioGroup, Toggle | States: Default / Focused / Error / Disabled; PHI variant for TextField |
| Navigation | TopNav / Header, Sidebar, Tabs, Breadcrumb | Desktop / Tablet / Mobile responsive variants |
| Content | AppointmentCard, SlotGrid, DataTable, ProfileSection, ConflictAlert, CodeSuggestionRow, Badge | Content variants; AI/Verified/Conflict state variants |
| Feedback | Modal, Drawer, Toast, Alert/Inline, SkeletonLoader, Spinner, ProgressBar | Success / Warning / Danger / Info types |

### Component Constraints

- Use only components from `designsystem.md`.
- No custom components without specification in this document.
- All components support states: Default, Hover, Focus, Active, Disabled, Loading.
- Naming convention: `C/<Category>/<Name>` — e.g., `C/Actions/Button`, `C/Content/ConflictAlert`.

---

## 11. Prototype Flows

### Flow: FL-001 — Patient Registration & Login

**Flow ID**: FL-001
**Derived From**: UC-001, UC-002, UC-003
**Personas Covered**: Patient, Staff, Admin
**Description**: New user registers or existing user authenticates; session timeout handled.

#### Flow Sequence

```text
1. Entry: SCR-001 Login / Default
   - Trigger: User navigates to platform URL
   |
   v
2. Decision Point:
   +-- New user -> SCR-002 Registration / Default
   |     - User enters email, password, profile fields
   |     - On success -> SCR-001 Login / Default (redirect with success toast)
   |     - On error -> SCR-002 / Validation (inline field errors)
   +-- Existing user -> stays on SCR-001
   |
   v
3. SCR-001 Login / Default
   - User submits credentials
   |
   v
4. Decision Point:
   +-- Credentials valid -> Role-appropriate landing screen
   |     - Patient -> SCR-003 Patient Dashboard / Default
   |     - Staff -> SCR-011 Staff Queue Dashboard / Default
   |     - Admin -> SCR-015 Admin User Management / Default
   +-- Invalid credentials -> SCR-001 / Error (generic error toast)
   +-- Account locked -> SCR-001 / Error (lock message + email notice)
   |
   v
5. Exit: Role-appropriate dashboard / Default
```

#### Required Interactions

- Input validation: email format check, password complexity on Registration.
- Generic "invalid credentials" message — no field-level disclosure on Login.
- Session timeout: modal appears at 13-minute mark → "Stay logged in" resets timer → SCR-001 on expiry.

---

### Flow: FL-002 — Appointment Booking with Preferred Slot

**Flow ID**: FL-002
**Derived From**: UC-004, UC-007, UC-018, UC-025
**Personas Covered**: Patient
**Description**: Patient selects an available slot, optionally registers a preferred slot, completes insurance validation, and receives PDF confirmation.

#### Flow Sequence

```text
1. Entry: SCR-003 Patient Dashboard / Default
   - Trigger: Patient clicks "Book appointment" CTA
   |
   v
2. SCR-004 Appointment Booking / Default
   - Slot grid displays real-time available slots (Redis-backed, TTL ≤5s)
   - Patient selects a slot (optimistic 200ms highlight)
   - Patient optionally selects a preferred unavailable slot
   - Patient enters insurance name + ID
   |
   v
3. Insurance soft-validation inline (UXR-602)
   - Validated: green badge
   - Not Recognised: amber advisory (non-blocking)
   |
   v
4. Patient confirms booking
   |
   v
5. Decision Point:
   +-- Slot still available -> Booking created; PDF gen job enqueued
   |     - SCR-003 / Default (confirmation toast + "Confirmation emailed" badge)
   |     - If preferred slot registered -> SCR-006 / Default
   +-- Slot taken (race) -> SCR-004 / Error
         - "Slot no longer available" toast
         - Slot grid auto-refreshes (UXR-601)
```

#### Required Interactions

- Slot selection: optimistic UI highlight within 200ms (UXR-501).
- Insurance inline validation badge — no page navigation required.
- PDF confirmation indicator: "Generating confirmation…" → "Confirmation emailed ✓" (UXR-504).
- Preferred slot summary shown on SCR-006 with waitlist position acknowledgement.

---

### Flow: FL-003 — Appointment Cancellation / Reschedule

**Flow ID**: FL-003
**Derived From**: UC-005, UC-006
**Personas Covered**: Patient
**Description**: Patient cancels or reschedules an existing booked appointment.

#### Flow Sequence

```text
1. Entry: SCR-003 Patient Dashboard / Default
   - Trigger: Patient selects appointment card → "Manage"
   |
   v
2. SCR-005 Appointment Detail / Default
   - Shows slot, status, insurance validation result
   |
   v
3. Decision Point:
   +-- Cancel path:
   |     - Cancel Confirmation Modal / Default
   |     - Confirm -> Appointment cancelled; slot released
   |       -> SCR-003 / Default (cancellation toast)
   |     - Abandon -> SCR-005 / Default (no change)
   +-- Reschedule path:
         - Reschedule Drawer opens (slot picker)
         - Patient selects new slot
         - Atomic swap executed (optimistic lock)
         - Decision:
           +-- Success -> SCR-003 / Default (reschedule toast + updated PDF badge)
           +-- Slot conflict -> Drawer / Error (conflict message + slot refresh)
```

#### Required Interactions

- Cancel modal requires explicit confirmation (UXR per UC-005 extension 4a).
- Reschedule slot picker in drawer; no full page navigation.
- Conflict resolution refreshes slot grid in-drawer without closing the drawer.

---

### Flow: FL-004 — Patient Intake — AI Path

**Flow ID**: FL-004
**Derived From**: UC-009, UC-011
**Personas Covered**: Patient
**Description**: Patient completes pre-appointment intake using AI conversational pathway; may switch to manual.

#### Flow Sequence

```text
1. Entry: SCR-003 Patient Dashboard / Default
   - Trigger: Patient clicks "Complete intake" on appointment card
   |
   v
2. SCR-007 AI Conversational Intake / Default
   - Gemini presents questions as chat bubbles, one at a time
   - Progress bar advances per question (UXR-502)
   |
   v
3. Per-question loop:
   +-- AI parses response -> next question rendered
   +-- AI cannot parse -> manual text field shown for that field (fallback)
   |
   v
4. All questions answered → Summary review screen (inline in SCR-007)
   - Patient can edit any answer
   |
   v
5. Decision Point:
   +-- Patient confirms -> IntakeRecord persisted -> SCR-003 / Default (intake complete badge)
   +-- Patient switches to Manual -> SCR-008 / Default (pre-populated with AI-captured fields)
   +-- AI unavailable -> SCR-008 / Default (fallback, pre-empty)
```

#### Required Interactions

- "Switch to manual form" toggle visible throughout session (UXR-103).
- Field data preserved across switch (UXR-103).
- Streaming or discrete-step question animation (UXR-502).

---

### Flow: FL-005 — Patient Intake — Manual Path

**Flow ID**: FL-005
**Derived From**: UC-010, UC-011
**Personas Covered**: Patient
**Description**: Patient completes pre-appointment intake using the structured manual form.

#### Flow Sequence

```text
1. Entry: SCR-003 Dashboard / Default
   - Trigger: Patient selects "Manual form" tab or switches from AI
   |
   v
2. SCR-008 Manual Intake Form / Default
   - All structured intake fields presented
   - Required fields marked with asterisk
   |
   v
3. Patient submits form
   |
   v
4. Decision Point:
   +-- All required fields present -> IntakeRecord persisted -> SCR-003 (intake complete)
   +-- Missing required fields -> SCR-008 / Validation (inline field errors, UXR-204)
   +-- Switch to AI -> SCR-007 / Default (pre-populated with entered field values)
```

---

### Flow: FL-006 — Calendar Sync OAuth Setup

**Flow ID**: FL-006
**Derived From**: UC-013
**Personas Covered**: Patient
**Description**: Patient grants OAuth consent for Google or Outlook calendar synchronisation.

#### Flow Sequence

```text
1. Entry: SCR-003 Dashboard or SCR-016 Calendar Setup / Default
   - Trigger: Patient clicks "Connect calendar" or prompted post-booking
   |
   v
2. SCR-016 Calendar OAuth Consent / Default
   - Patient selects provider (Google / Outlook)
   - Redirect to OAuth consent screen (external)
   |
   v
3. Decision Point:
   +-- OAuth granted -> CalendarSync record created
   |     -> SCR-016 / Default (provider connected badge)
   +-- OAuth denied -> SCR-016 / Default (advisory: "Calendar sync not enabled — you can connect later")
   +-- API error -> SCR-016 / Error (amber advisory, retry link — UXR-604)
```

---

### Flow: FL-007 — Staff Walk-in & Queue Management

**Flow ID**: FL-007
**Derived From**: UC-014, UC-015, UC-016
**Personas Covered**: Staff
**Description**: Staff books a walk-in appointment, manages the same-day queue, and marks patient arrivals.

#### Flow Sequence

```text
1. Entry: SCR-011 Staff Queue Dashboard / Default
   - Trigger: Staff logs in and lands on queue dashboard
   |
   v
2. SCR-011 — View today's queue
   +-- Reorder entry -> drag or position input; audit logged
   +-- Remove entry -> reason modal -> audit logged
   +-- Mark arrived -> "Mark Arrived" action on row -> status updates to "Arrived"
   |
   v
3. Walk-in booking path:
   - Staff clicks "New walk-in" -> SCR-012 Walk-in Booking / Default
   - Search or enter patient details; select slot
   - Optionally create patient account
   - Decision:
     +-- Slot available -> Appointment created -> SCR-011 / Default (queue updates)
     +-- No slots -> Advisory: "Add to same-day queue without slot"
```

#### Required Interactions

- RBAC enforcement: Patient role cannot access SCR-011 or SCR-012 (FR-026, UXR-202).
- "Mark Arrived" action records Staff actor and timestamp in audit log.

---

### Flow: FL-008 — Admin User Management

**Flow ID**: FL-008
**Derived From**: UC-017
**Personas Covered**: Admin
**Description**: Admin manages user accounts and roles.

#### Flow Sequence

```text
1. Entry: SCR-015 Admin User Management / Default
   - Trigger: Admin navigates to user management section
   |
   v
2. Search for user (inline table filter)
   |
   v
3. Select user row → inline edit panel expands
   |
   v
4. Action selection:
   +-- Update profile -> Field edit -> Save -> Audit logged
   +-- Change role -> Role select -> If Admin downgrade: Confirm Modal -> Save
   +-- Deactivate -> Confirm Modal -> Account deactivated; active session invalidated
   +-- Deactivate own account -> Blocked: Error message "You cannot deactivate your own account"
```

#### Required Interactions

- Role downgrade from Admin triggers explicit confirmation modal (UC-017 extension 3b).
- Self-deactivation blocked with inline error (UC-017 extension 3a).

---

### Flow: FL-009 — Clinical Document Upload & AI Extraction

**Flow ID**: FL-009
**Derived From**: UC-019, UC-021
**Personas Covered**: Patient
**Description**: Patient uploads a PDF clinical document; AI extraction is queued automatically.

#### Flow Sequence

```text
1. Entry: SCR-010 Document Upload / Default
   - Trigger: Patient navigates to "My Documents"
   |
   v
2. Patient selects document type (Historical / Post-visit)
   - FileUpload component: drag-drop or browse
   |
   v
3. Decision Point:
   +-- Valid PDF, within size limit -> Upload progress indicator
   |     -> Document stored; extraction job queued
   |     -> SCR-010 / Default (document in list, status: "Extracting…")
   +-- Invalid file type -> Inline error: "Only PDF files accepted" (UXR-603 variant)
   +-- File too large -> Inline error: "File exceeds size limit"
   |
   v
4. Extraction completes (background):
   +-- Success -> document status → "Complete"; 360° profile updated
   +-- Failure -> document status → "Failed"; "Retry extraction" CTA visible (UXR-603)
```

---

### Flow: FL-010 — 360° Profile View & Conflict Resolution

**Flow ID**: FL-010
**Derived From**: UC-020, UC-022
**Personas Covered**: Staff, Patient
**Description**: Authorised user reviews unified patient profile; Staff resolves flagged data conflicts.

#### Flow Sequence

```text
1. Entry: SCR-013 360° Profile (Staff) or SCR-009 (Patient) / Default
   - Trigger: Navigate to patient profile
   |
   v
2. Profile sections rendered: Vitals / Medications / History / Conflicts
   - AI-generated data: ai-accent border + "AI" badge (UXR-403)
   - PHI fields: lock icon + surface-phi background (UXR-402)
   |
   v
3. Staff conflict resolution path (SCR-013 only):
   - Conflict card visible in Conflicts section (UXR-404)
   - Staff clicks conflict → Conflict Detail Drawer opens
   - Staff reviews conflicting values and source documents
   |
   v
4. Decision Point:
   +-- Resolve with authoritative value -> ConflictAlert status → "Resolved"
   +-- Mark "Reviewed — Unresolved" -> ConflictAlert persists with "Reviewed" badge
   +-- No conflicts -> Conflicts section shows "No conflicts detected"
```

---

### Flow: FL-011 — Medical Code Suggestion & Verification

**Flow ID**: FL-011
**Derived From**: UC-023, UC-024
**Personas Covered**: Staff
**Description**: Staff reviews AI-generated ICD-10 and CPT code suggestions and makes verification decisions.

#### Flow Sequence

```text
1. Entry: SCR-011 Staff Queue Dashboard / Default
   - Trigger: Staff selects "Verify codes" action on an encounter row
   |
   v
2. SCR-014 Medical Code Verification / Default
   - CodeSuggestionRow entries for each ICD-10 and CPT suggestion
   - Each row: code, description, confidence %, evidence snippet (UXR-107)
   - AI provenance badge on all suggestions (UXR-403)
   |
   v
3. Per-code decision loop:
   +-- Accept -> row → "Accepted" (success badge); audit entry written
   +-- Modify -> Code Modification Modal opens; staff enters corrected code
   |     -> Validation against ICD-10/CPT codeset
   |     -> If valid: row → "Modified" badge; audit entry
   |     -> If invalid: modal error; staff re-enters
   +-- Reject -> row → "Rejected" (danger badge); audit entry
   |
   v
4. All codes reviewed:
   +-- At least one accepted/modified -> encounter coding complete
   +-- All rejected -> "Manual coding required" banner; encounter flagged
```

---

## 12. Export Requirements

### JPG Export Settings

| Setting | Value |
|---------|-------|
| Format | JPG |
| Quality | High (85%) |
| Scale — Desktop | 2x |
| Scale — Mobile | 2x |
| Color Profile | sRGB |

### Export Naming Convention

`UPACIP__<Platform>__<ScreenName>__<State>__v1.jpg`

(UPACIP = Unified Patient Access & Clinical Intelligence Platform)

### Export Manifest

| Screen | State | Platform | Filename |
|--------|-------|----------|----------|
| SCR-001 Login | Default | Web | `UPACIP__Web__Login__Default__v1.jpg` |
| SCR-001 Login | Error | Web | `UPACIP__Web__Login__Error__v1.jpg` |
| SCR-001 Login | Validation | Web | `UPACIP__Web__Login__Validation__v1.jpg` |
| SCR-001 Login | Loading | Web | `UPACIP__Web__Login__Loading__v1.jpg` |
| SCR-002 Registration | Default | Web | `UPACIP__Web__Registration__Default__v1.jpg` |
| SCR-002 Registration | Error | Web | `UPACIP__Web__Registration__Error__v1.jpg` |
| SCR-002 Registration | Validation | Web | `UPACIP__Web__Registration__Validation__v1.jpg` |
| SCR-002 Registration | Loading | Web | `UPACIP__Web__Registration__Loading__v1.jpg` |
| SCR-003 Patient Dashboard | Default | Web | `UPACIP__Web__PatientDashboard__Default__v1.jpg` |
| SCR-003 Patient Dashboard | Loading | Web | `UPACIP__Web__PatientDashboard__Loading__v1.jpg` |
| SCR-003 Patient Dashboard | Empty | Web | `UPACIP__Web__PatientDashboard__Empty__v1.jpg` |
| SCR-004 Appointment Booking | Default | Web | `UPACIP__Web__AppointmentBooking__Default__v1.jpg` |
| SCR-004 Appointment Booking | Loading | Web | `UPACIP__Web__AppointmentBooking__Loading__v1.jpg` |
| SCR-004 Appointment Booking | Error | Web | `UPACIP__Web__AppointmentBooking__Error__v1.jpg` |
| SCR-004 Appointment Booking | Validation | Web | `UPACIP__Web__AppointmentBooking__Validation__v1.jpg` |
| SCR-005 Appointment Detail | Default | Web | `UPACIP__Web__AppointmentDetail__Default__v1.jpg` |
| SCR-005 Appointment Detail | Loading | Web | `UPACIP__Web__AppointmentDetail__Loading__v1.jpg` |
| SCR-005 Appointment Detail | Error | Web | `UPACIP__Web__AppointmentDetail__Error__v1.jpg` |
| SCR-006 Preferred Slot Confirm | Default | Web | `UPACIP__Web__PreferredSlotConfirm__Default__v1.jpg` |
| SCR-006 Preferred Slot Confirm | Empty | Web | `UPACIP__Web__PreferredSlotConfirm__Empty__v1.jpg` |
| SCR-007 AI Intake | Default | Web | `UPACIP__Web__AIIntake__Default__v1.jpg` |
| SCR-007 AI Intake | Loading | Web | `UPACIP__Web__AIIntake__Loading__v1.jpg` |
| SCR-007 AI Intake | Error | Web | `UPACIP__Web__AIIntake__Error__v1.jpg` |
| SCR-008 Manual Intake | Default | Web | `UPACIP__Web__ManualIntake__Default__v1.jpg` |
| SCR-008 Manual Intake | Loading | Web | `UPACIP__Web__ManualIntake__Loading__v1.jpg` |
| SCR-008 Manual Intake | Error | Web | `UPACIP__Web__ManualIntake__Error__v1.jpg` |
| SCR-008 Manual Intake | Validation | Web | `UPACIP__Web__ManualIntake__Validation__v1.jpg` |
| SCR-009 Patient Profile | Default | Web | `UPACIP__Web__PatientProfile__Default__v1.jpg` |
| SCR-009 Patient Profile | Loading | Web | `UPACIP__Web__PatientProfile__Loading__v1.jpg` |
| SCR-009 Patient Profile | Empty | Web | `UPACIP__Web__PatientProfile__Empty__v1.jpg` |
| SCR-010 Document Upload | Default | Web | `UPACIP__Web__DocumentUpload__Default__v1.jpg` |
| SCR-010 Document Upload | Loading | Web | `UPACIP__Web__DocumentUpload__Loading__v1.jpg` |
| SCR-010 Document Upload | Error | Web | `UPACIP__Web__DocumentUpload__Error__v1.jpg` |
| SCR-011 Staff Queue | Default | Web | `UPACIP__Web__StaffQueue__Default__v1.jpg` |
| SCR-011 Staff Queue | Loading | Web | `UPACIP__Web__StaffQueue__Loading__v1.jpg` |
| SCR-011 Staff Queue | Empty | Web | `UPACIP__Web__StaffQueue__Empty__v1.jpg` |
| SCR-012 Walk-in Booking | Default | Web | `UPACIP__Web__WalkinBooking__Default__v1.jpg` |
| SCR-012 Walk-in Booking | Loading | Web | `UPACIP__Web__WalkinBooking__Loading__v1.jpg` |
| SCR-012 Walk-in Booking | Error | Web | `UPACIP__Web__WalkinBooking__Error__v1.jpg` |
| SCR-012 Walk-in Booking | Validation | Web | `UPACIP__Web__WalkinBooking__Validation__v1.jpg` |
| SCR-013 Staff Profile | Default | Web | `UPACIP__Web__StaffProfile__Default__v1.jpg` |
| SCR-013 Staff Profile | Loading | Web | `UPACIP__Web__StaffProfile__Loading__v1.jpg` |
| SCR-013 Staff Profile | Empty | Web | `UPACIP__Web__StaffProfile__Empty__v1.jpg` |
| SCR-013 Staff Profile | Error | Web | `UPACIP__Web__StaffProfile__Error__v1.jpg` |
| SCR-014 Code Verification | Default | Web | `UPACIP__Web__CodeVerification__Default__v1.jpg` |
| SCR-014 Code Verification | Loading | Web | `UPACIP__Web__CodeVerification__Loading__v1.jpg` |
| SCR-014 Code Verification | Empty | Web | `UPACIP__Web__CodeVerification__Empty__v1.jpg` |
| SCR-014 Code Verification | Error | Web | `UPACIP__Web__CodeVerification__Error__v1.jpg` |
| SCR-015 Admin Users | Default | Web | `UPACIP__Web__AdminUsers__Default__v1.jpg` |
| SCR-015 Admin Users | Loading | Web | `UPACIP__Web__AdminUsers__Loading__v1.jpg` |
| SCR-015 Admin Users | Empty | Web | `UPACIP__Web__AdminUsers__Empty__v1.jpg` |
| SCR-015 Admin Users | Error | Web | `UPACIP__Web__AdminUsers__Error__v1.jpg` |
| SCR-015 Admin Users | Validation | Web | `UPACIP__Web__AdminUsers__Validation__v1.jpg` |
| SCR-016 Calendar OAuth | Default | Web | `UPACIP__Web__CalendarOAuth__Default__v1.jpg` |
| SCR-016 Calendar OAuth | Loading | Web | `UPACIP__Web__CalendarOAuth__Loading__v1.jpg` |
| SCR-016 Calendar OAuth | Error | Web | `UPACIP__Web__CalendarOAuth__Error__v1.jpg` |

### Total Export Count

- **Screens**: 16
- **States per screen**: average 3.3
- **Total JPGs**: 53

---

## 13. Figma File Structure

### Page Organization

```text
UPACIP Figma File
+-- 00_Cover
|   +-- Project info, version (v1.0), date, platform (Web), status (Draft)
+-- 01_Foundations
|   +-- Color tokens (primitives + semantic aliases)
|   +-- Typography scale (system-stack, 6 sizes)
|   +-- Spacing scale (8-step)
|   +-- Radius tokens (4 values)
|   +-- Elevation/shadows (3 levels)
|   +-- Grid definitions (1280px / 768px / 375px)
+-- 02_Components
|   +-- C/Actions/[Button, Link, IconButton]
|   +-- C/Inputs/[TextField, PasswordField, SelectField, DateTimePicker, FileUpload, Checkbox, RadioGroup, Toggle]
|   +-- C/Navigation/[TopNav, Sidebar, Tabs, Breadcrumb]
|   +-- C/Content/[AppointmentCard, SlotGrid, DataTable, ProfileSection, ConflictAlert, CodeSuggestionRow, Badge]
|   +-- C/Feedback/[Modal, Drawer, Toast, Alert, SkeletonLoader, Spinner, ProgressBar]
+-- 03_Patterns
|   +-- Auth form pattern (login, registration)
|   +-- Slot picker pattern (grid + conflict state)
|   +-- Chat/intake pattern (AI conversation)
|   +-- Clinical profile pattern (PHI + AI attribution)
|   +-- Error/Empty/Loading patterns
|   +-- Session timeout pattern
+-- 04_Screens
|   +-- SCR-001 Login / [Default, Error, Validation, Loading]
|   +-- SCR-002 Registration / [Default, Error, Validation, Loading]
|   +-- SCR-003 Patient Dashboard / [Default, Loading, Empty]
|   +-- SCR-004 Appointment Booking / [Default, Loading, Error, Validation]
|   +-- SCR-005 Appointment Detail / [Default, Loading, Error]
|   +-- SCR-006 Preferred Slot Confirm / [Default, Empty]
|   +-- SCR-007 AI Intake / [Default, Loading, Error]
|   +-- SCR-008 Manual Intake / [Default, Loading, Error, Validation]
|   +-- SCR-009 Patient Profile / [Default, Loading, Empty]
|   +-- SCR-010 Document Upload / [Default, Loading, Error]
|   +-- SCR-011 Staff Queue / [Default, Loading, Empty]
|   +-- SCR-012 Walk-in Booking / [Default, Loading, Error, Validation]
|   +-- SCR-013 Staff Profile / [Default, Loading, Empty, Error]
|   +-- SCR-014 Code Verification / [Default, Loading, Empty, Error]
|   +-- SCR-015 Admin Users / [Default, Loading, Empty, Error, Validation]
|   +-- SCR-016 Calendar OAuth / [Default, Loading, Error]
+-- 05_Prototype
|   +-- FL-001: Registration & Login
|   +-- FL-002: Appointment Booking with Preferred Slot
|   +-- FL-003: Cancel / Reschedule
|   +-- FL-004: AI Intake
|   +-- FL-005: Manual Intake
|   +-- FL-006: Calendar Sync OAuth
|   +-- FL-007: Staff Walk-in & Queue
|   +-- FL-008: Admin User Management
|   +-- FL-009: Document Upload & Extraction
|   +-- FL-010: 360° Profile & Conflict Resolution
|   +-- FL-011: Code Suggestion & Verification
+-- 06_Handoff
    +-- Token usage rules
    +-- PHI field guidelines
    +-- AI-attribution component guidelines
    +-- Responsive specs (breakpoints, grid)
    +-- Accessibility notes (WCAG 2.2 AA, focus rings, ARIA)
    +-- Edge case documentation
```

---

## 14. Quality Checklist

### Pre-Export Validation

- [ ] All screens have required states (Default/Loading/Empty/Error/Validation per inventory)
- [ ] All components use design tokens only (no hard-coded hex, px, or rgba values)
- [ ] Color contrast meets WCAG 2.2 AA (≥4.5:1 text, ≥3:1 UI) on all screen states
- [ ] Focus states defined for all interactive elements with ≥3:1 contrast
- [ ] Touch targets ≥44×44px on 375px mobile frames
- [ ] All 11 prototype flows wired and navigable
- [ ] `UPACIP__<Platform>__<Screen>__<State>__v1.jpg` naming convention followed
- [ ] Export manifest above is complete (53 JPG entries)

### Post-Generation

- [ ] `designsystem.md` updated with Figma references when available
- [ ] Export manifest generated
- [ ] Handoff documentation complete
- [ ] PHI field annotation pattern applied consistently across all screens
- [ ] AI-generated vs human-verified visual differentiation applied to SCR-009, SCR-013, SCR-014
