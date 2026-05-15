# Task - TASK_001

## Requirement Reference
- **User Story:** us_021
- **Story Location:** .propel/context/tasks/EP-005/us_021/us_021.md
- **Acceptance Criteria:**
  - AC-004: Calendar sync failure (after all retries) → amber dismissible Toast "Calendar sync failed — your appointment is still confirmed"; appointment confirmation visible and unobstructed
  - AC-005: OAuth consent denied → booking continues; no CalendarSync record; no error; patient informed sync is optional
- **Edge Cases:**
  - Patient grants consent for both Google and Outlook → two independent CalendarSync records; failure in one does not block the other

---

## Design References [CONDITIONAL: UI Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-016-calendar-oauth.html |
| **Screen Spec** | SCR-016 |
| **UXR Requirements** | UXR-604 |
| **Design Tokens** | `--color-warning`, `--color-success` from variables.css; Toast uses `--color-warning` amber |

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
| Frontend | React (SPA) | 18.x | TR-001 — OAuth consent screen on SCR-016 |

---

## Task Overview
Build the SCR-016 calendar sync screen. It is presented as an optional step after booking confirmation (or as a standalone settings action). The screen shows two OAuth consent buttons — "Connect Google Calendar" and "Connect Outlook Calendar" — each opening the provider's consent page in a pop-up or redirect. On return from consent the frontend calls `POST /api/v1/calendar-sync/callback` (task_002) with the auth code. If sync fails the amber `Toast` component (from US_013) is rendered with the non-blocking message. Denied consent shows an "Optional — you can enable calendar sync later in Settings" advisory without error.

## Dependent Tasks
- `task_001_frontend-booking-form.md` (US_013) — `Toast` component must exist
- `task_001_frontend-login.md` (US_010) — `AuthContext` must exist

## Impacted Components
- `frontend/src/pages/CalendarSyncPage.tsx` — new SCR-016 OAuth consent page
- `frontend/src/hooks/useCalendarSync.ts` — new hook: OAuth redirect + callback + sync status polling
- `frontend/src/pages/BookingPage.tsx` — add optional "Sync to calendar" CTA after confirmation state

## Implementation Plan
1. Create `CalendarSyncPage.tsx` at `/calendar-sync`:
   - Two CTA buttons: "Connect Google Calendar" and "Connect Outlook Calendar"
   - Each button navigates to `GET /api/v1/calendar-sync/oauth-url?provider=Google|Outlook`; the backend returns the OAuth authorization URL; open in same tab (redirect flow)
   - After OAuth redirect back: URL contains `code` and `state` query params; call `POST /api/v1/calendar-sync/callback`; show loading state during callback
   - On success: "Calendar connected ✓" with `--color-success`
   - On failure (backend sync error): render amber `Toast` "Calendar sync failed — your appointment is still confirmed"; appointment confirmation remains fully visible (AC-004, UXR-604)
   - "Skip for now" link exits the flow without error
2. Create `useCalendarSync` hook: `initiateOAuth(provider)` calls backend for OAuth URL; `handleCallback(code, state)` calls `POST /api/v1/calendar-sync/callback`; returns `{ status: 'idle' | 'loading' | 'synced' | 'failed' }`; `failed` state triggers Toast display
3. In `BookingPage.tsx` confirmation state: add "Sync to calendar (optional)" CTA that navigates to `/calendar-sync`; not shown until appointment is confirmed; booking confirmation unobstructed if user skips (AC-004, AC-005)
4. Denied consent flow: OAuth callback receives `error=access_denied`; frontend shows advisory "Calendar sync is optional — you can enable it later in Profile Settings"; no Toast; no error state

## Current Project State
```
frontend/
  src/
    pages/BookingPage.tsx         (from US_013)
    components/common/Toast.tsx   (from US_013)
    context/AuthContext.tsx
    App.tsx
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/pages/CalendarSyncPage.tsx | SCR-016 OAuth consent + callback result screen |
| CREATE | frontend/src/hooks/useCalendarSync.ts | OAuth redirect, callback, sync status |
| MODIFY | frontend/src/pages/BookingPage.tsx | Add optional "Sync to calendar" CTA in confirmation state |
| MODIFY | frontend/src/App.tsx | Add /calendar-sync route |

## External References
- [wireframe-SCR-016-calendar-oauth.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-016-calendar-oauth.html)
- [OAuth 2.0 Authorization Code Flow](https://datatracker.ietf.org/doc/html/rfc6749#section-4.1)
- [Google Identity OAuth 2.0 for Web](https://developers.google.com/identity/protocols/oauth2/web-server)

## Build Commands
- Refer to [frontend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Click "Connect Google Calendar" → OAuth redirect opens; after consent → callback called; "Calendar connected ✓" shown
- [ ] Deny OAuth consent → advisory "Calendar sync is optional" shown; no error Toast; no CalendarSync record
- [ ] Force sync failure from backend → amber Toast "Calendar sync failed — your appointment is still confirmed"; appointment confirmation visible

## Implementation Checklist
- [ ] Build `CalendarSyncPage` with Google + Outlook consent buttons; redirect flow (AC-001)
- [ ] Handle `error=access_denied` OAuth callback → show optional advisory; no error (AC-005)
- [ ] Show amber Toast on sync failure; appointment confirmation unobstructed (AC-004, UXR-604)
- [ ] `useCalendarSync` hook drives OAuth initiation and callback POST (AC-001)
- [ ] Add optional "Sync to calendar" CTA in `BookingPage` confirmation state; bookable without it (AC-005)
- [ ] Add `/calendar-sync` route to `App.tsx` (AC-001)
