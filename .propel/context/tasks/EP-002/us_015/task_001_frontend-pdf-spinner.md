# Task - TASK_001

## Requirement Reference
- **User Story:** us_015
- **Story Location:** .propel/context/tasks/EP-002/us_015/us_015.md
- **Acceptance Criteria:**
  - AC-002: Booking confirmation screen shows "Generating confirmation..." spinner immediately after booking; transitions to "Confirmation emailed ✓" on job completion
- **Edge Cases:**
  - Polling request times out → keep spinner; retry next poll cycle
  - Job remains "Queued" for > 60 s → show "Confirmation is taking longer than expected" advisory text below spinner

---

## Design References [CONDITIONAL: UI Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-004-appointment-booking.html |
| **Screen Spec** | SCR-004 (confirmation state) |
| **UXR Requirements** | UXR-504 |
| **Design Tokens** | `--color-success`, `--spinner-size` from variables.css |

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
| Frontend | React (SPA) | 18.x | TR-001 — confirmation state on SCR-004 |

---

## Task Overview
Add a `PDFConfirmationStatus` component to the booking confirmation state on SCR-004. It appears immediately after a successful booking (US_013) and polls `GET /api/v1/appointments/{id}/confirmation-status` every 3 seconds. While polling shows `status == "Queued" | "Processing"` the spinner and "Generating confirmation..." text are displayed. When `status == "Sent"` the spinner is replaced with a green "Confirmation emailed ✓" checkmark. If polling has been running for more than 60 seconds without "Sent", an advisory message is shown below the spinner.

## Dependent Tasks
- `task_001_frontend-booking-form.md` (US_013) — `BookingPage` booking confirmation state must exist; `appointmentId` must be available after booking

## Impacted Components
- `frontend/src/components/booking/PDFConfirmationStatus.tsx` — new component
- `frontend/src/pages/BookingPage.tsx` — render `PDFConfirmationStatus` in confirmation state

## Implementation Plan
1. Create `PDFConfirmationStatus.tsx` accepting `appointmentId: string` prop
2. Use `useQuery` with `refetchInterval: 3000` to poll `GET /api/v1/appointments/${appointmentId}/confirmation-status`; stop polling when data `status === "Sent"` (set `enabled: status !== "Sent"`)
3. Render logic:
   - `status === "Sent"` → green checkmark icon + "Confirmation emailed ✓" text (`--color-success`)
   - `status === "Queued" | "Processing"` → CSS spinner + "Generating confirmation..." text
   - Polling elapsed > 60 s → show advisory text "Confirmation is taking longer than expected" below spinner
4. Track elapsed time via `useRef(Date.now())` on component mount; check on each poll tick
5. Render `PDFConfirmationStatus` in `BookingPage` confirmation state immediately after `appointmentId` is returned by `useBooking`

## Current Project State
```
frontend/
  src/
    pages/BookingPage.tsx  (booking form + confirmation state, from US_013)
    styles/variables.css
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/components/booking/PDFConfirmationStatus.tsx | Spinner → checkmark component polling confirmation status |
| MODIFY | frontend/src/pages/BookingPage.tsx | Render PDFConfirmationStatus in booking confirmation state |

## External References
- [TanStack Query — Dependent Queries](https://tanstack.com/query/latest/docs/framework/react/guides/dependent-queries)
- [wireframe-SCR-004-appointment-booking.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-004-appointment-booking.html)

## Build Commands
- Refer to [frontend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Booking completes → "Generating confirmation..." spinner immediately visible
- [ ] Simulate job status changing to "Sent" → component transitions to "Confirmation emailed ✓" checkmark
- [ ] Mock polling to exceed 60 s → advisory text appears below spinner without removing the spinner

## Implementation Checklist
- [ ] Create `PDFConfirmationStatus` component with React Query polling every 3 s; stop when `status === "Sent"` (AC-002)
- [ ] Render spinner + "Generating confirmation..." during `Queued | Processing` states (AC-002)
- [ ] Render green checkmark "Confirmation emailed ✓" when `status === "Sent"` (AC-002)
- [ ] Track elapsed polling time; show advisory text after 60 s without "Sent" (edge case)
- [ ] Mount `PDFConfirmationStatus` in `BookingPage` confirmation state immediately after successful booking (AC-002)
