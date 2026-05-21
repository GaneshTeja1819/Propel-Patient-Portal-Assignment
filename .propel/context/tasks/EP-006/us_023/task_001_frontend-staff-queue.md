# Task - TASK_001

## Requirement Reference
- **User Story:** us_023
- **Story Location:** .propel/context/tasks/EP-006/us_023/us_023.md
- **Acceptance Criteria:**
  - AC-001: Queue dashboard shows all today's appointments in chronological order; patient name, scheduled time, status badge; all visible without scroll at 1280 px
  - AC-002: Staff drags queue entry to new position; reorder saved; conflict warning shown but override allowed
  - AC-003: Staff removes patient from queue with mandatory reason; confirmation message shown
  - AC-004: Staff clicks "Mark Arrived"; appointment status badge updates to "Arrived"
- **Edge Cases:**
  - Tablet (768 px) → all columns visible; no horizontal scroll; status badges legible
  - Two Staff mark same patient arrived simultaneously → second attempt shows HTTP 409 "Patient already marked as arrived"
  - Staff removes patient already marked "Arrived" → confirmation warning "Patient has already arrived — confirm removal?"

---

## Design References [CONDITIONAL: UI Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-011-staff-queue.html |
| **Screen Spec** | SCR-011 |
| **UXR Requirements** | UXR-104 |
| **Design Tokens** | `--color-status-booked`, `--color-status-arrived`, `--color-status-cancelled` from variables.css |

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
| Frontend | React (SPA) | 18.x | TR-001 — Staff queue dashboard on SCR-011 |

---

## Task Overview
Build SCR-011, the same-day queue dashboard for Staff. The page loads all today's appointments via `GET /api/v1/queue/today` and renders them in a sortable table. Drag-to-reorder is implemented using the browser-native `Draggable` API (no external lib). A "Remove" action opens an inline reason dropdown (required). A "Mark Arrived" button calls the arrive endpoint optimistically and reflects the status badge change immediately. Status badge colours come from design tokens. The table is fully responsive at 768 px using CSS Grid with column stacking.

## Dependent Tasks
- `task_001_frontend-walkin-booking.md` (US_022) — `<RequireRole role="Staff" />` guard must exist
- `task_001_frontend-login.md` (US_010) — `AuthContext` must exist

## Impacted Components
- `frontend/src/pages/StaffQueuePage.tsx` — new SCR-011 Staff-only page
- `frontend/src/components/staff/QueueTable.tsx` — new sortable queue table
- `frontend/src/components/staff/QueueRow.tsx` — new row with drag handle, status badge, actions
- `frontend/src/components/staff/RemoveQueueModal.tsx` — inline reason form modal
- `frontend/src/hooks/useQueue.ts` — new hook: fetch today's queue + PATCH arrive/reorder/remove
- `frontend/src/styles/variables.css` — add status colour tokens

## Implementation Plan
1. Add `--color-status-booked`, `--color-status-arrived`, `--color-status-cancelled` to `variables.css`
2. Create `useQueue` hook: `GET /api/v1/queue/today` → returns `QueueEntry[]`; `markArrived(id)` → `PATCH /api/v1/queue/{id}/arrive`; `removeEntry(id, reason)` → `DELETE /api/v1/queue/{id}?reason=`; `reorder(id, newIndex)` → `PATCH /api/v1/queue/{id}/reorder`
3. Create `QueueRow.tsx`: drag handle (`draggable="true"`, `onDragStart/onDrop`); status badge using design token colour; "Mark Arrived" button (disabled if status ≠ "Booked"); "Remove" button opens `RemoveQueueModal`
4. Create `RemoveQueueModal.tsx`: required reason `<select>` (options: "No-show", "Error", "Patient request", "Other"); "Confirm removal" CTA; `aria-required="true"`; blocks submission if reason empty
5. Create `QueueTable.tsx`: manages drag state in local state (`draggedId`, `dropTargetId`); on drop: compute new index from DOM order; if `conflictDetected` (same time slot order as another patient) → show amber toast "Position conflicts with another appointment — override applied"; calls `useQueue.reorder`
6. Create `StaffQueuePage.tsx` at `/staff/queue`; wrapped in `<RequireRole role="Staff" />`; renders `QueueTable`; 768 px breakpoint: stacks columns, status badges remain visible
7. On HTTP 409 for arrive → inline status message "Patient already marked as arrived"

## Current Project State
```
frontend/
  src/
    pages/WalkInBookingPage.tsx  (from US_022)
    styles/variables.css
    App.tsx
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/pages/StaffQueuePage.tsx | SCR-011 Staff queue dashboard |
| CREATE | frontend/src/components/staff/QueueTable.tsx | Sortable queue table with drag-and-drop |
| CREATE | frontend/src/components/staff/QueueRow.tsx | Queue row with drag handle + action buttons |
| CREATE | frontend/src/components/staff/RemoveQueueModal.tsx | Required-reason removal modal |
| CREATE | frontend/src/hooks/useQueue.ts | Queue fetch + arrive / reorder / remove hooks |
| MODIFY | frontend/src/styles/variables.css | Add status colour tokens |
| MODIFY | frontend/src/App.tsx | Add /staff/queue route with Staff guard |

## External References
- [wireframe-SCR-011-staff-queue.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-011-staff-queue.html)
- [MDN Drag and Drop API](https://developer.mozilla.org/en-US/docs/Web/API/HTML_Drag_and_Drop_API)
- [WCAG 2.2 SC 2.1.1 Keyboard](https://www.w3.org/WAI/WCAG22/Understanding/keyboard.html)

## Build Commands
- Refer to [frontend build commands](.propel/build/)

## Implementation Validation Strategy
- [x] Queue loads with all today's appointments in chronological order; status badges coloured correctly
- [x] Drag row to new position → order updates; conflict toast appears when position conflicts
- [x] Click "Remove" → reason required; confirm → row removed; confirmation shown
- [x] Click "Mark Arrived" → status badge changes to "Arrived" immediately (optimistic); HTTP 409 → inline "already arrived" message
- [x] Render at 768 px → no horizontal scroll; columns visible

## Implementation Checklist
- [x] Add status colour tokens to `variables.css` (AC-001)
- [x] Build `QueueRow` with drag handle; status badge; "Mark Arrived" disabled when not "Booked" (AC-001, AC-004)
- [x] Build `QueueTable` with drag-reorder; conflict warning toast on slot overlap (AC-002)
- [x] Build `RemoveQueueModal` with mandatory reason field; block submit if empty (AC-003)
- [x] `useQueue` hook: fetch today's queue; PATCH arrive (HTTP 409 → inline message); PATCH reorder; DELETE remove (AC-001–AC-004)
- [x] Responsive 768 px layout; no horizontal scroll (UXR-104 edge case)
- [x] Override-arrived removal guard: confirmation warning if status already "Arrived" (edge case)
