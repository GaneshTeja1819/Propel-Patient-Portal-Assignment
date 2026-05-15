# Task - TASK_002

## Requirement Reference
- **User Story:** us_009
- **Story Location:** .propel/context/tasks/EP-001/us_009/us_009.md
- **Acceptance Criteria:**
  - AC-001: Patient account created with hashed password and Patient role; immutable audit log entry written; user redirected to login with success message
  - AC-002: Duplicate email → "Email address already in use" error; no indication of role; no partial account created
  - AC-003: Password complexity failure → specific failing rule returned inline; form not submitted
  - AC-004: DB failure → generic error message; no partial User record; no audit log entry for failed attempt
- **Edge Cases:**
  - Double-click submit → server-side idempotency check prevents duplicate account; second request returns HTTP 409
  - Email with unicode characters → normalised to ASCII-compatible before uniqueness check; validation error if normalisation fails

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
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | TR-002 — registration endpoint controller |
| Backend | BCrypt.Net-Next | Latest stable | NFR-004 — bcrypt password hashing; free/open-source |
| Database | PostgreSQL via Supabase | 15 | DR-001, DR-002 — User table; unique email constraint |

---

## Task Overview
Implement the `POST /api/v1/auth/register` endpoint in the API layer backed by a `RegisterUserCommand` use case in the Application layer. The endpoint validates password complexity server-side, normalises the email to lowercase ASCII, checks uniqueness, hashes the password with BCrypt, creates the User record with `role = Patient`, and writes an immutable audit log entry. All operations within the registration use case run inside a single database transaction; on failure the transaction is rolled back atomically so no partial records exist.

## Dependent Tasks
- `task_001_backend-migrations.md` (US_005) — User entity table must exist
- `task_001_backend-phi-encryption.md` (US_006) — `IAuditLogService` must be available
- `task_001_backend-jwt-auth.md` (US_007) — authentication infrastructure in place (endpoint is unauthenticated but uses the same pipeline)

## Impacted Components
- `backend/src/UPACIP.Application/Commands/Auth/RegisterUserCommand.cs` — new CQRS command
- `backend/src/UPACIP.Application/Handlers/Auth/RegisterUserHandler.cs` — new command handler
- `backend/src/UPACIP.API/Controllers/AuthController.cs` — new `POST /api/v1/auth/register` action

## Implementation Plan
1. Define `RegisterUserCommand` record: `Email`, `Password`, `FirstName`, `LastName`
2. Implement `RegisterUserHandler`: normalise `Email` to lowercase; validate password complexity (8+ chars, uppercase, lowercase, digit) — return `ValidationException` with rule details on failure; check uniqueness in DB — return `ConflictException` on duplicate; hash password with `BCrypt.EnhancedHashPassword(password, workFactor: 12)`
3. Wrap User creation and audit log write in `IUnitOfWork.BeginTransactionAsync()`; commit on success, rollback on any exception — ensures no partial record on DB failure (AC-004)
4. Write audit entry: `actorId = new User's id`, `actorRole = "Patient"`, `actionType = "USER_REGISTERED"`, `targetEntity = "User"`, `targetId = new User's id`
5. Add `[AllowAnonymous]` `POST /api/v1/auth/register` action to `AuthController`; map `ConflictException` → HTTP 409, `ValidationException` → HTTP 422 (field-level errors), `Exception` → HTTP 500 generic response
6. Add server-side idempotency via unique index on `User.email` in the DB — concurrent duplicate inserts will hit the unique constraint and return HTTP 409

## Current Project State
```
backend/
  src/
    UPACIP.Application/Interfaces/IAuditLogService.cs  (from US_006)
    UPACIP.Infrastructure/Auth/JwtAuthService.cs       (from US_007)
    UPACIP.API/Controllers/AuthController.cs           (login scaffold from US_007)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/UPACIP.Application/Commands/Auth/RegisterUserCommand.cs | Registration command record |
| CREATE | backend/src/UPACIP.Application/Handlers/Auth/RegisterUserHandler.cs | Handler: validate, hash, create User, write audit |
| MODIFY | backend/src/UPACIP.API/Controllers/AuthController.cs | Add POST /api/v1/auth/register action |

## External References
- [BCrypt.Net-Next NuGet](https://www.nuget.org/packages/BCrypt.Net-Next)
- [OWASP Password Storage Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html)
- [ASP.NET Core CQRS (MediatR pattern)](https://github.com/jbogard/MediatR)
- [Unicode email normalisation (RFC 5322)](https://datatracker.ietf.org/doc/html/rfc5322)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] `POST /api/v1/auth/register` with valid payload → HTTP 200; User row created; audit log entry present
- [ ] Duplicate email → HTTP 409; no new User row in DB
- [ ] Password without uppercase → HTTP 422 with "Must contain at least one uppercase letter" field error
- [ ] Simulated DB error (mocked `IUnitOfWork`) → HTTP 500 generic response; no User row created

## Implementation Checklist
- [ ] Implement `RegisterUserCommand` + `RegisterUserHandler`; normalise email to lowercase; validate password complexity server-side (AC-003)
- [ ] Check email uniqueness before insert; return HTTP 409 with "Email address already in use" — no role disclosure (AC-002)
- [ ] Hash password with BCrypt work factor 12; store hash in `User.passwordHash` (AC-001)
- [ ] Wrap User creation + audit write in a single DB transaction; rollback on any failure; no partial record (AC-004)
- [ ] Write `USER_REGISTERED` audit entry with required fields on success only (AC-001, AC-004)
- [ ] Add `POST /api/v1/auth/register` [AllowAnonymous] action; map exceptions to HTTP 409/422/500 with generic 500 body (AC-001, AC-002, AC-004)
