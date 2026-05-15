# Task - TASK_001

## Requirement Reference
- **User Story:** us_011
- **Story Location:** .propel/context/tasks/EP-001/us_011/us_011.md
- **Acceptance Criteria:**
  - AC-001: Session terminates after 15 min inactivity; SPA redirects to login with "Session expired" message
  - AC-002: Any interaction (click, keypress, scroll) resets inactivity timer to 0; no modal appears
  - AC-003: Modal appears at 13 min; countdown from 120 s; `aria-live="assertive"` announces "Session expiring in 2 minutes"; "Stay logged in" CTA is visible and focusable
  - AC-004: "Stay logged in" resets timer, closes modal, session remains active
  - AC-005: If modal is ignored, countdown reaches 0; modal transitions to "Session expired"; redirect within 1 second
- **Edge Cases:**
  - Multiple tabs → session expiry in one tab; all tabs redirect on next API call
  - Long-running JS task → countdown uses `requestAnimationFrame` or Web Worker timer; not blocked by synchronous JS
  - Screen reader active when modal appears → `aria-live="assertive"` announcement without requiring focus move

---

## Design References [CONDITIONAL: UI Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | PENDING |
| **Wireframe Type** | N/A |
| **Wireframe Path/URL** | N/A — global overlay; no dedicated wireframe |
| **Screen Spec** | All authenticated screens |
| **UXR Requirements** | UXR-205, UXR-503 |
| **Design Tokens** | `--color-*`, `--elevation-*`, `--spacing-*` from variables.css (modal overlay) |

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
| Frontend | React (SPA) | 18.x | TR-001, NFR-006 — global session timeout component mounted inside AuthProvider |
| Frontend | Web Worker / requestAnimationFrame | Browser API | UXR-503 — timer must not be blocked by synchronous JS |

---

## Task Overview
Implement the `SessionTimeoutManager` component mounted globally inside `AuthProvider`. An inactivity timer tracks time since the last user interaction (click, keypress, scroll). At 13 minutes (780 s) of inactivity the `SessionTimeoutModal` renders with a 120-second countdown. The countdown uses `requestAnimationFrame` to remain accurate even during synchronous JS execution. Clicking "Stay logged in" calls `POST /api/v1/auth/refresh` and resets the timer. If the countdown reaches 0, the SPA calls `POST /api/v1/auth/logout`, invalidates the local auth state, and redirects to `/login?reason=expired`. The modal uses `role="dialog"` with `aria-live="assertive"` on the countdown region.

## Dependent Tasks
- `task_001_frontend-login.md` (US_010) — `AuthContext` must exist; `logout()` and refresh call needed
- `task_001_backend-jwt-auth.md` (US_007) — `POST /api/v1/auth/refresh` endpoint must be available

## Impacted Components
- `frontend/src/components/session/SessionTimeoutModal.tsx` — new modal component
- `frontend/src/hooks/useInactivityTimer.ts` — new inactivity timer hook
- `frontend/src/context/AuthContext.tsx` — add `refreshSession()` action; mount `SessionTimeoutManager`

## Implementation Plan
1. Create `useInactivityTimer(timeoutMs, warningMs)` hook: attaches `click`, `keydown`, `scroll` event listeners on `window`; uses `requestAnimationFrame` loop to tick the elapsed counter; returns `{ phase: 'active' | 'warning' | 'expired', secondsRemaining }`
2. Create `SessionTimeoutModal.tsx`: renders only when `phase === 'warning'`; `role="dialog"`, `aria-modal="true"`, `aria-labelledby="session-timeout-title"`; countdown region has `aria-live="assertive"` and `aria-atomic="true"` announcing "Session expiring in {secondsRemaining} seconds"
3. "Stay logged in" button: calls `AuthContext.refreshSession()` which calls `POST /api/v1/auth/refresh`; on success resets `useInactivityTimer`; on HTTP 503 show "Refresh failed — you will be logged out in {n} seconds"
4. When `phase === 'expired'`: transition modal text to "Session expired"; call `AuthContext.logout()`; after 1 second navigate to `/login?reason=expired`
5. Mount `SessionTimeoutManager` inside `AuthProvider` wrapping all authenticated routes only; do not mount on `/login` or `/register`
6. `LoginPage` reads `?reason=expired` query param and displays "Session expired. Please log in again." status message

## Current Project State
```
frontend/
  src/
    context/AuthContext.tsx  (login/logout from US_010)
    pages/LoginPage.tsx
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/hooks/useInactivityTimer.ts | Timer hook tracking inactivity phase and countdown |
| CREATE | frontend/src/components/session/SessionTimeoutModal.tsx | Modal with aria-live countdown and "Stay logged in" CTA |
| MODIFY | frontend/src/context/AuthContext.tsx | Add refreshSession(); mount SessionTimeoutManager for auth routes |
| MODIFY | frontend/src/pages/LoginPage.tsx | Read `?reason=expired` and display session expired message |

## External References
- [WCAG 2.2 SC 2.2.1 Timing Adjustable](https://www.w3.org/WAI/WCAG22/Understanding/timing-adjustable.html)
- [MDN requestAnimationFrame](https://developer.mozilla.org/en-US/docs/Web/API/Window/requestAnimationFrame)
- [ARIA dialog pattern](https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/)
- [aria-live regions (MDN)](https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/Attributes/aria-live)

## Build Commands
- Refer to [frontend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Simulate 13 min inactivity (mock timer to 780 s); confirm modal renders with countdown
- [ ] Click anywhere while timer is at 700 s; confirm modal does not appear and timer resets
- [ ] Click "Stay logged in" in modal; confirm modal closes and countdown resets
- [ ] Let countdown reach 0; confirm redirect to `/login?reason=expired` within 1 s
- [ ] Screen reader test: `aria-live="assertive"` region announces countdown without focus move

## Implementation Checklist
- [ ] Implement `useInactivityTimer` with `requestAnimationFrame` tick; three phases: active / warning (13 min) / expired (15 min) (AC-001, AC-002, AC-003, AC-005)
- [ ] Build `SessionTimeoutModal` with `role="dialog"`, `aria-live="assertive"` countdown region; "Stay logged in" CTA focusable via Tab (AC-003)
- [ ] Wire "Stay logged in" to `refreshSession()` → `POST /api/v1/auth/refresh`; reset timer on success (AC-004)
- [ ] Wire `phase === 'expired'` → logout() → navigate `/login?reason=expired` within 1 s (AC-001, AC-005)
- [ ] Mount `SessionTimeoutManager` inside `AuthProvider` for authenticated routes only (AC-001)
- [ ] Add `?reason=expired` banner on `LoginPage` (AC-001)
- [ ] Verify `aria-live="assertive"` fires announcement without focus movement (AC-003 screen reader edge case)
- [ ] Verify modal rendering is not blocked by synchronous JS tasks (AC-003 edge case)
