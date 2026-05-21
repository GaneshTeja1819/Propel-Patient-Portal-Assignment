# Task - TASK_002

## Requirement Reference
- **User Story:** us_024
- **Story Location:** .propel/context/tasks/EP-007/us_024/us_024.md
- **Acceptance Criteria:**
  - AC-001: Admin creates, updates, deactivates, changes role of any user account via API
  - AC-002: Role change invalidates all active Redis session tokens for the affected user; next request returns HTTP 401
  - AC-003: Admin cannot deactivate their own account; HTTP 422 returned; no audit entry written
  - AC-004: Admin downgrade confirmation handled on frontend only; backend accepts the role change unconditionally once received
  - AC-005: All admin actions produce immutable audit entries: actorRole="Admin", actorId, actionType, targetEntity="User", targetId, timestamp
- **Edge Cases:**
  - Deactivating a user in an active session → session invalidated at Redis layer immediately; in-flight requests return HTTP 401
  - Admin session expires mid-action → action not committed; JWT expiry returns HTTP 401

---

## Design References [CONDITIONAL: UI Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | No |
| **Figma URL** | N/A |
| **Wireframe Status** | N/A |
| **Wireframe Type** | N/A |
| **Wireframe Path/URL** | N/A |
| **Screen Spec** | N/A |
| **UXR Requirements** | N/A |
| **Design Tokens** | N/A |

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
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | TR-002 — Admin user management endpoints |
| Caching | Upstash Redis | Serverless | NFR-002 — session invalidation on role change or deactivation |
| Database | PostgreSQL via Supabase | 15 | TR-003 — User entity; role and status columns |

---

## Task Overview
Implement the Admin user management API under `[Authorize(Policy = "AdminPolicy")]`: `GET /api/v1/admin/users?q=` (search), `POST /api/v1/admin/users` (create), `PATCH /api/v1/admin/users/{id}` (update profile), `PATCH /api/v1/admin/users/{id}/deactivate` (deactivate with self-guard), `PATCH /api/v1/admin/users/{id}/role` (role change). Role change and deactivation invalidate all Redis session keys matching `session:{userId}:*`. All actions produce immutable audit entries with `actorRole = "Admin"`.

## Dependent Tasks
- `task_001_backend-jwt-auth.md` (US_007) — `AdminPolicy`, `IRedisSessionStore` must be registered
- `task_001_backend-phi-encryption.md` (US_006) — `IAuditLogService` must be registered

## Impacted Components
- `backend/src/UPACIP.API/Controllers/AdminController.cs` — new controller
- `backend/src/UPACIP.Application/Commands/Admin/UpdateUserCommand.cs` — new command
- `backend/src/UPACIP.Application/Handlers/Admin/UpdateUserHandler.cs` — new handler
- `backend/src/UPACIP.Application/Handlers/Admin/ChangeRoleHandler.cs` — new handler
- `backend/src/UPACIP.Application/Handlers/Admin/DeactivateUserHandler.cs` — new handler (self-guard)

## Implementation Plan
1. Implement `GET /api/v1/admin/users?q=`: ILIKE search on `firstName || ' ' || lastName` or email; return list of `UserSummaryDto`
2. Implement `POST /api/v1/admin/users`: same validation as self-registration (password complexity, unique email); hash password with BCrypt; create `User` with role; write `USER_CREATED` audit (`actorRole = "Admin"`)
3. Implement `PATCH /api/v1/admin/users/{id}`: update `firstName`, `lastName`, `email`, `phoneNumber`; write `USER_UPDATED` audit
4. Implement `PATCH /api/v1/admin/users/{id}/deactivate`:
   - Self-guard: if `id == currentAdminId` → HTTP 422 "You cannot deactivate your own account"; no audit entry
   - Set `User.status = "Inactive"`, `User.deactivatedAt = UtcNow`
   - Invalidate Redis sessions: `IRedisSessionStore.InvalidateAllSessionsForUserAsync(userId)` — scans keys `session:{userId}:*` and deletes
   - Write `USER_DEACTIVATED` audit entry
5. Implement `PATCH /api/v1/admin/users/{id}/role`:
   - Accept `{ newRole: string }` body; validate newRole ∈ ["Patient", "Staff", "Admin"]
   - Update `User.role`
   - Invalidate Redis sessions: `IRedisSessionStore.InvalidateAllSessionsForUserAsync(userId)` (AC-002)
   - Write `ROLE_CHANGED` audit entry: include `oldRole` and `newRole` fields
6. All audit entries: `targetEntity = "User"`, `targetId = userId`, `actorRole = "Admin"`, `actorId = adminId`; stored without UPDATE/DELETE permission at DB row level

## Current Project State
```
backend/
  src/
    UPACIP.Infrastructure/Auth/JwtAuthService.cs  (AdminPolicy registered)
    UPACIP.Infrastructure/Auth/RedisSessionStore.cs  (from US_007)
    UPACIP.Domain/Entities/User.cs
    UPACIP.Infrastructure/Audit/AuditLogService.cs
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/UPACIP.API/Controllers/AdminController.cs | Admin user management endpoints |
| CREATE | backend/src/UPACIP.Application/Commands/Admin/UpdateUserCommand.cs | Update user profile command |
| CREATE | backend/src/UPACIP.Application/Handlers/Admin/UpdateUserHandler.cs | Profile update handler + audit |
| CREATE | backend/src/UPACIP.Application/Handlers/Admin/ChangeRoleHandler.cs | Role change + Redis invalidation + audit |
| CREATE | backend/src/UPACIP.Application/Handlers/Admin/DeactivateUserHandler.cs | Deactivate with self-guard + Redis + audit |
| MODIFY | backend/src/UPACIP.Infrastructure/Auth/RedisSessionStore.cs | Add InvalidateAllSessionsForUserAsync |

## External References
- [OWASP A01 — Access Control](https://owasp.org/Top10/A01_2021-Broken_Access_Control/)
- [Redis — SCAN + DEL pattern](https://redis.io/commands/scan/)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] PATCH /role → User.role updated; Redis sessions for that user invalidated; next request by user → HTTP 401
- [ ] PATCH /deactivate own account → HTTP 422; no audit entry; no session change
- [ ] PATCH /deactivate another user in active session → Redis sessions deleted immediately; HTTP 401 on next request
- [ ] All mutating admin actions produce immutable audit entries with actorRole="Admin"

## Implementation Checklist
- [x] Implement admin user search endpoint with case-insensitive ILIKE (AC-001)
- [x] Implement user create/update with BCrypt password hashing; unique email validation (AC-001)
- [x] Self-deactivation guard: HTTP 422; no audit entry (AC-003)
- [x] Role change + `InvalidateAllSessionsForUserAsync`; write `ROLE_CHANGED` audit with oldRole/newRole (AC-002, AC-005)
- [x] Deactivation + `InvalidateAllSessionsForUserAsync`; write `USER_DEACTIVATED` audit (AC-001, edge case)
- [x] All endpoints `[Authorize(Policy = "AdminPolicy")]`; immutable audit entries (no UPDATE/DELETE) (AC-005)
