# Task - TASK_001

## Requirement Reference
- **User Story:** us_003
- **Story Location:** .propel/context/tasks/EP-TECH/us_003/us_003.md
- **Acceptance Criteria:**
  - AC-001: Hangfire schema tables created in PostgreSQL on startup; dashboard accessible at `/hangfire` with Basic Auth; no job-registration errors in startup logs
  - AC-003: Global job filter configured with 3 retries and exponential back-off (10 s, 60 s, 360 s); job transitions to Failed after third failure with no additional retries
- **Edge Cases:**
  - PostgreSQL Hangfire schema migration fails on startup → API logs error and exits with non-zero code; CI deployment gate treats as failed deployment
  - Hangfire dashboard exposed without authentication → integration test asserts HTTP 401 on `/hangfire` without credentials

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
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | TR-002, NFR-010 — API host for Hangfire server |
| Background Jobs | Hangfire (PostgreSQL storage) | 1.8.x | NFR-008, TR-004 — BRD §5; retry-safe background job scheduling with free-tier PostgreSQL persistence |
| Database | PostgreSQL via Supabase | 15 | TR-003, DR-001 — Hangfire job store using existing Supabase PostgreSQL instance |

---

## Task Overview
Configure Hangfire 1.8.x in the Infrastructure and API layers of the .NET 8 solution using PostgreSQL (Supabase) as the persistent job store. The Hangfire server starts with the API, schema migration runs on startup, and the dashboard is secured with HTTP Basic Authentication. The global job retry filter implements the NFR-008 exponential back-off policy (3 attempts: 10 s, 60 s, 360 s). This provides the shared background job infrastructure for reminder dispatch, slot swap, AI extraction, and calendar sync features.

## Dependent Tasks
- `task_001_backend-scaffold.md` (US_002) — .NET solution and Infrastructure layer must exist

## Impacted Components
- `backend/src/UPACIP.Infrastructure/` — Hangfire service registration and storage configuration
- `backend/src/UPACIP.API/Program.cs` — Hangfire server and dashboard middleware registration
- `backend/src/UPACIP.Infrastructure/BackgroundJobs/HangfireJobFilter.cs` — new retry job filter
- `backend/src/UPACIP.API/appsettings.json` — Hangfire dashboard credential placeholder

## Implementation Plan
1. Add `Hangfire` (1.8.x) and `Hangfire.PostgreSql` NuGet packages to Infrastructure project
2. Create `HangfireServiceExtensions.cs` in Infrastructure — configures PostgreSQL storage with connection string from environment variable; sets schema name to `hangfire`
3. Create `ExponentialBackOffRetryFilter.cs` implementing `IElectStateFilter` — 3 retries at delays 10 s, 60 s, 360 s; transitions to Failed on exhaustion (NFR-008)
4. Register Hangfire services and the retry filter in `Program.cs` DI; add `UseHangfireServer()` and `UseHangfireDashboard()` middleware
5. Configure Hangfire dashboard at `/hangfire` with `DashboardOptions.Authorization` using `BasicAuthAuthorizationFilter`; credentials loaded from environment variables
6. Add startup health-check log message confirming schema tables exist; exit non-zero if schema migration fails
7. Verify dashboard returns HTTP 401 without credentials and HTTP 200 with valid credentials

## Current Project State
```
backend/
  UPACIP.sln
  src/
    UPACIP.Domain/
    UPACIP.Application/
    UPACIP.Infrastructure/  ← add Hangfire here
    UPACIP.API/             ← register middleware here
  tests/
    UPACIP.Tests/
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | backend/src/UPACIP.Infrastructure/UPACIP.Infrastructure.csproj | Add Hangfire 1.8.x and Hangfire.PostgreSql NuGet references |
| CREATE | backend/src/UPACIP.Infrastructure/BackgroundJobs/HangfireServiceExtensions.cs | Hangfire storage configuration with PostgreSQL |
| CREATE | backend/src/UPACIP.Infrastructure/BackgroundJobs/ExponentialBackOffRetryFilter.cs | IElectStateFilter implementation — 3 retries, exponential back-off |
| MODIFY | backend/src/UPACIP.API/Program.cs | Register Hangfire services; add server and dashboard middleware |
| MODIFY | backend/src/UPACIP.API/appsettings.json | Add Hangfire dashboard credential key names (no values) |

## External References
- [Hangfire 1.8 Docs](https://docs.hangfire.io/en/latest/)
- [Hangfire.PostgreSql NuGet](https://www.nuget.org/packages/Hangfire.PostgreSql)
- [Hangfire Dashboard Authorization](https://docs.hangfire.io/en/latest/configuration/using-dashboard.html#configuring-authorization)
- [IElectStateFilter for retry customisation](https://docs.hangfire.io/en/latest/extensibility/using-job-filters.html)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] API starts without errors; Hangfire schema tables (`hangfire.*`) exist in PostgreSQL after first run
- [ ] `GET /hangfire` without credentials returns HTTP 401
- [ ] `GET /hangfire` with valid Basic Auth credentials returns HTTP 200
- [ ] A test job that throws an exception retries exactly 3 times and enters Failed state (no further retries)

## Implementation Checklist
- [ ] Add Hangfire 1.8.x and Hangfire.PostgreSql to Infrastructure project; configure PostgreSQL job store from environment variable connection string (AC-001)
- [ ] Implement `ExponentialBackOffRetryFilter` — 3 retries at 10 s / 60 s / 360 s; Failed state after third failure (AC-003)
- [ ] Register retry filter as global job filter in `AddHangfire()` DI configuration (AC-003)
- [ ] Register Hangfire server (`UseHangfireServer`) and dashboard (`UseHangfireDashboard`) in `Program.cs` middleware pipeline (AC-001)
- [ ] Configure dashboard `BasicAuthAuthorizationFilter`; load credentials from environment variables only; verify HTTP 401 without credentials (AC-001 edge case)
- [ ] Add startup log confirming schema migration success; exit with non-zero code on migration failure (AC-001 edge case)
