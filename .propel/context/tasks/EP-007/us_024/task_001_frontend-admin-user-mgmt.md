# Task - TASK_001

## Requirement Reference
- **User Story:** us_024
- **Story Location:** .propel/context/tasks/EP-007/us_024/us_024.md
- **Acceptance Criteria:**
  - AC-001: All actions (create, update, deactivate, role change) completed in ≤ 2 screen transitions; no separate page per action
  - AC-003: Admin cannot deactivate their own account; blocked with inline message; no audit entry written for blocked action
  - AC-004: Downgrade from Admin role → confirmation dialog "This will revoke Admin access for [username] — confirm?"; role change proceeds only after explicit confirmation
- **Edge Cases:**
  - No users match search → empty state "No users found matching your search"; no error
  - Admin session expires mid-action → action not saved; redirect to login; dashboard state restored on re-auth

---

## Design References [CONDITIONAL: UI Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-015-admin-user-management.html |
| **Screen Spec** | SCR-015 |
| **UXR Requirements** | UXR-105 |
| **Design Tokens** | `--color-error`, `--color-warning`, `--color-primary` from variables.css |

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
| Frontend | React (SPA) | 18.x | TR-001 — Admin user management on SCR-015 |

---

## Task Overview
Build SCR-015 — the Admin user management dashboard. The page uses a two-panel layout: left panel is the search/user list; right panel is the user detail/action form rendered inline (no full navigation). All actions (update profile, deactivate, role change) happen within the right panel (≤ 2 screen transitions: dashboard → user detail). An Admin-downgrade confirmation modal appears before any Admin → Staff/Patient role change. A self-deactivation guard disables the deactivate button for the Admin's own account with an inline tooltip.

## Dependent Tasks
- `task_001_frontend-login.md` (US_010) — `AuthContext` with Admin role guard must exist

## Impacted Components
- `frontend/src/pages/AdminUserManagementPage.tsx` — new SCR-015 Admin-only page
- `frontend/src/components/admin/UserSearchPanel.tsx` — new user search + list component
- `frontend/src/components/admin/UserDetailPanel.tsx` — new inline user detail/action form
- `frontend/src/components/admin/RoleDowngradeConfirmModal.tsx` — new role downgrade confirmation dialog
- `frontend/src/hooks/useAdminUsers.ts` — new hook: search, get, update, deactivate, role change

## Implementation Plan
1. Create `AdminUserManagementPage.tsx` at `/admin/users`; guard with `<RequireRole role="Admin" />`; two-panel CSS Grid layout (300 px left / flex right)
2. Create `UserSearchPanel.tsx`: debounced search input (300 ms) calling `GET /api/v1/admin/users?q=`; renders a scrollable list of user rows; clicking a row sets `selectedUserId` and renders `UserDetailPanel`; empty state "No users found matching your search" when no results
3. Create `UserDetailPanel.tsx`: shows user profile fields; actions — "Edit" (inline field editing with save), "Deactivate", "Change Role" (dropdown); self-deactivation guard: if `user.id === currentAdmin.id` → disable "Deactivate" button with `title="You cannot deactivate your own account"` tooltip; AC-003
4. Role change: if current role is "Admin" and new role is "Staff" or "Patient" → open `RoleDowngradeConfirmModal` before calling the API (AC-004)
5. Create `RoleDowngradeConfirmModal.tsx`: message "This will revoke Admin access for [username] — confirm?"; "Confirm" CTA calls `useAdminUsers.changeRole`; "Cancel" closes modal; role unchanged on cancel
6. `useAdminUsers` hook: `searchUsers(q)`, `updateUser(id, fields)`, `deactivateUser(id)`, `changeRole(id, newRole)`; on 403 redirect to `/admin/users`; on session expiry (401) redirect to `/login?redirect=/admin/users`

## Current Project State
```
frontend/
  src/
    context/AuthContext.tsx  (from US_010)
    styles/variables.css
    App.tsx
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/pages/AdminUserManagementPage.tsx | SCR-015 Admin user management page |
| CREATE | frontend/src/components/admin/UserSearchPanel.tsx | Debounced user search + list |
| CREATE | frontend/src/components/admin/UserDetailPanel.tsx | Inline user detail + action form |
| CREATE | frontend/src/components/admin/RoleDowngradeConfirmModal.tsx | Admin downgrade confirmation dialog |
| CREATE | frontend/src/hooks/useAdminUsers.ts | Admin CRUD user hooks |
| MODIFY | frontend/src/App.tsx | Add /admin/users route with Admin guard |

## External References
- [wireframe-SCR-015-admin-user-management.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-015-admin-user-management.html)
- [WCAG 2.2 SC 3.3.4 Error Prevention](https://www.w3.org/WAI/WCAG22/Understanding/error-prevention-legal-financial-data.html)

## Build Commands
- Refer to [frontend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Search for user → select → detail panel renders inline; total transitions ≤ 2 from dashboard
- [ ] Attempt to deactivate own Admin account → button disabled; tooltip visible
- [ ] Change another Admin's role to Staff → confirmation modal appears; cancel → role unchanged; confirm → role updated
- [ ] Search with no matches → empty state shown; no error thrown

## Implementation Checklist
- [x] Build `AdminUserManagementPage` with two-panel layout; `<RequireRole role="Admin" />` (AC-001)
- [x] Build `UserSearchPanel` with 300 ms debounce; empty state on no results (AC-001, edge case)
- [x] Build `UserDetailPanel`: inline actions; self-deactivation guard disables button with tooltip (AC-001, AC-003)
- [x] Build `RoleDowngradeConfirmModal`; only opens when Admin → lower role; cancel leaves role unchanged (AC-004)
- [x] `useAdminUsers` hook; on 401 redirect to login with `?redirect=/admin/users` (edge case)
- [x] All actions complete within 2 screen transitions (no separate page per action) (AC-001, UXR-105)
