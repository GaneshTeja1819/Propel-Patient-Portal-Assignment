# Task - TASK_001

## Requirement Reference
- **User Story:** us_014
- **Story Location:** .propel/context/tasks/EP-002/us_014/us_014.md
- **Acceptance Criteria:**
  - AC-002: Cancel confirmation prompt shown; patient can dismiss without changing appointment
  - AC-003: Past appointments — "Cancel" action is disabled or hidden; no API call possible
- **Edge Cases:**
  - Cancel prompt dismissed → appointment remains "Booked"; no audit entry
  - No alternative slots available → empty slot grid; existing appointment unchanged
  - Reschedule HTTP 409 → error message shown; grid refreshes; existing appointment unchanged

---

## Design References [CONDITIONAL: UI Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-003-patient-dashboard.html |
| **Screen Spec** | SCR-003, SCR-005 |
| **UXR Requirements** | N/A |
| **Design Tokens** | `--color-error`, `--color-neutral` from variables.css |

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
| Frontend | React (SPA) | 18.x | TR-001, NFR-010 — modals and routing on SCR-003/SCR-005 |

---

## Task Overview
Add cancel and reschedule UI actions to the patient's appointment list (SCR-003) and appointment detail (SCR-005). Cancellation requires a confirmation modal before calling the API. The "Cancel" and "Reschedule" buttons are hidden/disabled for past appointments (`appointment.startDateTime < now()`). Rescheduling reuses the `SlotGrid` component from US_012; on HTTP 409 from the reschedule endpoint an error message is shown and the grid refetches without modifying the existing appointment.

## Dependent Tasks
- `task_001_frontend-slot-grid.md` (US_012) — `SlotGrid` component must exist
- `task_001_frontend-booking-form.md` (US_013) — `Toast` component must exist

## Impacted Components
- `frontend/src/pages/DashboardPage.tsx` — show cancel/reschedule action buttons per appointment
- `frontend/src/pages/AppointmentDetailPage.tsx` — new SCR-005 page
- `frontend/src/components/appointments/CancelConfirmModal.tsx` — new cancel confirmation modal
- `frontend/src/components/appointments/RescheduleFlow.tsx` — new reschedule inline flow
- `frontend/src/hooks/useCancelAppointment.ts` — new hook calling PATCH /cancel
- `frontend/src/hooks/useRescheduleAppointment.ts` — new hook calling PATCH /reschedule

## Implementation Plan
1. Create `CancelConfirmModal.tsx`: `role="dialog"`, `aria-modal="true"`, `aria-labelledby="cancel-confirm-title"`; two buttons — "Yes, cancel" (destructive style) and "Keep appointment" (secondary style); `"Keep appointment"` closes modal without firing any API call
2. Create `useCancelAppointment` hook: calls `PATCH /api/v1/appointments/{id}/cancel`; on HTTP 200 → refetch appointment list; on error → show error toast
3. Create `RescheduleFlow.tsx`: renders `SlotGrid` inside a modal or inline panel; on slot selection → submit button fires `useRescheduleAppointment`; on HTTP 409 → error toast "Slot no longer available" + `queryClient.invalidateQueries(['slots'])`; on HTTP 200 → close panel + refetch appointments
4. Create `useRescheduleAppointment` hook: calls `PATCH /api/v1/appointments/{id}/reschedule` with `{ newSlotId }`; handles HTTP 409 and 200
5. In `DashboardPage` and `AppointmentDetailPage`: compute `isPast = appointment.startDateTime < new Date()` and set `disabled` or `hidden` on action buttons (AC-003)
6. Add `/appointments/:id` route to `App.tsx` for `AppointmentDetailPage`

## Current Project State
```
frontend/
  src/
    pages/DashboardPage.tsx
    components/booking/SlotGrid.tsx  (from US_012)
    components/common/Toast.tsx      (from US_013)
    styles/variables.css
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | frontend/src/pages/DashboardPage.tsx | Add cancel/reschedule action buttons; isPast guard |
| CREATE | frontend/src/pages/AppointmentDetailPage.tsx | SCR-005 appointment detail page |
| CREATE | frontend/src/components/appointments/CancelConfirmModal.tsx | Cancel confirmation modal |
| CREATE | frontend/src/components/appointments/RescheduleFlow.tsx | Slot grid reschedule flow |
| CREATE | frontend/src/hooks/useCancelAppointment.ts | PATCH /cancel API call |
| CREATE | frontend/src/hooks/useRescheduleAppointment.ts | PATCH /reschedule API call |
| MODIFY | frontend/src/App.tsx | Add /appointments/:id route |

## External References
- [WCAG 2.2 SC 3.3.4 Error Prevention (Legal, Financial, Data)](https://www.w3.org/WAI/WCAG22/Understanding/error-prevention-legal-financial-data.html)
- [wireframe-SCR-003-patient-dashboard.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-003-patient-dashboard.html)

## Build Commands
- Refer to [frontend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Click "Cancel" on a future appointment → modal appears; click "Keep appointment" → modal closes; appointment unchanged
- [ ] Click "Cancel" on a past appointment → button disabled/hidden; no API call fired
- [ ] Reschedule; force HTTP 409 → error message shown; appointment still shows original slot

## Implementation Checklist
- [x] Build `CancelConfirmModal` with "Keep appointment" dismiss path (no API call) and accessible `role="dialog"` (AC-002)
- [x] Implement `useCancelAppointment` hook; on success refetch appointment list (AC-002)
- [x] Disable/hide "Cancel" + "Reschedule" buttons when `isPast` is true; no API call possible (AC-003)
- [x] Build `RescheduleFlow` reusing `SlotGrid`; HTTP 409 → error toast + grid refresh; existing appointment unchanged (AC-005 edge case)
- [x] Implement `useRescheduleAppointment` hook; handle HTTP 409 (edge case)
- [x] Add `/appointments/:id` route; create `AppointmentDetailPage` with same cancel/reschedule guards (AC-002, AC-003)
