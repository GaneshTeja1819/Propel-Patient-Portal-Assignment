# Task - TASK_001

## Requirement Reference
- **User Story:** us_007
- **Story Location:** .propel/context/tasks/EP-DATA/us_007/us_007.md
- **Acceptance Criteria:**
  - AC-001: `POST /api/v1/auth/login` response has `Set-Cookie` with HttpOnly, Secure, SameSite=Strict; JWT not in response body; cookie contains valid signed JWT
  - AC-002: JWT issued at T expires at T+15 min+1 s; API returns HTTP 401; no expiry details exposed
  - AC-003: Refresh token stored server-side in Redis keyed by session ID; never in response body or client-accessible cookie
  - AC-004: Redis session key TTL = 15 min; expired JWT returns HTTP 401; client redirected to login
  - AC-005: CORS restricted to InfinityFree origin; other origins receive HTTP 403; no wildcard origins
- **Edge Cases:**
  - JWT from previous session after password change → rejected at signature validation; re-authentication required
  - Redis unavailable during refresh → HTTP 503 with `Retry-After` header; no refresh granted
  - JWT tampered → signature validation fails; HTTP 401 with no sensitive error details

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
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | TR-002, NFR-006 — JWT bearer middleware, cookie options, CORS policy |
| Backend | Microsoft.AspNetCore.Authentication.JwtBearer | Built-in .NET 8 | TR-007, NFR-006 — JWT validation middleware; 15-min token expiry |
| Caching | Upstash Redis (StackExchange.Redis) | Serverless | NFR-006, NFR-009 — server-side refresh token store; 15-min sliding TTL |
| Database | PostgreSQL via Supabase | 15 | TR-003 — User table for credential validation on login |

---

## Task Overview
Configure the JWT authentication pipeline in the .NET 8 API: issue JWTs with a 15-minute expiry into HttpOnly Secure SameSite=Strict cookies on successful login, store refresh tokens server-side in Redis with a matching TTL, and configure CORS to accept requests only from the InfinityFree production origin. The JWT signing key, Redis connection string, and CORS origin are loaded exclusively from environment variables. The login endpoint validates credentials against the `User` entity in PostgreSQL.

## Dependent Tasks
- `task_001_backend-scaffold.md` (US_002) — API project structure must exist
- `task_002_backend-redis.md` (US_003) — `ISlotCacheService` and `IConnectionMultiplexer` must be registered
- `task_001_backend-migrations.md` (US_005) — `User` table must exist for credential lookup

## Impacted Components
- `backend/src/UPACIP.API/Program.cs` — JWT bearer config, cookie options, CORS policy
- `backend/src/UPACIP.Application/Interfaces/IAuthService.cs` — new auth service interface
- `backend/src/UPACIP.Infrastructure/Auth/JwtAuthService.cs` — new JWT issuance and refresh implementation
- `backend/src/UPACIP.Infrastructure/Auth/RedisSessionStore.cs` — new Redis refresh token store
- `backend/src/UPACIP.API/Controllers/AuthController.cs` — new login, refresh, and logout endpoints

