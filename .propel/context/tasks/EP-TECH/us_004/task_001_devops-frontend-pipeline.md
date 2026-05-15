# Task - TASK_001

## Requirement Reference
- **User Story:** us_004
- **Story Location:** .propel/context/tasks/EP-TECH/us_004/us_004.md
- **Acceptance Criteria:**
  - AC-001: GitHub Actions frontend workflow runs `npm install`, `npm run build`, frontend tests, deploys to InfinityFree; exits code 0
  - AC-003: Static secret scan on workflow YAML finds zero raw credential values; all sensitive values use `${{ secrets.SECRET_NAME }}` syntax
  - AC-004: If frontend tests fail, the deploy step is skipped and the pipeline reports failure
  - AC-005: Frontend pipeline completes within 10 minutes on `ubuntu-latest`
- **Edge Cases:**
  - InfinityFree FTP endpoint unavailable → deployment step fails with timeout; pipeline exits non-zero
  - GitHub Secrets missing → pipeline fails at the referencing step with a descriptive error; no partial deployment
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
| Frontend | React (SPA) | 18.x | TR-001, NFR-010 — SPA artefact built and deployed in this pipeline |
| Infrastructure | InfinityFree | — | NFR-010, TR-018 — free-tier static hosting target for React SPA |
| Testing | React Testing Library | Latest stable | NFR-012 — frontend test gate blocks deployment on failure |

---

## Task Overview
Create the GitHub Actions CI/CD workflow (`frontend.yml`) that installs dependencies, runs the build, executes frontend tests, and deploys the React SPA artefact to InfinityFree on every push to `main`. The deploy step is conditional on the test step passing (exit code 0). All credentials (FTP host, username, password) are loaded exclusively from GitHub Secrets. The complete pipeline must finish within 10 minutes on the `ubuntu-latest` free-tier runner.

## Dependent Tasks
- `task_001_frontend-scaffold.md` (US_001) — React SPA scaffold must exist before the pipeline can build it

## Impacted Components
- `.github/workflows/frontend.yml` — new GitHub Actions workflow file
- `frontend/package.json` — `test` script must be defined (established in US_001)

## Implementation Plan
1. Create `.github/workflows/frontend.yml` triggered on `push` to `main` and `pull_request` to `main`
2. Define `ubuntu-latest` runner; set `working-directory: frontend`
3. Add steps: `actions/checkout@v4`, `actions/setup-node@v4` (Node 20 LTS), `npm ci`, `npm run build`, `npm test -- --watchAll=false`
4. Configure deploy step with `SamKirkland/FTP-Deploy-Action@v4` using `${{ secrets.INFINITYFREE_FTP_SERVER }}`, `${{ secrets.INFINITYFREE_FTP_USER }}`, `${{ secrets.INFINITYFREE_FTP_PASSWORD }}`; `if: success()` condition ensures test failure blocks deploy
5. Add `timeout-minutes: 10` at job level to enforce AC-005
6. Add a `gitleaks` or `truffleHog` step (or inline grep) confirming zero raw credential values in workflow YAML; fail if found

## Current Project State
```
/ (greenfield)
.github/workflows/  ← create here
frontend/           ← built by this pipeline
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | .github/workflows/frontend.yml | Frontend CI/CD: install → build → test → deploy to InfinityFree |

## External References
- [GitHub Actions Docs](https://docs.github.com/en/actions)
- [actions/setup-node@v4](https://github.com/actions/setup-node)
- [SamKirkland/FTP-Deploy-Action](https://github.com/SamKirkland/FTP-Deploy-Action)
- [GitHub Encrypted Secrets](https://docs.github.com/en/actions/security-guides/encrypted-secrets)

## Build Commands
- Refer to [frontend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Push to `main` triggers `frontend.yml`; all steps complete with exit code 0
- [ ] Intentionally broken test (`throw new Error()`) causes pipeline to fail at test step; deploy step is skipped
- [ ] Static scan of `frontend.yml` reports zero raw credential values
- [ ] Pipeline completes within 10 minutes (verified from Actions run summary)

## Implementation Checklist
- [ ] Create `.github/workflows/frontend.yml`; trigger on `push`/`pull_request` to `main`; set `ubuntu-latest` runner with `timeout-minutes: 10` (AC-005)
- [ ] Add `actions/checkout@v4`, `actions/setup-node@v4` (Node 20 LTS), `npm ci`, `npm run build` steps (AC-001)
- [ ] Add `npm test -- --watchAll=false` step; confirm exit code propagation (AC-001, AC-004)
- [ ] Add FTP deploy step with `if: success()`; all FTP credentials reference `${{ secrets.* }}` only (AC-001, AC-003)
- [ ] Verify zero raw credential values in `frontend.yml` via static scan step or grep assertion (AC-003)
