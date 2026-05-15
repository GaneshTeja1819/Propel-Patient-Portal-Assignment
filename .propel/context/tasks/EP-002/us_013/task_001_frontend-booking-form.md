# Task - TASK_001

## Requirement Reference
- **User Story:** us_013
- **Story Location:** .propel/context/tasks/EP-002/us_013/us_013.md
- **Acceptance Criteria:**
  - AC-003: Insurance provider + ID fields show "Validated" green badge or "Not Recognised" amber advisory inline; booking CTA always enabled
  - AC-004: Slot conflict (HTTP 409) → error toast "Slot no longer available" within 2 s; slot grid refreshes; selected slot reverts to "Unavailable"
  - AC-005: Booking confirmation reached in ≤ 3 screen transitions from dashboard
- **Edge Cases:**
  - Insurance fields blank → soft validation skipped; booking not blocked
  - Concurrent slot conflict → losing patient sees slot revert with error toast after optimistic selection

---

## Design References [CONDITIONAL: UI Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-004-appointment-booking.html |
| **Screen Spec** | SCR-004 |
| **UXR Requirements** | UXR-101, UXR-601, UXR-602 |
| **Design Tokens** | `--color-success`, `--color-warning`, `--color-error` from variables.css |

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
| Frontend | React (SPA) | 18.x | TR-001, NFR-010 — booking form on SCR-004 |
| Frontend | React Hook Form | Latest stable | NFR-010 — form state; insurance field management |

---

## Task Overview
Extend `BookingPage` (SCR-004) with the booking form: date/time summary, insurance provider and ID fields with inline soft-validation badges, and a "Confirm Booking" CTA. The insurance badge updates on blur (calling `POST /api/v1/insurance/validate`) without blocking the CTA. On HTTP 409 from the booking endpoint, a dismissible error toast appears within 2 seconds and the `SlotGrid` refetches immediately. The full flow (slot select → form fill → confirm) completes in ≤ 3 screen transitions from the patient dashboard.

## Dependent Tasks
- `task_001_frontend-slot-grid.md` (US_012) — `SlotGrid` and `BookingPage` scaffold must exist

## Impacted Components
- `frontend/src/pages/BookingPage.tsx` — extend with booking form and flow
- `frontend/src/components/booking/BookingForm.tsx` — new form component
- `frontend/src/components/booking/InsuranceBadge.tsx` — new soft-validation badge component
- `frontend/src/components/common/Toast.tsx` — new dismissible toast component
- `frontend/src/hooks/useBooking.ts` — new hook wrapping POST /api/v1/appointments
- `frontend/src/hooks/useInsuranceValidation.ts` — new hook calling insurance validate endpoint

## Implementation Plan
1. Create `InsuranceBadge.tsx`: shows "Validated" (green, `--color-success`) or "Not Recognised" (amber, `--color-warning`) depending on validation result; renders nothing when fields are blank; validation fires on blur of the insurance ID field via `useInsuranceValidation`
2. Create `useInsuranceValidation` hook: calls `POST /api/v1/insurance/validate` with provider + id; on success returns `{ valid: boolean }`; on error defaults to `null` (badge hidden); never blocks the CTA
3. Create `BookingForm.tsx` with React Hook Form: fields — `selectedSlotId` (hidden, from grid), `insuranceProvider`, `insuranceId`; wire `InsuranceBadge` to insurance fields; "Confirm Booking" CTA always enabled
4. Create `Toast.tsx`: dismissible amber/red message component; auto-dismisses after 5 s; renders via a `useToast` hook with a portal (renders outside main DOM tree); `role="alert"` and `aria-live="polite"` for screen readers
5. Implement `useBooking` hook: calls `POST /api/v1/appointments`; on HTTP 409 → show error toast "Slot no longer available"; invalidate React Query slots cache (`queryClient.invalidateQueries(['slots'])`); on HTTP 200 → navigate to booking confirmation state
6. Wire `BookingPage` so the complete flow is: dashboard → `/booking` (slot selection, 1 transition) → booking form on same page (inline, 0 additional transitions) → confirmation state on same page (0 additional transitions) = 1 total navigation from dashboard (≤ 3 per AC-005)

## Current Project State
```
frontend/
  src/
    pages/BookingPage.tsx  (slot grid only, from US_012)
    components/booking/SlotGrid.tsx
    styles/variables.css
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | frontend/src/pages/BookingPage.tsx | Add BookingForm, confirmation state, flow wiring |
| CREATE | frontend/src/components/booking/BookingForm.tsx | Form with insurance fields and Confirm CTA |
| CREATE | frontend/src/components/booking/InsuranceBadge.tsx | Inline insurance soft-validation badge |
| CREATE | frontend/src/components/common/Toast.tsx | Dismissible toast with role="alert" |
| CREATE | frontend/src/hooks/useBooking.ts | POST /api/v1/appointments; 409 toast + grid refresh |
| CREATE | frontend/src/hooks/useInsuranceValidation.ts | Insurance validate API call on blur |
| MODIFY | frontend/src/styles/variables.css | Add --color-success, --color-warning, --color-error tokens |

## External References
- [WCAG 2.2 SC 4.1.3 Status Messages](https://www.w3.org/WAI/WCAG22/Understanding/status-messages.html)
- [wireframe-SCR-004-appointment-booking.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-004-appointment-booking.html)

## Build Commands
- Refer to [frontend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Entering valid insurance details shows "Validated" green badge on blur; blank fields show no badge
- [ ] Submitting when slot conflicts → toast "Slot no longer available" within 2 s; slot grid refetches
- [ ] Full booking flow from `/dashboard` completes in ≤ 3 screen transitions (1 navigation to `/booking`)

## Implementation Checklist
- [ ] Build `InsuranceBadge` showing "Validated" / "Not Recognised" on blur; hidden when fields blank; never blocks CTA (AC-003)
- [ ] Implement `useInsuranceValidation` hook; errors default to `null` (no badge) without blocking booking (AC-003)
- [ ] Build `BookingForm` with React Hook Form; wire InsuranceBadge; CTA always enabled (AC-003)
- [ ] Create `Toast` component with `role="alert"`, `aria-live="polite"`, auto-dismiss 5 s (AC-004)
- [ ] Implement `useBooking`; on HTTP 409 → show toast + invalidate slots cache within 2 s (AC-004)
- [ ] Wire `BookingPage` as single-page flow: slot select + form + confirm on one route ≤ 3 transitions from dashboard (AC-005)
