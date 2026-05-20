# Task - TASK_001

## Requirement Reference
- **User Story:** us_016
- **Story Location:** .propel/context/tasks/EP-003/us_016/us_016.md
- **Acceptance Criteria:**
  - AC-001: Unavailable slots show "Register Preferred" option; one preferred slot selectable; visually distinct from available selection; booking continues uninterrupted
  - AC-003: Skipping preferred slot → booking completes normally; no error shown
  - AC-004: Re-selecting the same preferred slot replaces the previous selection (UI state)
- **Edge Cases:**
  - Preferred slot becomes available before booking confirmed → slot may shift to "Available"; UI handles the state transition gracefully without clearing the preferred selection
  - Patient selects preferred slot = booked slot → validate client-side; clear preferred selection; show inline validation "Preferred slot must differ from your booked slot"

---

## Design References [CONDITIONAL: UI Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-006-preferred-slot-confirm.html |
| **Screen Spec** | SCR-004, SCR-006 |
| **UXR Requirements** | N/A |
| **Design Tokens** | `--color-slot-preferred`, `--color-slot-selected`, `--color-slot-unavailable` from variables.css |

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
| Frontend | React (SPA) | 18.x | TR-001 — preferred slot selection state within SlotGrid |

---

## Task Overview
Extend `SlotGrid` (from US_012) so that unavailable slots display a secondary "Register Preferred" button below the card. The patient can select exactly one unavailable slot as preferred; the card is visually distinguished with a `--color-slot-preferred` CSS token. The preferred selection is tracked in local state (`preferredSlotId`). A client-side guard prevents the patient from registering the same slot they are actively booking. On the booking confirmation screen (SCR-006 / booking confirmation state on SCR-004) an acknowledgement message shows "Preferred slot registered" when a preferred slot was selected. The `preferredSlotId` is passed alongside the booking form payload to the booking API (wired in US_016 task_002).

## Dependent Tasks
- `task_001_frontend-slot-grid.md` (US_012) — `SlotGrid` and `SlotCard` components must exist
- `task_001_frontend-booking-form.md` (US_013) — `BookingForm` and booking flow must exist; `preferredSlotId` field added to form payload

## Impacted Components
- `frontend/src/components/booking/SlotCard.tsx` — add "Register Preferred" action for unavailable cards
- `frontend/src/components/booking/SlotGrid.tsx` — manage `preferredSlotId` state; pass to `SlotCard`
- `frontend/src/hooks/useBooking.ts` — include `preferredSlotId` in POST payload
- `frontend/src/pages/BookingPage.tsx` — render preferred-slot acknowledgement in confirmation state
- `frontend/src/styles/variables.css` — add `--color-slot-preferred` token

## Implementation Plan
1. Add `--color-slot-preferred` CSS token to `variables.css` (amber/teal distinct from available green and selected blue)
2. Extend `SlotCard.tsx` props: add `isPreferred: boolean` and `onRegisterPreferred?: (slotId: string) => void`; when `slot.status === "Unavailable"` render a secondary "Register Preferred" button inside the card
3. Extend `SlotGrid.tsx`: add `preferredSlotId` state via `useState<string | null>(null)`; pass to each `SlotCard`; client-side guard: if `preferredSlotId === selectedSlotId` → clear `preferredSlotId` and show inline validation message "Preferred slot must differ from your booked slot"
4. Extend `useBooking.ts`: accept optional `preferredSlotId` in args; include in `POST /api/v1/appointments` body as nullable field
5. In `BookingPage.tsx` confirmation state: if `preferredSlotId` was set → show "Preferred slot registered — you'll be notified if it becomes available" acknowledgement badge
6. Ensure "Register Preferred" is keyboard-accessible: `role="button"`, `aria-label="Register as preferred slot for [date/time]"`

## Current Project State
```
frontend/
  src/
    components/booking/SlotGrid.tsx   (from US_012)
    components/booking/SlotCard.tsx   (from US_012)
    hooks/useBooking.ts               (from US_013)
    pages/BookingPage.tsx             (from US_013)
    styles/variables.css
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | frontend/src/components/booking/SlotCard.tsx | Add "Register Preferred" button for unavailable slots |
| MODIFY | frontend/src/components/booking/SlotGrid.tsx | Manage preferredSlotId state; validate ≠ selectedSlotId |
| MODIFY | frontend/src/hooks/useBooking.ts | Include preferredSlotId in POST payload |
| MODIFY | frontend/src/pages/BookingPage.tsx | Preferred slot acknowledgement in confirmation state |
| MODIFY | frontend/src/styles/variables.css | Add --color-slot-preferred token |

## External References
- [wireframe-SCR-006-preferred-slot-confirm.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-006-preferred-slot-confirm.html)
- [WCAG 2.2 SC 4.1.2 Name, Role, Value](https://www.w3.org/WAI/WCAG22/Understanding/name-role-value.html)

## Build Commands
- Refer to [frontend build commands](.propel/build/)

## Implementation Validation Strategy
- [x] Unavailable slot card shows "Register Preferred" button; clicking it applies `--color-slot-preferred` styling
- [x] Selecting the same slot as preferred and booked → inline validation clears preferred selection
- [x] Skip preferred slot → booking completes; no preferred acknowledgement shown in confirmation
- [x] Preferred slot selected → confirmation state shows acknowledgement text

## Implementation Checklist
- [x] Add `--color-slot-preferred` CSS token to `variables.css` (AC-001)
- [x] Extend `SlotCard` with "Register Preferred" button for unavailable cards; keyboard-accessible (AC-001)
- [x] Manage `preferredSlotId` state in `SlotGrid`; only one preferred selectable at a time (AC-001, AC-004)
- [x] Client-side guard: preferred ≠ booked slot; inline validation on mismatch (edge case)
- [x] Include `preferredSlotId` (nullable) in `useBooking` POST payload (AC-002 enabler)
- [x] Render preferred-slot acknowledgement in booking confirmation state (AC-002)
- [x] Booking without preferred slot proceeds normally; no error (AC-003)
