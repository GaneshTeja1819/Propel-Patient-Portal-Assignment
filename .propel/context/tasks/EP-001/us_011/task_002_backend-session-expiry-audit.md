# Task - TASK_002

## Requirement Reference
- **User Story:** us_011
- **Story Location:** .propel/context/tasks/EP-001/us_011/us_011.md
- **Acceptance Criteria:**
  - AC-001: Server-side session token invalidated on JWT expiry; `SESSION_TIMEOUT` audit event written; expired JWT request returns HTTP 401
- **Edge Cases:**
  - Multiple tabs: session expiry on server side invalidates shared token; all tabs receive HTTP 401 on next call

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
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | NFR-006 — JWT expiry event handled in authentication middleware |
| Caching | Upstash Redis | Serverless | NFR-006 — server-side session TTL; Redis key expiry aligns with JWT 15-min window |
| Database | PostgreSQL via Supabase | 15 | DR-003, NFR-007 — SESSION_TIMEOUT audit entry written to audit.audit_log |

---

## Task Overview
Add a `SESSION_TIMEOUT` audit event to the JWT authentication failure pipeline. When the JWT bearer middleware detects that a token has expired (as opposed to being tampered or missing), the `OnAuthenticationFailed` event writes a `SESSION_TIMEOUT` entry to `audit.audit_log` via `IAuditLogService`. The Redis session key (established in US_007) has a matching 15-minute TTL, so it expires simultaneously with the JWT, ensuring the server-side session is invalid before the next refresh attempt.

## Dependent Tasks
- `task_001_backend-jwt-auth.md` (US_007) — JWT bearer middleware pipeline must exist
- `task_001_backend-phi-encryption.md` (US_006) — `IAuditLogService` must be registered

## Impacted Components
- `backend/src/UPACIP.API/Program.cs` — extend JWT bearer `Events.OnAuthenticationFailed` handler
- `backend/src/UPACIP.Infrastructure/Auth/SessionAuditHandler.cs` — new helper writing SESSION_TIMEOUT audit entry

## Implementation Plan
1. Create `SessionAuditHandler.cs` in Infrastructure/Auth: `WriteSessionTimeoutAuditAsync(AuthenticationFailedContext ctx, IAuditLogService auditLogService)` — extracts `actorId` from the expired token's `sub` claim (if decodable without verification), writes `SESSION_TIMEOUT` audit entry; handles cases where `sub` claim is unavailable (writes with null `actorId`)
2. In `Program.cs`, extend the existing `AddJwtBearer(options => { options.Events = new JwtBearerEvents { OnAuthenticationFailed = ... } })` configuration: when the exception is a `SecurityTokenExpiredException`, call `SessionAuditHandler.WriteSessionTimeoutAuditAsync`; for all other failure types, do not write this specific audit entry
3. Verify that the Redis session key TTL set in US_007 (`session:{sessionId}`) expires at 15 minutes — confirming server-side and client-side session lifetimes are aligned

## Current Project State
```
backend/
  src/
    UPACIP.API/Program.cs              (JWT bearer configured from US_007)
    UPACIP.Infrastructure/Auth/        (JwtAuthService, RedisSessionStore from US_007)
    UPACIP.Infrastructure/Audit/AuditLogService.cs  (from US_006)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/UPACIP.Infrastructure/Auth/SessionAuditHandler.cs | Write SESSION_TIMEOUT audit entry on SecurityTokenExpiredException |
| MODIFY | backend/src/UPACIP.API/Program.cs | Wire SessionAuditHandler into JwtBearerEvents.OnAuthenticationFailed |

## External References
- [ASP.NET Core JwtBearerEvents](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.jwtbearer.jwtbearerevents)
- [System.IdentityModel.Tokens.Jwt SecurityTokenExpiredException](https://learn.microsoft.com/en-us/dotnet/api/microsoft.identitymodel.tokens.securitytokenexpiredexception)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Present an expired JWT (fabricated with past `exp` claim); confirm HTTP 401 returned and `SESSION_TIMEOUT` row appears in `audit.audit_log`
- [ ] Present a tampered JWT; confirm HTTP 401 returned and no `SESSION_TIMEOUT` row appears (only expired-token path triggers the event)

## Implementation Checklist
- [ ] Implement `SessionAuditHandler` — write `SESSION_TIMEOUT` audit entry when `SecurityTokenExpiredException` is caught in `OnAuthenticationFailed` (AC-001)
- [ ] Wire handler into `JwtBearerEvents.OnAuthenticationFailed` in `Program.cs`; guard: only for `SecurityTokenExpiredException` (AC-001)
- [ ] Verify Redis session key TTL = 15 min (aligns with JWT expiry for multi-tab invalidation edge case) (AC-001 edge case)