## Implementation Plan
1. Add `Microsoft.AspNetCore.Authentication.JwtBearer` to the API project (built-in .NET 8); configure in `Program.cs` with signing key from env var; token expiry = 15 minutes; `ValidateIssuerSigningKey = true`, `ValidateLifetime = true`
2. Configure cookie options: `HttpOnly = true`, `Secure = true`, `SameSite = SameSiteMode.Strict`; JWT written to cookie named `__Host-access`; never written to response body
3. Define `IAuthService` in Application layer: `LoginAsync`, `RefreshAsync`, `LogoutAsync`
4. Implement `JwtAuthService` in Infrastructure: generate JWT with `sub` = userId, `role` = user role, `exp` = UTC + 15 min; sign with HMAC-SHA256 key from env var
5. Implement `RedisSessionStore` in Infrastructure: on login, generate a secure random refresh token, store in Redis as key `session:{sessionId}` with TTL = 15 min; on logout, delete the key; on refresh, verify key exists before issuing a new JWT
6. Create `AuthController`: `POST /api/v1/auth/login` — validate credentials (bcrypt hash compare against `User.passwordHash`), issue JWT in cookie, store refresh in Redis; `POST /api/v1/auth/refresh`; `POST /api/v1/auth/logout`
7. Configure CORS in `Program.cs`: `AllowedOrigins = [Environment.GetEnvironmentVariable("ALLOWED_ORIGIN")]`; no wildcard; preflight to other origins returns HTTP 403 (OWASP A05 alignment)
8. Handle Redis unavailability in `RefreshAsync`: catch `RedisConnectionException`, return HTTP 503 with `Retry-After: 30` header; log warning

## Current Project State
```
backend/
  src/
    UPACIP.Application/Interfaces/  (IRepository, IUnitOfWork, IEncryptionService, IAuditLogService)
    UPACIP.Infrastructure/Caching/  (RedisServiceExtensions, SlotCacheService from US_003)
    UPACIP.Domain/Entities/User.cs  (from US_005)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/UPACIP.Application/Interfaces/IAuthService.cs | Login / Refresh / Logout interface |
| CREATE | backend/src/UPACIP.Infrastructure/Auth/JwtAuthService.cs | JWT generation with 15-min expiry; HMAC-SHA256 signing |
| CREATE | backend/src/UPACIP.Infrastructure/Auth/RedisSessionStore.cs | Refresh token store in Redis; 15-min TTL; never exposed to client |
| CREATE | backend/src/UPACIP.API/Controllers/AuthController.cs | POST /api/v1/auth/login, /refresh, /logout |
| MODIFY | backend/src/UPACIP.API/Program.cs | JWT bearer config, HttpOnly cookie options, CORS restricted to InfinityFree origin |

## External References
- [ASP.NET Core JWT Bearer Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn)
- [ASP.NET Core Cookie Options](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.cookieoptions)
- [ASP.NET Core CORS](https://learn.microsoft.com/en-us/aspnet/core/security/cors)
- [OWASP Session Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html)
- [StackExchange.Redis — Key Expiry](https://stackexchange.github.io/StackExchange.Redis/KeysValues)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] `POST /api/v1/auth/login` with valid credentials → response `Set-Cookie` has `HttpOnly; Secure; SameSite=Strict`; JWT not in response body
- [ ] Using the issued JWT, wait 15 min + 1 s and call any protected endpoint → HTTP 401 returned
- [ ] Redis inspection confirms refresh token key present after login; key absent after logout
- [ ] CORS preflight from `http://attacker.com` → HTTP 403; no `Access-Control-Allow-Origin` header in response

## Implementation Checklist
- [ ] Configure JWT bearer middleware: signing key from env var, 15-min expiry, `ValidateLifetime = true` (AC-001, AC-002)
- [ ] Issue JWT in `__Host-access` cookie with HttpOnly, Secure, SameSite=Strict; never in response body (AC-001)
- [ ] Store refresh token in Redis (`session:{sessionId}`) with 15-min TTL; never expose to client (AC-003)
- [ ] Expired JWT (T+15min+1s) returns HTTP 401 without exposing expiry details (AC-002)
- [ ] Redis session key TTL = 15 min; expired key → HTTP 401 on next request; redirect to login (AC-004)
- [ ] Configure CORS: `AllowedOrigins` from env var (InfinityFree URL only); no wildcard; preflight from other origins → HTTP 403 (AC-005)
- [ ] Handle Redis unavailability in `RefreshAsync`: return HTTP 503 with `Retry-After: 30` header (AC-003 edge case)
- [ ] JWT tamper/forgery returns HTTP 401 with generic error message — no internal details disclosed (AC-001 edge case)
