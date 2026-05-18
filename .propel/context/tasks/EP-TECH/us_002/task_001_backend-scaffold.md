# Task - TASK_001

## Requirement Reference
- **User Story:** us_002
- **Story Location:** .propel/context/tasks/EP-TECH/us_002/us_002.md
- **Acceptance Criteria:**
  - AC-001: Solution compiles; published artefact deploys to MonsterASP IIS; `GET /api/v1/health` returns HTTP 200
  - AC-002: Domain ← Application ← Infrastructure ← API layer dependency direction enforced; direct infrastructure-to-domain coupling is a build error
  - AC-003: `/api/v1/health` returns HTTP 200; `/api/health` returns HTTP 404 confirming versioned prefix is mandatory
  - AC-004: Swagger UI renders at `/swagger/index.html` in development; all endpoints listed with method, route, and response schema
- **Edge Cases:**
  - IIS application pool recycles → health check returns HTTP 503; CI deployment gate fails if 200 not returned within 60 s
  - MonsterASP free-tier memory limit → application returns HTTP 503; CI gate alerts on sustained 503
  - Solution references Linux-only API → CI build targeting Windows IIS fails with descriptive compile error (TR-018)

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
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | TR-002, NFR-010, NFR-012 — BRD §5; free/open-source; LTS for Phase 1 |
| Backend | Swashbuckle (OpenAPI/Swagger) | 6.x | NFR-012 — auto-generated API documentation from controllers |
| Backend | Asp.Versioning.Mvc | 8.x | TR-016 — REST API versioning (`/api/v1/`) mandatory |
| Database | PostgreSQL via Supabase | 15 | DR-001–DR-007, TR-003 — primary data store; Supabase free tier |
| Infrastructure | MonsterASP (IIS) | — | TR-018, NFR-010 — Windows IIS hosting; free tier |

---

## Task Overview
Create the .NET 8 ASP.NET Core Web API solution following Clean Architecture layering — four separate .csproj files (Domain, Application, Infrastructure, API). Layer dependency direction is enforced by project references only (Domain ← Application ← Infrastructure ← API). The API exposes a `/api/v1/health` endpoint, REST versioning via Asp.Versioning, and Swagger UI via Swashbuckle. The published artefact must deploy to MonsterASP IIS and respond to health check with HTTP 200. This scaffold is the backend foundation for all feature teams.

## Dependent Tasks
- None (EP-TECH foundational task — no upstream dependencies)

## Impacted Components
- `backend/UPACIP.sln` — new solution file
- `backend/src/UPACIP.Domain/` — new Domain project
- `backend/src/UPACIP.Application/` — new Application project (refs Domain only)
- `backend/src/UPACIP.Infrastructure/` — new Infrastructure project (refs Application)
- `backend/src/UPACIP.API/` — new API project (refs Infrastructure)
- `backend/src/UPACIP.API/Controllers/HealthController.cs` — new health check controller

## Implementation Plan
1. Create solution file and four .csproj files; configure project references enforcing the Clean Architecture direction
2. Add `Domain` layer: base entity class (`BaseEntity.cs` with `Id` UUID), `IRepository<T>` interface, no external package references
3. Add `Application` layer: base use case handler interface; `IUnitOfWork` interface; reference Domain only
4. Add `Infrastructure` layer: EF Core `DbContext` stub, `Repository<T>` implementation; reference Application only
5. Add `API` layer: configure `WebApplication` builder, register DI, add Swagger (Swashbuckle 6.x), add API versioning (Asp.Versioning 8.x)
6. Implement `GET /api/v1/health` endpoint returning `{ "status": "healthy", "timestamp": "<UTC>" }` with HTTP 200
7. Configure `appsettings.json` with placeholder connection string; load all secrets from environment variables (never hardcoded)
8. Verify `dotnet publish -c Release` produces artefact deployable to Windows IIS (TR-018)

## Current Project State
```
/ (greenfield — no existing project files)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/UPACIP.sln | .NET solution file |
| CREATE | backend/src/UPACIP.Domain/UPACIP.Domain.csproj | Domain layer project |
| CREATE | backend/src/UPACIP.Domain/Entities/BaseEntity.cs | Base entity with UUID id |
| CREATE | backend/src/UPACIP.Domain/Interfaces/IRepository.cs | Generic repository interface |
| CREATE | backend/src/UPACIP.Application/UPACIP.Application.csproj | Application layer (refs Domain) |
| CREATE | backend/src/UPACIP.Application/Interfaces/IUnitOfWork.cs | Unit of work interface |
| CREATE | backend/src/UPACIP.Infrastructure/UPACIP.Infrastructure.csproj | Infrastructure layer (refs Application) |
| CREATE | backend/src/UPACIP.Infrastructure/Persistence/AppDbContext.cs | EF Core DbContext stub |
| CREATE | backend/src/UPACIP.API/UPACIP.API.csproj | API layer (refs Infrastructure) |
| CREATE | backend/src/UPACIP.API/Program.cs | WebApplication builder with DI, Swagger, versioning |
| CREATE | backend/src/UPACIP.API/Controllers/HealthController.cs | GET /api/v1/health endpoint |
| CREATE | backend/src/UPACIP.API/appsettings.json | Connection string placeholders (no secrets) |

## External References
- [ASP.NET Core 8 Web API Docs](https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api?view=aspnetcore-8.0)
- [Clean Architecture .NET — Jason Taylor template](https://github.com/jasontaylordev/CleanArchitecture)
- [Asp.Versioning.Mvc 8.x](https://github.com/dotnet/aspnet-api-versioning)
- [Swashbuckle ASP.NET Core 6.x](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)
- [EF Core 8 Docs](https://learn.microsoft.com/en-us/ef/core/)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] `dotnet build` exits with code 0; no CS-level dependency direction violations
- [ ] `GET /api/v1/health` returns HTTP 200 on local run
- [ ] `GET /api/health` (no version prefix) returns HTTP 404
- [ ] Swagger UI renders at `http://localhost:<port>/swagger/index.html` with health endpoint listed
- [ ] `dotnet publish -c Release` produces Windows-IIS-deployable artefact

## Implementation Checklist
- [x] Create solution with Domain, Application, Infrastructure, API projects; enforce layer reference direction (AC-002)
- [x] Implement `BaseEntity` (UUID id) in Domain; define `IRepository<T>` and `IUnitOfWork` interfaces in Domain/Application (AC-002)
- [x] Configure EF Core `AppDbContext` stub in Infrastructure; wire DI in `Program.cs` (AC-001)
- [x] Add Asp.Versioning 8.x; configure `/api/v1/` prefix; verify `/api/health` returns HTTP 404 (AC-003)
- [x] Add Swashbuckle 6.x; configure Swagger UI at `/swagger/index.html` for development environment (AC-004)
- [x] Implement `HealthController` with `GET /api/v1/health` returning HTTP 200 JSON body (AC-001)
- [x] Load connection string from environment variable only; verify no secrets in `appsettings.json` (DR-001)
- [x] `dotnet publish -c Release --runtime win-x64` produces IIS-deployable artefact (TR-018, AC-001)
