# Task - TASK_002

## Requirement Reference
- **User Story:** us_003
- **Story Location:** .propel/context/tasks/EP-TECH/us_003/us_003.md
- **Acceptance Criteria:**
  - AC-002: Upstash Redis client connects on startup; `PING` returns `PONG` within 2 seconds; failed connection logs a warning without crashing the API
  - AC-004: Slot availability key written to Redis expires after ≤ 5 seconds (TTL per TR-005/NFR-002)
  - AC-005: No Redis connection string or Hangfire credentials appear in any committed source file; all sensitive values loaded from environment variables
- **Edge Cases:**
  - Upstash free-tier connection limit reached → connection pool returns timeout; application logs error and falls through to database read; no crash

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
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | TR-002, NFR-010 — API host for Redis client |
| Caching | Upstash Redis (StackExchange.Redis) | Serverless | NFR-002, NFR-009, TR-005 — BRD §5; slot availability cache with ≤ 5 s TTL |

---

## Task Overview
Configure the Upstash Redis client (`StackExchange.Redis`) in the Infrastructure layer and integrate it into the application's DI container as `IConnectionMultiplexer`. A startup health check issues a `PING` to verify connectivity. A `SlotCacheService` wrapper enforces the 5-second TTL on all slot availability keys per TR-005/NFR-002. The connection string is loaded exclusively from environment variables. On connection failure, the API falls through to database reads and logs a warning — it does not crash.

## Dependent Tasks
- `task_001_backend-scaffold.md` (US_002) — Infrastructure layer must exist

## Impacted Components
- `backend/src/UPACIP.Infrastructure/` — Redis client registration and slot cache service
- `backend/src/UPACIP.Infrastructure/Caching/RedisServiceExtensions.cs` — new Redis DI extension
- `backend/src/UPACIP.Infrastructure/Caching/SlotCacheService.cs` — new slot cache wrapper enforcing TTL
- `backend/src/UPACIP.Application/Interfaces/ISlotCacheService.cs` — new cache service interface in Application layer
- `backend/src/UPACIP.API/Program.cs` — register Redis service and startup health check

## Implementation Plan
1. Add `StackExchange.Redis` NuGet package to Infrastructure project
2. Create `RedisServiceExtensions.cs` — reads connection string from environment variable; registers `IConnectionMultiplexer` as singleton; wraps in try/catch so connection failure logs a warning and does not throw during startup
3. Define `ISlotCacheService` interface in Application layer (`GetSlotAsync`, `SetSlotAsync`, `InvalidateSlotAsync`)
4. Implement `SlotCacheService` in Infrastructure — all `SetSlotAsync` calls apply `TimeSpan.FromSeconds(5)` as TTL; `GetSlotAsync` falls through to null on Redis unavailability (no exception propagation)
5. Register `ISlotCacheService` → `SlotCacheService` in DI in `Program.cs`
6. Add startup PING health check: on API start, issue `PING` to Redis; log success or log warning on failure; do not exit on failure
7. Verify no connection string appears in any `.cs`, `.json`, or `.yaml` file in the repository

## Current Project State
```
backend/
  UPACIP.sln
  src/
    UPACIP.Domain/
    UPACIP.Application/
    UPACIP.Infrastructure/  ← add Redis here
    UPACIP.API/
  tests/
    UPACIP.Tests/
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | backend/src/UPACIP.Infrastructure/UPACIP.Infrastructure.csproj | Add StackExchange.Redis NuGet reference |
| CREATE | backend/src/UPACIP.Infrastructure/Caching/RedisServiceExtensions.cs | Singleton IConnectionMultiplexer registration from env var |
| CREATE | backend/src/UPACIP.Application/Interfaces/ISlotCacheService.cs | Slot cache interface (Get/Set/Invalidate) |
| CREATE | backend/src/UPACIP.Infrastructure/Caching/SlotCacheService.cs | ISlotCacheService implementation with 5 s TTL enforcement |
| MODIFY | backend/src/UPACIP.API/Program.cs | Register Redis service; add startup PING health check log |

## External References
- [StackExchange.Redis Docs](https://stackexchange.github.io/StackExchange.Redis/)
- [Upstash Redis .NET Quickstart](https://upstash.com/docs/redis/quickstarts/dotnet)
- [ASP.NET Core IHealthCheck for Redis](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] API starts; startup log contains `Redis PING: PONG` or `Redis unavailable — falling back to database reads`
- [ ] A slot key written with `SetSlotAsync` is confirmed expired after 5 seconds via `GetSlotAsync` returning null
- [ ] Static grep of repository source confirms zero occurrences of connection string value in committed files
- [ ] Simulated Redis failure (invalid connection string) results in API starting without crash; warning logged

## Implementation Checklist
- [ ] Add StackExchange.Redis to Infrastructure; register `IConnectionMultiplexer` singleton from environment variable; log warning (not throw) on connection failure (AC-002)
- [ ] Define `ISlotCacheService` in Application layer — `GetSlotAsync`, `SetSlotAsync`, `InvalidateSlotAsync` (AC-004)
- [ ] Implement `SlotCacheService` with 5 s TTL on all `SetSlotAsync` writes; null fall-through on Redis unavailability (AC-004, AC-002 edge case)
- [ ] Add startup PING — log `PONG` on success or warning on failure; no API crash on Redis unavailability (AC-002)
- [ ] Confirm all Redis and Hangfire credentials are environment-variable-only; zero values in committed source files (AC-005)
