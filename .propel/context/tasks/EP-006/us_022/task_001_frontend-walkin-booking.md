# Task - TASK_001

## Requirement Reference
- **User Story:** us_022
- **Story Location:** .propel/context/tasks/EP-006/us_022/us_022.md
- **Acceptance Criteria:**
  - AC-001: Staff books walk-in for existing patient; Appointment.createdByStaffId = Staff actor ID; slot booked; audit entry with Staff attribution
  - AC-002: No matching patient account found → anon booking with patient details; no User record required; audit records Staff actor + anonymous booking nature
  - AC-003: Staff creates patient account from anon walk-in; User record created; Appointment linked to new patientId; audit records account creation + link
  - AC-005: No available slots → "No available slots" shown with CTA to add patient to same-day queue
- **Edge Cases:**
  - Duplicate account creation (email already exists) → inform Staff; offer to link walk-in to existing account instead
  - Weak password for new patient account → same complexity rules as self-registration; inline validation error
  - Network failure after slot reserved but before Appointment created → atomic rollback; slot released; "Booking failed" shown

---

## Design References [CONDITIONAL: UI Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-012-walkin-booking.html |
| **Screen Spec** | SCR-012 |
| **UXR Requirements** | N/A |
| **Design Tokens** | `--color-primary`, `--color-error` from variables.css |

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
| Frontend | React (SPA) | 18.x | TR-001 — walk-in booking UI on SCR-012; Staff-only route |

---

## Task Overview
Build SCR-012 walk-in booking screen, accessible only to authenticated Staff. The screen has three sequential sections: (1) patient search (type-ahead by name or DOB), (2) slot selection (reuses `SlotGrid` from US_012), and (3) booking confirmation form with optional patient account creation. If no patient is found via search, the Staff can proceed as anonymous (pre-fill patient name/DOB fields). After booking, a "Create patient account" toggle expands an account creation form. No available slots shows an empty state with a "Add to same-day queue" CTA (queue functionality implemented in US_023). All UI transitions are handled as in-page steps (no full-page navigation).

## Dependent Tasks
- `task_001_frontend-slot-grid.md` (US_012) — `SlotGrid` must exist
- `task_001_frontend-login.md` (US_010) — `AuthContext` with role-based route guard must exist

## Impacted Components
- `frontend/src/pages/WalkInBookingPage.tsx` — new SCR-012 Staff-only page
- `frontend/src/components/staff/PatientSearchInput.tsx` — new type-ahead patient search
- `frontend/src/components/staff/AnonymousPatientForm.tsx` — new anonymous patient detail form
- `frontend/src/components/staff/CreatePatientAccountForm.tsx` — new optional account creation form
- `frontend/src/hooks/useWalkInBooking.ts` — new hook for staff walk-in POST
- `frontend/src/App.tsx` — add `/staff/walk-in` route with Staff role guard

## Implementation Plan
1. Create `WalkInBookingPage.tsx` at `/staff/walk-in`; guard with `<RequireRole role="Staff" />` component; three-step inline flow: Search → Slot Selection → Confirm
2. Create `PatientSearchInput.tsx`: debounced input (300 ms) calling `GET /api/v1/patients/search?q=`; renders dropdown of matching patients `{ id, name, dob }`; on select sets `selectedPatientId`; "Patient not found" option leads to anonymous form
3. Step 1 result paths:
   - Existing patient selected → `selectedPatientId` set; proceed to slot selection
   - "Patient not found" selected → render `AnonymousPatientForm` (name, DOB, phone, optional email)
4. Step 2: Render `SlotGrid` (date selector + slot cards); no available slots → empty state with "Add to same-day queue" CTA (navigates to `/staff/queue` — US_023 placeholder)
5. Step 3 (Confirm): show selected slot + patient summary; "Confirm Walk-in Booking" CTA calls `useWalkInBooking.bookWalkIn()`; after success render success state + "Create patient account" accordion toggle
6. `CreatePatientAccountForm.tsx`: email + password fields; password strength indicator; on submit calls `useWalkInBooking.createAccount()`; on duplicate email error → inline message "Email already registered — link to existing account?" with "Link" CTA

## Current Project State
```
frontend/
  src/
    components/booking/SlotGrid.tsx  (from US_012)
    context/AuthContext.tsx
    App.tsx
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/pages/WalkInBookingPage.tsx | SCR-012 Staff walk-in booking page (3-step flow) |
| CREATE | frontend/src/components/staff/PatientSearchInput.tsx | Debounced patient search type-ahead |
| CREATE | frontend/src/components/staff/AnonymousPatientForm.tsx | Anonymous patient detail form |
| CREATE | frontend/src/components/staff/CreatePatientAccountForm.tsx | Optional patient account creation |
| CREATE | frontend/src/hooks/useWalkInBooking.ts | Staff walk-in booking + account creation hooks |
| MODIFY | frontend/src/App.tsx | Add /staff/walk-in route with Staff role guard |

## External References
- [wireframe-SCR-012-walkin-booking.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-012-walkin-booking.html)
- [WCAG 2.2 SC 1.4.3 Contrast Minimum](https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html)

## Build Commands
- Refer to [frontend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Staff navigates to `/staff/walk-in`; non-Staff role redirected
- [ ] Type-ahead search returns matching patients; selecting one advances to slot selection
- [ ] "Patient not found" → anon form shown; booking proceeds without User record
- [ ] No available slots → "Add to same-day queue" CTA visible; slot grid empty
- [ ] After booking → "Create patient account" toggle available; duplicate email → inline error with "Link" CTA

## Implementation Checklist
- [ ] Build `WalkInBookingPage` with `<RequireRole role="Staff" />`; 3-step inline flow (AC-001, AC-002, AC-003)
- [ ] Build `PatientSearchInput` with 300 ms debounce; "Patient not found" option leads to anon form (AC-001, AC-002)
- [ ] Render `SlotGrid` in step 2; no-slots empty state with "Add to same-day queue" CTA (AC-005)
- [ ] Build `AnonymousPatientForm` for anon walk-in details (AC-002)
- [ ] Build `CreatePatientAccountForm`; duplicate email → inline "Link to existing account" message (AC-003, edge case)
- [ ] Password complexity validation via same rules as self-registration (edge case)
- [ ] Add `/staff/walk-in` route with Staff role guard (AC-001)
