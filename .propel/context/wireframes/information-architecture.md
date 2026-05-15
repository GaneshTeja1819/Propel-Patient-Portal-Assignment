# Information Architecture

> **Project:** Unified Patient Access & Clinical Intelligence Platform (UPACIP)
> **Wireframe Set:** Hi-Fi — 16 screens, standalone HTML5
> **Date:** 2026-05-20
> **Aesthetic direction:** Utilitarian — clinical precision, trust-first AI, information hierarchy over decoration

---

## 1. Wireframe Specification

| Attribute | Value |
|---|---|
| Fidelity | High-Fidelity (Hi-Fi) |
| Screen type | Web |
| Viewport width | 1440px (primary) |
| Responsive breakpoints | 1440px · 768px · 640px (slot grid) · 375px (mobile) |
| Output path | `.propel/context/wireframes/Hi-Fi/` |
| Total screens | 17 |
| Tech stack | Standalone HTML5, CSS custom properties, vanilla JS |
| Dependencies | None (zero external dependencies) |
| Accessibility | WCAG 2.2 Level AA |
| Focus rings | 3px solid `#1A56DB`, 2px offset |
| Touch targets | ≥ 44px all interactive elements |
| ARIA | Semantic roles, live regions, aria-modal, aria-expanded throughout |

---

## 2. System Overview

UPACIP is a multi-role web application serving three distinct actor groups:

| Actor | Entry point | Portal |
|---|---|---|
| **Patient** | SCR-001 → SCR-003 | Patient Portal (navy logo mark) |
| **Staff** (nurse / reception) | SCR-001 → SCR-011 | Staff Portal (green logo mark) |
| **Admin** | SCR-001 → SCR-015 | Admin Portal (red logo mark) |

The system processes PHI under HIPAA controls (visual cue: 🔒 lock icon prefix + `#EFF6FF` background on all PHI fields and values). AI-generated content carries a purple AI badge (`#7C3AED`); clinician-verified content carries a green Verified badge (`#16A34A`).

---

## 3. Wireframe Reference Index

| File | Screen ID | Title | Actor | Primary UC |
|---|---|---|---|---|
| `wireframe-SCR-001-login.html` | SCR-001 | Login | All | UC-001 |
| `wireframe-SCR-002-registration.html` | SCR-002 | Registration | Patient | UC-002 |
| `wireframe-SCR-003-patient-dashboard.html` | SCR-003 | Patient Dashboard | Patient | UC-003 |
| `wireframe-SCR-004-appointment-booking.html` | SCR-004 | Appointment Booking | Patient | UC-004, UC-018 |
| `wireframe-SCR-005-appointment-detail.html` | SCR-005 | Appointment Detail | Patient | UC-005, UC-006 |
| `wireframe-SCR-006-preferred-slot-confirm.html` | SCR-006 | Preferred Slot Confirmation | Patient | UC-007 |
| `wireframe-SCR-007-ai-intake.html` | SCR-007 | AI Conversational Intake | Patient | UC-009, UC-011 |
| `wireframe-SCR-008-manual-intake.html` | SCR-008 | Manual Intake Form | Patient | UC-010, UC-011 |
| `wireframe-SCR-009-patient-profile.html` | SCR-009 | Patient 360° Profile (Patient View) | Patient | UC-020 |
| `wireframe-SCR-010-document-upload.html` | SCR-010 | Document Upload | Patient | UC-019 |
| `wireframe-SCR-011-staff-queue.html` | SCR-011 | Staff Dashboard / Queue | Staff | UC-014, UC-015, UC-016 |
| `wireframe-SCR-012-walkin-booking.html` | SCR-012 | Staff Walk-in Booking | Staff | UC-014 |
| `wireframe-SCR-013-staff-patient-profile.html` | SCR-013 | Staff Patient Profile (Conflicts) | Staff | UC-020, UC-022 |
| `wireframe-SCR-014-code-verification.html` | SCR-014 | Medical Code Verification | Staff | UC-023, UC-024 |
| `wireframe-SCR-015-admin-user-management.html` | SCR-015 | Admin User Management | Admin | UC-017 |
| `wireframe-SCR-016-calendar-oauth.html` | SCR-016 | Calendar OAuth Consent | Patient | UC-013 |
| `wireframe-SCR-017-admin-audit-log.html` | SCR-017 | Admin Audit Log | Admin | UC-018 |

---

## 4. Personas & Flows

### Persona A — Sarah Johnson (Patient, usr-001)
- Age 37, DOB Mar 12 1989, active portal user
- Primary flows: Book → Intake → Dashboard → Upload → Profile
- Calendar: Google Calendar connected (sync failure state shown in SCR-016)

