# Task - TASK_002

## Requirement Reference
- **User Story:** us_002
- **Story Location:** .propel/context/tasks/EP-TECH/us_002/us_002.md
- **Acceptance Criteria:**
  - AC-005: xUnit test project exists referencing the Application layer; `dotnet test` passes in CI; test results reported in CI summary
- **Edge Cases:**
  - IIS application pool recycles unexpectedly → health-check response test detects HTTP 503 and marks CI as failed

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
| Testing | xUnit (.NET) | Latest stable | NFR-012, TR-002 — unit/integration testing framework for .NET backend |
| Testing | Moq | Latest stable | NFR-012 — mocking framework for interface-based unit tests |
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | TR-002, NFR-012 — test project targets the Application layer of the scaffold |

---

## Task Overview
Create the xUnit test project (`UPACIP.Tests`) that references the Application layer and provides the baseline test infrastructure for all backend unit tests. This task establishes the test project structure, wiring, and a smoke test to confirm the CI `dotnet test` gate is operational. All feature-specific unit tests will be added in their respective feature tasks.

## Dependent Tasks
- `task_001_backend-scaffold.md` — Application layer project must exist before the test project can reference it

## Impacted Components
- `backend/tests/UPACIP.Tests/` — new xUnit test project
- `backend/tests/UPACIP.Tests/UPACIP.Tests.csproj` — project file referencing Application layer
- `backend/UPACIP.sln` — test project added to solution

## Implementation Plan
1. Create `UPACIP.Tests.csproj` under `backend/tests/UPACIP.Tests/`; add xUnit, xUnit.runner.visualstudio, and Moq NuGet references
2. Add project reference to `UPACIP.Application` only (test isolation — do not reference Infrastructure or API directly)
3. Create `SmokeTests/ApplicationLayerSmokeTest.cs` with one fact test asserting that the Application assembly loads without exception
4. Add the test project to `UPACIP.sln` with `dotnet sln add`
5. Verify `dotnet test --logger trx` exits with code 0 and produces a `.trx` results file

## Current Project State
```
backend/
  UPACIP.sln (created by task_001)
  src/
    UPACIP.Domain/
    UPACIP.Application/
    UPACIP.Infrastructure/
    UPACIP.API/
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/tests/UPACIP.Tests/UPACIP.Tests.csproj | xUnit test project referencing Application layer |
| CREATE | backend/tests/UPACIP.Tests/SmokeTests/ApplicationLayerSmokeTest.cs | Smoke test confirming Application assembly loads |
| MODIFY | backend/UPACIP.sln | Add tests/UPACIP.Tests project to solution |

## External References
- [xUnit.net Docs](https://xunit.net/docs/getting-started/netcore/cmdline)
- [Moq Quickstart](https://github.com/devlooped/moq/wiki/Quickstart)
- [dotnet test CLI](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] `dotnet test` exits with code 0; smoke test passes
- [ ] `dotnet test --logger trx` produces a `.trx` results file readable by CI summary reporter
- [ ] No direct references to Infrastructure or API layer exist in the test project

## Implementation Checklist
- [x] Create `UPACIP.Tests.csproj` with xUnit, xUnit.runner.visualstudio, Moq references; add project reference to Application layer only (AC-005)
- [x] Write `ApplicationLayerSmokeTest.cs` — one `[Fact]` asserting Application assembly loads (AC-005)
- [x] Add test project to `UPACIP.sln`; confirm `dotnet build` of the solution includes test project (AC-005)
