# Task - TASK_002

## Requirement Reference
- **User Story:** us_004
- **Story Location:** .propel/context/tasks/EP-TECH/us_004/us_004.md
- **Acceptance Criteria:**
  - AC-002: GitHub Actions backend workflow runs `dotnet build`, `dotnet test`, publishes artefact, deploys to MonsterASP/IIS, calls `GET /api/v1/health` for HTTP 200; exits code 0
  - AC-003: Static secret scan on workflow YAML finds zero raw credential values; all sensitive values use `${{ secrets.SECRET_NAME }}` syntax
  - AC-004: If `dotnet test` fails, deploy step is skipped and pipeline reports failure
  - AC-005: Backend pipeline completes within 10 minutes on `ubuntu-latest`
- **Edge Cases:**
  - MonsterASP FTP/deployment endpoint unavailable → deployment step fails with timeout; pipeline exits non-zero
  - GitHub Secrets missing → pipeline fails at referencing step with a descriptive error; no partial deployment
  - Free-tier Actions minutes exhausted → pipeline queues or fails with quota-exceeded message

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
| DevOps | GitHub Actions | — | NFR-010, NFR-012, TR-012 — BRD §5; free-tier CI/CD; mandated platform |
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | TR-002, NFR-012 — compiled, tested, and deployed by this pipeline |
| Testing | xUnit (.NET) | Latest stable | NFR-012, TR-002 — test gate; `dotnet test` failure blocks deployment |
| Infrastructure | MonsterASP (IIS) | — | NFR-010, TR-018 — Windows IIS hosting target for .NET 8 API |

---

## Task Overview
Create the GitHub Actions CI/CD workflow (`backend.yml`) that builds the .NET 8 solution, runs xUnit tests, publishes a Windows IIS-compatible artefact, deploys to MonsterASP, and calls `GET /api/v1/health` to verify the deployment succeeded. The deploy step is conditional on `dotnet test` passing. All deployment credentials are loaded exclusively from GitHub Secrets. The pipeline must complete within 10 minutes on `ubuntu-latest`.

## Dependent Tasks
- `task_001_backend-scaffold.md` (US_002) — .NET solution and health endpoint must exist
- `task_002_testing-scaffold.md` (US_002) — xUnit test project must exist for `dotnet test` to run

## Impacted Components
- `.github/workflows/backend.yml` — new GitHub Actions workflow file
- `backend/UPACIP.sln` — solution file referenced by `dotnet build` and `dotnet test`

## Implementation Plan
1. Create `.github/workflows/backend.yml` triggered on `push` to `main` and `pull_request` to `main`
2. Define `ubuntu-latest` runner with `timeout-minutes: 10`
3. Add steps: `actions/checkout@v4`, `actions/setup-dotnet@v4` (SDK 8.0), `dotnet restore`, `dotnet build --no-restore -c Release`
4. Add `dotnet test --no-build -c Release --logger trx` step; use `dorny/test-reporter@v1` to publish `.trx` results to CI summary
5. Add `dotnet publish -c Release --runtime win-x64 --self-contained false -o ./publish` step with `if: success()`
6. Add FTP/WebDeploy deployment step to MonsterASP using `${{ secrets.MONSTERASP_* }}` credentials; `if: success()` condition
7. Add health-check step: `curl -f https://${{ secrets.MONSTERASP_HOST }}/api/v1/health` — fail pipeline if status is not 200; retry up to 3 times with 20-second interval to allow IIS warm-up (AC-002 edge case — 60-second window)
8. Add static credential scan step verifying zero raw values in `backend.yml`

## Current Project State
```
/ (greenfield)
.github/workflows/
  frontend.yml  ← created by task_001_devops-frontend-pipeline
backend/        ← built and deployed by this pipeline
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | .github/workflows/backend.yml | Backend CI/CD: restore → build → test → publish → deploy to MonsterASP → health check |

## External References
- [GitHub Actions Docs](https://docs.github.com/en/actions)
- [actions/setup-dotnet@v4](https://github.com/actions/setup-dotnet)
- [dorny/test-reporter (xUnit .trx)](https://github.com/dorny/test-reporter)
- [dotnet publish CLI](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-publish)
- [GitHub Encrypted Secrets](https://docs.github.com/en/actions/security-guides/encrypted-secrets)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Push to `main` triggers `backend.yml`; all steps complete with exit code 0; health check returns HTTP 200
- [ ] Intentionally broken xUnit test causes pipeline to fail at `dotnet test`; deploy step is skipped
- [ ] Static scan of `backend.yml` reports zero raw credential values
- [ ] Pipeline completes within 10 minutes (verified from Actions run summary)
- [ ] `.trx` test results file visible in CI summary via `dorny/test-reporter`

## Implementation Checklist
- [ ] Create `.github/workflows/backend.yml`; trigger on `push`/`pull_request` to `main`; set `ubuntu-latest` with `timeout-minutes: 10` (AC-005)
- [ ] Add `actions/setup-dotnet@v4` (SDK 8.0), `dotnet restore`, `dotnet build --no-restore -c Release` steps (AC-002)
- [ ] Add `dotnet test --no-build -c Release --logger trx` step; attach `dorny/test-reporter` to publish results to CI summary (AC-002, AC-004)
- [ ] Add `dotnet publish -c Release --runtime win-x64 --self-contained false` and deploy to MonsterASP with `if: success()`; all credentials from `${{ secrets.* }}` (AC-002, AC-003)
- [ ] Add post-deploy health check `curl -f .../api/v1/health` with 3 retries / 20 s interval; fail pipeline if HTTP 200 not returned within 60 s (AC-002 edge case)
- [ ] Verify zero raw credential values in `backend.yml` via static scan step (AC-003)