### Persona B — Michael Chen (Patient as Staff subject, usr-002)
- Appears as the subject in SCR-013 (staff view) and SCR-014 (code verification)
- Has a CRITICAL medication conflict (conf-001: Metoprolol dosage discrepancy)

### Persona C — Staff Nurse Practitioner (Staff, usr-006)
- Manages SCR-011 queue, SCR-012 walk-in booking, SCR-013 profile review, SCR-014 code verification

### Persona D — Admin (Admin, usr-001 in admin portal)
- Manages all users via SCR-015; protected from self-deactivation (UC-017 ext 3a)

### Flows

| Flow ID | Description | Screens |
|---|---|---|
| FL-001 | Auth — login / register | SCR-001 ↔ SCR-002, SCR-001 → SCR-003/011/015 |
| FL-002 | Appointment booking (patient) | SCR-003 → SCR-004 → SCR-003 (or SCR-006 if waitlist) |
| FL-003 | Appointment detail / cancel / reschedule | SCR-003 → SCR-005 → SCR-003 |
| FL-004 | AI intake | SCR-003 → SCR-007 → SCR-003 |
| FL-005 | Manual intake | SCR-003 → SCR-008 → SCR-003 |
| FL-006 | Calendar sync | SCR-003/016 → SCR-016 → SCR-003 |
| FL-007 | Staff queue → walk-in | SCR-011 → SCR-012 → SCR-011 |
| FL-008 | Admin user management | SCR-015 (self-contained) |
| FL-009 | Admin audit log | SCR-017 (self-contained, read-only) |
| FL-009 | Document upload → profile | SCR-010 → SCR-009 |
| FL-010 | Staff profile + conflict resolution | SCR-011/013 → SCR-013 (drawer) |
| FL-011 | Staff code verification | SCR-011 → SCR-014 → SCR-011 |

---

## 5. Screen Hierarchy

```
Root
├── Auth layer
│   ├── SCR-001 Login
│   └── SCR-002 Registration
├── Patient Portal
│   ├── SCR-003 Dashboard (hub)
│   │   ├── SCR-004 Appointment Booking
│   │   │   └── SCR-006 Waitlist Confirmation
│   │   ├── SCR-005 Appointment Detail
│   │   ├── SCR-007 AI Intake
│   │   ├── SCR-008 Manual Intake
│   │   ├── SCR-009 Patient Profile
│   │   │   └── → SCR-010 Document Upload
│   │   ├── SCR-010 Document Upload
│   │   └── SCR-016 Calendar Sync
├── Staff Portal
│   ├── SCR-011 Queue Dashboard (hub)
│   │   ├── SCR-012 Walk-in Booking
│   │   ├── SCR-013 Staff Patient Profile
│   │   └── SCR-014 Code Verification
└── Admin Portal
    └── SCR-015 User Management
```

---

## 6. Navigation Architecture

### Patient Portal Top Navigation
| Item | Target | Notes |
|---|---|---|
| Logo / Brand | SCR-003 | Always visible |
| Dashboard | SCR-003 | Active state on SCR-003 |
| Book | SCR-004 | Active on SCR-004 |
| My Profile | SCR-009 | Active on SCR-009 |
| Documents | SCR-010 | Active on SCR-010 |
| Calendar | SCR-016 | Active on SCR-016 |
| Sign out | SCR-001 | Destroys session |

### Staff Portal Top Navigation
| Item | Target |
|---|---|
| Logo | SCR-011 |
| Queue | SCR-011 |
| New Walk-in | SCR-012 |
| Sign out | SCR-001 |

### Admin Portal Top Navigation
| Item | Target |
|---|---|
| Logo | SCR-015 |
| Users | SCR-015 |
| Sign out | SCR-001 |

### In-Page Navigation Patterns
- **Drawers:** Right-side slide-in (SCR-005 reschedule, SCR-013 conflict detail). Overlay backdrop dismisses on click. Escape key closes.
- **Modals:** Centred overlays for destructive confirmations (cancel appointment, deactivate user, role change). Escape key closes.
- **Breadcrumbs:** Dashboard / [Current Page] on all non-hub screens. Role: navigation landmark.
- **Method switch:** AI ↔ Manual toggle persisted across SCR-007/008 (UXR-103).

---

## 7. Interaction Patterns

