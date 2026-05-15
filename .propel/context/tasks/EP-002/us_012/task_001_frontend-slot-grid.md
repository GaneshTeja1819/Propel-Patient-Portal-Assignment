# Task - TASK_001

## Requirement Reference
- **User Story:** us_012
- **Story Location:** .propel/context/tasks/EP-002/us_012/us_012.md
- **Acceptance Criteria:**
  - AC-001: Available slots rendered within 500 ms from Redis cache; slot cards show date, time, "Available" badge
  - AC-002: Slot state change reflected within 5 s without full page reload
  - AC-003: 4 columns at 1280 px; 2 columns at 768 px; 1 column at 375 px; no horizontal scroll
  - AC-004: Clicked slot transitions to "Selected" visual state within 200 ms before server response
  - AC-005: Empty state shows "No available slots — try another date" when all slots booked
- **Edge Cases:**
  - Redis unavailable → fallback to DB; "Showing live data" badge shown; response may exceed 100 ms
  - Slow connection → loading skeleton shown on mount before data arrives
  - Two patients select same slot → both show "Selected" briefly; losing patient sees slot revert to "Unavailable" with error toast (resolved in US_013)

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
| **UXR Requirements** | UXR-101, UXR-102, UXR-302, UXR-501 |
| **Design Tokens** | `--color-available`, `--color-selected`, `--spacing-*`, `--radius-*` from variables.css |

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
| Frontend | React (SPA) | 18.x | TR-001, NFR-010 — SlotGrid component |
| Frontend | React Query (TanStack Query) | Latest stable | NFR-002 — automatic 5-second refetch interval matching Redis TTL; stale-while-revalidate |

---

## Task Overview
Build the `SlotGrid` component for SCR-004 (appointment booking screen). The grid polls `GET /api/v1/slots` every 5 seconds via React Query (matching the Redis TTL), so slot state changes are reflected without a full reload. A loading skeleton renders on initial mount. Clicking a slot card triggers immediate optimistic visual feedback (CSS class swap within 200 ms). The grid is responsive: 4/2/1 columns at 1280/768/375 px via CSS Grid. An empty state message replaces the grid when no slots are available.

## Dependent Tasks
- `task_001_frontend-scaffold.md` (US_001) — design tokens and breakpoints must exist
- `task_001_frontend-login.md` (US_010) — `AuthContext` must exist for authenticated API calls

## Impacted Components
- `frontend/src/pages/BookingPage.tsx` — new SCR-004 page
- `frontend/src/components/booking/SlotGrid.tsx` — new slot grid component
- `frontend/src/components/booking/SlotCard.tsx` — new slot card component
- `frontend/src/components/booking/SlotGridSkeleton.tsx` — new loading skeleton
- `frontend/src/hooks/useSlots.ts` — new React Query hook polling /api/v1/slots

## Implementation Plan
1. Create `useSlots(date: string)` hook using `useQuery` with `refetchInterval: 5000`; returns `{ slots, isLoading, isError, isCachedData }` — `isCachedData` drives the "Showing live data" fallback badge
2. Create `SlotCard.tsx`: renders date, time, status badge (`Available` / `Unavailable` / `Selected`); on click fires `onSelect(slotId)` callback; CSS transition `background-color 200ms ease` for optimistic selection (AC-004)
3. Create `SlotGrid.tsx`: CSS Grid layout — `grid-template-columns` responsive via media queries (4/2/1 col); renders `SlotCardSkeleton` (4 placeholder cards) while `isLoading`; renders empty state when `slots.length === 0`; passes `selectedSlotId` as prop to each `SlotCard` for selected styling
4. Create `SlotGridSkeleton.tsx`: 4 grey placeholder cards with CSS animation pulse; replaces grid on initial mount
5. Create `BookingPage.tsx` at `/booking`; compose `SlotGrid` + booking form; wire selected slot state
6. Apply CSS token variables for card colours: `--color-slot-available`, `--color-slot-selected`, `--color-slot-unavailable`; add to `variables.css`
7. Verify responsive layout at 1280/768/375 px via browser dev tools
8. Add "Showing live data" badge when `isCachedData === false` (Redis fallback path)

## Current Project State
```
frontend/
  src/
    styles/variables.css  (from US_001)
    context/AuthContext.tsx  (from US_010)
    App.tsx
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/pages/BookingPage.tsx | SCR-004 booking page |
| CREATE | frontend/src/components/booking/SlotGrid.tsx | Responsive CSS Grid slot grid |
| CREATE | frontend/src/components/booking/SlotCard.tsx | Slot card with optimistic selection |
| CREATE | frontend/src/components/booking/SlotGridSkeleton.tsx | Loading skeleton (4 placeholder cards) |
| CREATE | frontend/src/hooks/useSlots.ts | React Query hook; 5 s refetch interval |
| MODIFY | frontend/src/styles/variables.css | Add slot colour tokens |
| MODIFY | frontend/src/App.tsx | Add `/booking` route |

## External References
- [TanStack Query (React Query) Docs](https://tanstack.com/query/latest)
- [CSS Grid MDN](https://developer.mozilla.org/en-US/docs/Web/CSS/CSS_grid_layout)
- [wireframe-SCR-004-appointment-booking.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-004-appointment-booking.html)

## Build Commands
- Refer to [frontend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Slot grid renders within 500 ms on initial load; React Query shows data from cache
- [ ] After 5 s a booked slot transitions to "Unavailable" without page reload (simulate by booking on another tab)
- [ ] Grid at 375/768/1280 px: correct column counts; no horizontal scroll
- [ ] Clicking a slot card triggers CSS "Selected" state within 200 ms (browser DevTools performance trace)

## Implementation Checklist
- [ ] Create `useSlots` hook with React Query `refetchInterval: 5000`; expose `isCachedData` flag (AC-001, AC-002)
- [ ] Build `SlotCard` with 200 ms CSS transition for optimistic "Selected" state on click (AC-004)
- [ ] Build `SlotGrid` with CSS Grid 4/2/1 column responsive layout via media queries (AC-003)
- [ ] Render `SlotGridSkeleton` while `isLoading`; replace with grid on data arrival (edge case)
- [ ] Render empty state "No available slots — try another date" when `slots.length === 0` (AC-005)
- [ ] Show "Showing live data" badge when `isCachedData === false` (Redis unavailable edge case)
- [ ] Add slot colour tokens to `variables.css` and apply in `SlotCard` styles (AC-001 badge colours)
- [ ] Verify no horizontal scroll at 375/768/1280 px breakpoints (AC-003)
