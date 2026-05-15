# Task - TASK_001

## Requirement Reference
- **User Story:** us_010
- **Story Location:** .propel/context/tasks/EP-001/us_010/us_010.md
- **Acceptance Criteria:**
  - AC-001: Successful login → JWT cookie issued → redirect to role-specific dashboard (SCR-003/011/015)
  - AC-002: Invalid credentials → "Invalid email or password" with no field-level disclosure
  - AC-005: All form fields have visible labels; inline errors linked via `aria-describedby`; keyboard operable with visible focus ring
- **Edge Cases:**
  - Browser blocks cookies → "Cookies required" message displayed
  - Email case-insensitive comparison (User@Example.com === user@example.com)

---

## Design References [CONDITIONAL: UI Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-001-login.html |
| **Screen Spec** | SCR-001 |
| **UXR Requirements** | UXR-203, UXR-204 |
| **Design Tokens** | `--color-*`, `--spacing-*`, `--font-*` from variables.css |

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
| Frontend | React (SPA) | 18.x | TR-001, NFR-010 — login page component |
| Frontend | React Hook Form | Latest stable | NFR-010 — form state management with accessible error wiring |
| Frontend | React Router | v7 | NFR-010 — role-based programmatic navigation |

---

## Task Overview
Build the `LoginPage` component for SCR-001. The form captures email and password. On successful API response the React SPA reads the `role` value from the response body and navigates to the appropriate dashboard (Patient → `/dashboard`, Staff → `/staff/queue`, Admin → `/admin/users`). All inputs are accessible (visible labels, `aria-describedby` errors, keyboard operable). If the browser blocks cookies, the SPA detects the missing session and shows a "Cookies required" banner.

## Dependent Tasks
- `task_001_frontend-scaffold.md` (US_001) — design token system and routing baseline must exist

## Impacted Components
- `frontend/src/pages/LoginPage.tsx` — new login page route
- `frontend/src/components/auth/LoginForm.tsx` — new form component
- `frontend/src/hooks/useLogin.ts` — new hook wrapping login API call and role-redirect logic
- `frontend/src/context/AuthContext.tsx` — new auth context storing role and auth state

## Implementation Plan
1. Create `AuthContext.tsx` with `{ role, isAuthenticated, login(), logout() }` — `login()` calls `POST /api/v1/auth/login` and stores the decoded role in context state
2. Create `LoginPage.tsx` at `/login` route; use `LoginForm` and `AuthContext`
3. Build `LoginForm.tsx` with React Hook Form: fields `email` and `password`; each has `<label htmlFor>`, matching `id`, and `aria-describedby` pointing to error paragraph
4. On successful login (HTTP 200), read `role` from response body JSON; navigate with React Router: `Patient` → `/dashboard`, `Staff` → `/staff/queue`, `Admin` → `/admin/users`
5. On HTTP 401 → display "Invalid email or password" as a form-level error (not field-level); do not indicate which field is wrong
6. On HTTP 423 (locked) → display "Account temporarily locked"
7. After successful login, attempt to call a protected endpoint; if it fails due to missing cookie, display a `CookiesRequired` banner component
8. Run axe-core on `/login`; confirm zero label and aria violations

## Current Project State
```
frontend/
  src/
    styles/variables.css  (from US_001)
    pages/RegistrationPage.tsx  (from US_009)
    App.tsx
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/pages/LoginPage.tsx | SCR-001 login page route |
| CREATE | frontend/src/components/auth/LoginForm.tsx | Accessible login form |
| CREATE | frontend/src/hooks/useLogin.ts | Login API call + role-based redirect |
| CREATE | frontend/src/context/AuthContext.tsx | Auth state (role, isAuthenticated, login, logout) |
| MODIFY | frontend/src/App.tsx | Add `/login` route; wrap app in AuthProvider |

## External References
- [React Router v7 Navigation](https://reactrouter.com/6.28.0/hooks/use-navigate)
- [WCAG 2.2 SC 3.3.1 Error Identification](https://www.w3.org/WAI/WCAG22/Understanding/error-identification.html)
- [wireframe-SCR-001-login.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-001-login.html)

## Build Commands
- Refer to [frontend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Login with valid Patient credentials → navigates to `/dashboard`
- [ ] Login with valid Staff credentials → navigates to `/staff/queue`
- [ ] Login with invalid credentials → form-level "Invalid email or password" shown; no field disclosure
- [ ] axe-core scan on `/login` reports zero violations

## Implementation Checklist
- [ ] Create `AuthContext` with role, isAuthenticated, login(), logout() (AC-001)
- [ ] Build `LoginForm.tsx` with email + password fields; visible labels, `aria-describedby` errors, keyboard operable (AC-005)
- [ ] On HTTP 200, read `role` from response; navigate to role-specific dashboard (AC-001)
- [ ] On HTTP 401, show generic "Invalid email or password" — no field-level disclosure (AC-002)
- [ ] On HTTP 423, show "Account temporarily locked" message (feeds AC in task_002)
- [ ] Detect missing cookie after login and show "Cookies required" banner (edge case)
- [ ] Run axe-core on `/login`; confirm zero label and aria violations (AC-005)
