# Task - TASK_002

## Requirement Reference
- **User Story:** us_010
- **Story Location:** .propel/context/tasks/EP-001/us_010/us_010.md
- **Acceptance Criteria:**
  - AC-001: Valid credentials → JWT in HttpOnly cookie; login audit event written; role returned in response body
  - AC-002: Invalid password → "Invalid email or password" (generic); failed-attempt counter incremented by 1
  - AC-003: Deactivated account + valid credentials → "Invalid email or password" (generic); failed-attempt counter NOT incremented
  - AC-004: Failed-attempt threshold reached → account temporarily locked; notification email sent via Hangfire; subsequent attempts return "Account temporarily locked"
- **Edge Cases:**
  - Network retry during login → session idempotency; only one JWT issued per active session
  - Email comparison case-insensitive

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
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | TR-002 — login endpoint; lockout logic |
| Backend | BCrypt.Net-Next | Latest stable | NFR-004 — bcrypt credential verification |
| Background Jobs | Hangfire (PostgreSQL storage) | 1.8.x | NFR-008, TR-004 — lockout notification email job |
| Database | PostgreSQL via Supabase | 15 | TR-003 — User table; failedLoginCount; lockUntil |

---

## Task Overview
Extend the `POST /api/v1/auth/login` endpoint (scaffolded in US_007) with full business logic: case-insensitive email lookup, BCrypt credential verification, deactivated account handling, failed-attempt counter increment, configurable lockout threshold, and a Hangfire notification job dispatched on lockout. A successful login writes a `USER_LOGIN` audit entry and returns the user's role in the JSON response body (alongside the JWT cookie). All credential failure paths return the same generic message to prevent user enumeration (OWASP A07).

## Dependent Tasks
- `task_001_backend-jwt-auth.md` (US_007) — JWT cookie issuance infrastructure must exist
- `task_001_backend-migrations.md` (US_005) — `failedLoginCount` and `lockUntil` columns needed on User entity
- `task_001_backend-hangfire.md` (US_003) — Hangfire server must be running for notification job

## Impacted Components
- `backend/src/UPACIP.Application/Commands/Auth/LoginUserCommand.cs` — new CQRS command
- `backend/src/UPACIP.Application/Handlers/Auth/LoginUserHandler.cs` — new handler
- `backend/src/UPACIP.API/Controllers/AuthController.cs` — update login action
- `backend/src/UPACIP.Domain/Entities/User.cs` — add `failedLoginCount`, `lockUntil` (nullable DateTime)

## Implementation Plan
1. Add `failedLoginCount` (int, default 0) and `lockUntil` (nullable `DateTimeOffset`) to `User` entity; create EF Core migration
2. Implement `LoginUserCommand` + `LoginUserHandler`: look up user by `email.ToLowerInvariant()`; if not found → return generic error (no counter increment); if `status == Inactive` → return generic error (no counter increment); if `lockUntil > UtcNow` → return HTTP 423 "Account temporarily locked"
3. Verify password with `BCrypt.EnhancedVerify(password, user.passwordHash)`; on failure → increment `failedLoginCount`; if count reaches threshold (read from config, default 5) → set `lockUntil = UtcNow + lockDuration`; enqueue Hangfire `AccountLockoutNotificationJob`
4. On successful verification: reset `failedLoginCount` to 0; update `lastLoginAt`; call `JwtAuthService.IssueJwt(user)` (sets JWT cookie); write `USER_LOGIN` audit entry; return HTTP 200 with `{ role: "Patient|Staff|Admin" }` in body
5. Ensure all three failure paths (not found, inactive, wrong password) return identical HTTP 401 body "Invalid email or password" — no field distinction
6. `AccountLockoutNotificationJob`: send email via SMTP to user's email address notifying them of the lockout

## Current Project State
```
backend/
  src/
    UPACIP.Infrastructure/Auth/JwtAuthService.cs  (from US_007)
    UPACIP.API/Controllers/AuthController.cs       (login stub from US_007)
    UPACIP.Domain/Entities/User.cs                 (from US_005)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | backend/src/UPACIP.Domain/Entities/User.cs | Add failedLoginCount, lockUntil columns |
| CREATE | backend/src/UPACIP.Infrastructure/Persistence/Migrations/[ts]_AddLoginLockout.cs | EF Core migration for new User columns |
| CREATE | backend/src/UPACIP.Application/Commands/Auth/LoginUserCommand.cs | Login command record |
| CREATE | backend/src/UPACIP.Application/Handlers/Auth/LoginUserHandler.cs | Handler: verify, counter, lockout, JWT, audit |
| CREATE | backend/src/UPACIP.Infrastructure/BackgroundJobs/AccountLockoutNotificationJob.cs | Hangfire email notification on lockout |
| MODIFY | backend/src/UPACIP.API/Controllers/AuthController.cs | Wire LoginUserHandler; return role in body |

## External References
- [OWASP Authentication Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html)
- [BCrypt.Net-Next EnhancedVerify](https://github.com/BcryptNet/bcrypt.net)
- [ASP.NET Core Account Lockout pattern](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Valid credentials → HTTP 200; `Set-Cookie` with JWT; response body contains `{ "role": "Patient" }`; audit row present
- [ ] Wrong password → HTTP 401 generic; `failedLoginCount` incremented in DB
- [ ] Inactive account + correct password → HTTP 401 generic; `failedLoginCount` unchanged
- [ ] 5 consecutive failures → account `lockUntil` set; subsequent attempt returns HTTP 423; Hangfire job enqueued

## Implementation Checklist
- [x] Add `failedLoginCount` and `lockUntil` to User entity; create migration (AC-002, AC-004)
- [x] Implement `LoginUserHandler`: case-insensitive email lookup; BCrypt verify (AC-001, AC-002)
- [x] Inactive account path → HTTP 401 generic message; no counter increment (AC-003)
- [x] Failed-attempt threshold → set `lockUntil`; enqueue `AccountLockoutNotificationJob` (AC-004)
- [x] Successful login → reset counter; issue JWT cookie; write `USER_LOGIN` audit; return role (AC-001)
- [x] All failure paths return identical generic body "Invalid email or password" — no field disclosure (AC-002, OWASP A07)