| Pattern | Screens | UXR | Implementation |
|---|---|---|---|
| Optimistic slot selection | SCR-004 | UXR-501 | Instant visual update on click; server confirmation deferred |
| Slot conflict toast | SCR-004 | UXR-601 | `aria-live="assertive"`, auto-dismiss 5s |
| Insurance soft-validation | SCR-004 | UXR-602 | Non-blocking badge; booking proceeds regardless |
| PDF confirmation progress | SCR-004 | UXR-504 | Inline spinner strip, no modal |
| Session timeout modal | SCR-003 | UXR-205 | `aria-live="assertive"` countdown, 30s warning |
| Intake method switch | SCR-007/008 | UXR-103 | Data preserved on toggle |
| Step progress bar | SCR-007/008 | UXR-502 | Discrete steps, percentage fill |
| Collapsible profile sections | SCR-009/013 | — | `aria-expanded` toggle, hidden class |
| Drag-drop upload | SCR-010 | — | File API + dragover/drop events |
| Retry extraction | SCR-010 | UXR-603 | Inline CTA on failed row (non-modal) |
| Mark arrived | SCR-011 | UC-016 | Inline row button, optimistic status update |
| Conflict resolution drawer | SCR-013 | UXR-404 | Right drawer, radio source selection, resolution note |
| Code decision audit | SCR-014 | UXR-107, UXR-403 | Accept/Modify/Reject per row; all audit-logged with timestamp |
| Self-deactivation block | SCR-015 | UC-017 ext 3a | Inline error inside modal, confirm disabled |
| OAuth connect/disconnect | SCR-016 | UC-013 | Simulated OAuth redirect, loading states |
| Sync failure advisory | SCR-016 | UXR-604 | Amber non-modal advisory, dismissable, retry CTA |

---

## 8. Error Handling

| Error | Screen | Approach |
|---|---|---|
| Login failure | SCR-001 | Generic error (no field-specific disclosure, OWASP A07) |
| Password strength insufficient | SCR-002 | Inline strength meter (visual only, no text error until submit) |
| Required field missing | SCR-002/008 | Inline field-level error, `aria-describedby` |
| Slot conflict (concurrent booking) | SCR-004 | Assertive live-region toast, slot cleared |
| Insurance not recognised | SCR-004 | Non-blocking amber badge; booking continues |
| Appointment cancellation (confirm) | SCR-005 | Two-step modal (MOD-001); no cancel without confirmation |
| Intake submission without required field | SCR-008 | Field validation, focus moves to first error |
| AI extraction failure | SCR-010 | Row-level error badge + Retry CTA (UXR-603) |
| Conflict unresolved | SCR-013 | Critical/High alert card with Review CTA |
| Code submission with pending decisions | SCR-014 | Submit button disabled until all decided |
| Self-deactivation attempt | SCR-015 | Inline error inside modal; confirm button disabled |
| Calendar sync failure | SCR-016 | Amber advisory (UXR-604), non-modal, retry CTA |

---

## 9. Responsive Strategy

| Breakpoint | Changes |
|---|---|
| ≤ 1024px | Booking/detail grids collapse to single column; staff queue stats to 2-column |
| ≤ 768px | Top navigation links hidden; profile grids linearise; drawers expand to full width |
| ≤ 640px | Slot grid: 4-col → 2-col (UXR-302); form field rows → single column; main padding reduced |
| ≤ 375px | Slot grid: 1-col; all layouts fully linear; touch targets retained at ≥ 44px |

---

## 10. Accessibility

| Requirement | Implementation |
|---|---|
| WCAG 2.2 AA | Contrast ratios ≥ 4.5:1 (text), ≥ 3:1 (UI components) |
| Focus indicators | 3px solid `#1A56DB`, 2px offset, `:focus-visible` only |
| Touch targets | All buttons/inputs height ≥ 44px, min-width ≥ 44px |
| Keyboard navigation | Escape closes modals/drawers; Enter/Space activates card buttons |
| Screen reader | `aria-label`, `aria-required`, `aria-describedby`, `aria-expanded`, `role="dialog"` throughout |
| Live regions | Session timeout `aria-live="assertive"`; slot conflict toast; queue mark-arrived |
| PHI | Lock icon prefix (🔒) + `#EFF6FF` background on all PHI input fields and read-only values |
| AI content | Purple AI badge (`#7C3AED`) on all AI-generated values (UXR-403) |
| Disabled states | `disabled` attribute on taken/blocked slots; cursor:not-allowed |

---

## 11. Content Strategy

- **Tone:** Clinical, direct, trust-building. No marketing copy.
- **PHI labels:** All PHI fields prefixed with 🔒 and labelled with "Protected health information".
- **AI attribution:** Every AI-generated data point carries a "🤖 AI" badge. Confidence percentages shown where available (SCR-014).
- **Error messages:** Generic on auth (security); specific + actionable on form validation.
- **Empty states:** Descriptive placeholder text ("No slot selected — select a slot first") on all summary/confirmation components.
- **Sample data:** All wireframes use realistic healthcare domain data from `data/sample-data.json` (no lorem ipsum).
- **Dates:** All relative to May 20, 2026 (today's date in sample data context).
