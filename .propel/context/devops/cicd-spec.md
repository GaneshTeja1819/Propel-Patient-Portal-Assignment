---
title: "CI/CD Pipeline Specification – Unified Patient Access & Clinical Intelligence Platform"
version: "1.0"
date: "2026-05-16"
source: "design.md v1.0, spec.md v1.0"
status: "Draft"
platform: "github-actions"
---

# CI/CD Pipeline Specification

## Rules Applied

- `cicd-pipeline-standards` **[CRITICAL]** — Atomic stages, fail-fast security gates, stage order Build → Lint → Test → Scan → Deploy, no `continue-on-error` on security stages, manual approvals enforced
- `security-standards-owasp` **[CRITICAL]** — Secrets via environment variables only, HTTPS everywhere, dependency vulnerability scanning, SAST mandatory, no inline credentials
- `gitops-standards` **[CRITICAL]** — Branch-driven environment promotion, immutable artifacts, pipeline-as-code in repository

---

## Project Overview

The Unified Patient Access & Clinical Intelligence Platform is a HIPAA-compliant, web-based healthcare system with:

- **Frontend**: React 18 SPA deployed to InfinityFree static hosting
- **Backend**: ASP.NET Core 8 Web API deployed to MonsterASP via IIS (WebDeploy)
- **Database**: PostgreSQL managed via Supabase (schema managed with EF Core migrations)
- **Background Jobs**: Hangfire (PostgreSQL-backed)
- **AI Engine**: Gemini API (Google AI .NET SDK)
- **Infrastructure**: Shared free-tier hosting — no containers, no Kubernetes, no Terraform

All CI/CD runs on GitHub Actions free tier. Deployment is non-containerised per TR-018 and NFR-010.

---

## Target Configuration

| Attribute | Value |
|-----------|-------|
| CI/CD Platform | GitHub Actions |
| Frontend Hosting | InfinityFree (FTP static deploy) |
| Backend Hosting | MonsterASP — IIS via WebDeploy |
| Database | Supabase PostgreSQL (managed) |
| Environments | dev, qa, staging, prod |
| Branching Strategy | GitHub Flow with environment promotion |
| Containers in Production | No (TR-018) |
| IaC | None — manual shared hosting setup |

---

## Technology Stack Summary

| Layer | Technology | Build Tool | Test Framework | Lint Tool |
|-------|------------|------------|----------------|-----------|
| Frontend | React 18 SPA | npm / Vite | Jest + React Testing Library | ESLint |
| Backend | ASP.NET Core 8 | dotnet CLI | xUnit + coverlet | StyleCop / Roslyn Analyzers |
| E2E | Playwright | npm | Playwright Test | — |
| Database | EF Core 8 Migrations | dotnet ef | — | — |

---

## Pipeline Stages

### Stage 1: Build Verification

- **CICD-001**: Pipeline MUST compile and build all application artifacts — React SPA (`npm run build` → `dist/`) and .NET API (`dotnet publish --configuration Release` → deployment package)
- **CICD-002**: Pipeline MUST verify all dependencies resolve successfully — `npm ci` (frontend) and `dotnet restore` (backend) MUST complete without error
- **CICD-003**: Pipeline MUST generate and attach build metadata to each artifact: Git commit SHA, branch name, build timestamp, and semantic version derived from `package.json` (frontend) or `Directory.Build.props` (backend)
- **CICD-004**: Pipeline MUST fail immediately on any compilation error, type error, or unresolved dependency; the pipeline MUST NOT proceed to subsequent stages on build failure

### Stage 2: Code Quality

- **CICD-010**: Pipeline MUST run ESLint on the React codebase with zero-tolerance for errors (`eslint --max-warnings 0`); StyleCop Roslyn analyzers MUST be applied to all `.cs` files with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in the project configuration
- **CICD-011**: Pipeline MUST enforce a minimum unit test code coverage threshold of **80%** for both frontend (Jest `--coverage --coverageThreshold`) and backend (coverlet with reportgenerator); the pipeline MUST fail if either threshold is not met
- **CICD-012**: Pipeline MUST fail the quality stage if any linter rule violation is introduced or code coverage drops below the threshold relative to the previous passing build
- **CICD-013**: Pipeline MUST generate code quality reports in JUnit XML format (test results) and LCOV/Cobertura format (coverage), and upload them as GitHub Actions artifacts for every run
- **CICD-014**: Pipeline MUST validate that no EF Core migration is missing (`dotnet ef migrations list --no-build`); unapplied migrations MUST be flagged in the quality report

### Stage 3: Security Scanning — ALL GATES BLOCKING

> **HIPAA NOTE**: Security gates are unconditionally blocking on all environments. `continue-on-error: true` is PROHIBITED on any security job. Skipping is not permitted unless `--skip-security-gates` is explicitly documented with security lead sign-off.

- **CICD-020**: Pipeline MUST run SAST using **CodeQL** (`github/codeql-action@v3`) targeting both `csharp` and `javascript-typescript` language configurations; the pipeline MUST fail on any `critical` or `high` severity CodeQL finding
- **CICD-021**: Pipeline MUST run Software Composition Analysis (SCA) using **Snyk** (`snyk/actions/node@master` for frontend npm packages and `snyk/actions/dotnet@master` for backend NuGet packages); the pipeline MUST fail on any `critical` or `high` severity vulnerability; SNYK_TOKEN MUST be sourced from GitHub Secrets
- **CICD-022**: Pipeline MUST run **`npm audit --audit-level=critical`** for the frontend and **`dotnet list package --vulnerable`** for the backend as supplementary dependency audit steps; findings at `critical` severity MUST block the pipeline
  > _Note: Container image scanning (Trivy/Grype) is not applicable to this project — production deployment is non-containerised per TR-018. If Docker is introduced in future phases, Trivy scanning MUST be added before production deployment._
- **CICD-023**: Pipeline MUST run secrets detection using **GitLeaks** (`gitleaks/gitleaks-action@v2.3`) on every push and pull request; zero findings are permitted; GITLEAKS_LICENSE secret MUST be configured
- **CICD-024**: Pipeline MUST fail on ANY CRITICAL or HIGH severity finding from SAST (CodeQL), SCA (Snyk), or secrets detection (GitLeaks); findings MUST be surfaced in the GitHub Security tab via SARIF upload
- **CICD-025**: Pipeline MUST include a custom PHI audit step that verifies no plain-text PHI patterns (e.g., email addresses, SSN-like patterns, medical identifiers) appear in build output, log files, or generated configuration artifacts; this step supports HIPAA compliance per DR-001 and NFR-011
  > _IaC scanning (tfsec/Checkov) is not applicable — no Terraform or Bicep files exist in this project._

### Stage 4: Testing

- **CICD-030**: Pipeline MUST run all unit tests — `npx jest --ci --coverage` (frontend) and `dotnet test --collect:"XPlat Code Coverage"` (backend) — with coverage reports uploaded as artifacts; coverage MUST meet the 80% threshold defined in CICD-011
- **CICD-031**: Pipeline MUST run integration tests for the .NET API layer targeting a dedicated test Supabase project or in-memory PostgreSQL substitute; integration tests MUST cover all API endpoint groups: authentication, appointments, clinical documents, and audit log; integration tests run on `qa`, `staging`, and `prod` environments only
- **CICD-032**: Pipeline MUST run E2E tests using **Playwright** (`npx playwright test`) targeting the deployed dev/qa environment; E2E tests MUST cover all critical user journeys: patient registration (FR-001), appointment booking (FR-005), preferred slot swap (FR-011), AI conversational intake (FR-015), clinical document upload (FR-032), and ICD-10 code suggestion verification (FR-037–FR-039)
- **CICD-033**: Pipeline MUST run performance baseline checks using **k6** against the staging environment before every production promotion; P95 API response time MUST be ≤ 500 ms (NFR-002); slot availability reads MUST complete in ≤ 100 ms (NFR-002); any breach of these thresholds MUST block production deployment
- **CICD-034**: Pipeline MUST generate Playwright HTML test reports and k6 JSON summary reports as downloadable GitHub Actions artifacts; JUnit XML results MUST be published to the GitHub Actions test summary

### Stage 5: Deployment Validation

- **CICD-040**: Pipeline MUST validate all required GitHub Secrets are present before initiating deployment (`SUPABASE_CONNECTION_STRING`, `UPSTASH_REDIS_URL`, `GEMINI_API_KEY`, `JWT_SECRET_KEY`, `DATA_PROTECTION_KEY`, `SMTP_USERNAME`, `SMTP_PASSWORD`, and hosting credentials); a missing secret MUST abort the deployment with a descriptive error
- **CICD-041**: Pipeline MUST execute EF Core database migrations (`dotnet ef database update`) as a pre-deployment step before deploying the backend package; migration failures MUST abort the deployment and trigger automated rollback
- **CICD-042**: Pipeline MUST produce and archive a deployment package checksum (SHA-256) before upload; the deployed checksum MUST be verified post-deployment to confirm artifact integrity
- **CICD-043**: Pipeline MUST archive the previous deployment package (backend `.zip` and frontend `dist/` snapshot) to GitHub Artifacts before overwriting with the new version; retained per environment artifact retention policy to enable rollback

### Stage 6: Deployment

- **CICD-050**: Pipeline MUST deploy the React SPA `dist/` to InfinityFree via FTPS (`ftp-deploy` or `lftp` action) using encrypted FTP credentials from GitHub Secrets; pipeline MUST deploy the .NET API to MonsterASP via WebDeploy package upload using `MONSTERAPSP_WEBDEPLOY_URL`, `MONSTERAPSP_USERNAME`, and `MONSTERAPSP_PASSWORD` secrets
- **CICD-051**: Pipeline MUST validate all deployment prerequisites before deployment: database connectivity (`GET /api/v1/health/db` from runner), Supabase storage accessibility, and Redis reachability; failed prerequisite checks MUST abort deployment
- **CICD-052**: Pipeline MUST run smoke tests immediately after every deployment:
  - `GET /api/v1/health` → HTTP 200
  - `GET /api/v1/health/db` → HTTP 200 (confirms database connectivity)
  - React app root URL → HTTP 200 with expected `<meta name="application-name"` present
  - `POST /api/v1/auth/token` with test credentials → HTTP 200 (confirms auth pipeline)
  Smoke test failure MUST trigger automated rollback (CICD-053)
- **CICD-053**: Pipeline MUST support automated rollback on smoke test failure: restore the archived backend package via re-upload to MonsterASP WebDeploy, restore the frontend snapshot via FTPS, and re-apply the previous EF Core migration state; rollback completion MUST trigger a Critical notification to the on-call team
- **CICD-054**: Pipeline MUST tag the Git commit with the deployment version (`v{semver}-{env}`) and create a GitHub Release for every successful production deployment, attaching the deployment manifest (version, commit SHA, migration state, deployment timestamp)

### Stage 7: Approval Gates

- **CICD-060**: Pipeline MUST require **1 human approval** from a designated reviewer before deploying to the `staging` environment; the approval request MUST be sent via GitHub Environment Protection Rule with Slack webhook notification
- **CICD-061**: Pipeline MUST require **2 human approvals** from designated reviewers (DevOps Engineer + Development Lead or Security Engineer) before deploying to the `prod` environment; both approvals MUST be recorded in the GitHub Actions audit log
- **CICD-062**: Staging approval requests MUST expire after **24 hours** if no action is taken; production approval requests MUST expire after **72 hours**; expired requests MUST cancel the pending deployment and notify approvers
- **CICD-063**: Pipeline MUST notify designated approvers via GitHub Environment Protection email notification and an optional Slack webhook (`SLACK_WEBHOOK_URL` secret) when an approval is requested, approved, rejected, or expired
- **CICD-064**: Pipeline MUST log all approval decisions (approver identity, timestamp, decision, environment) to the GitHub Actions audit trail; no approval bypass mechanism is permitted

---

## Environment Pipeline Matrix

| Stage | dev | qa | staging | prod |
|-------|-----|----|---------|------|
| Build (frontend + backend) | Auto | Auto | Auto | Auto |
| ESLint + StyleCop Lint | Auto | Auto | Auto | Auto |
| Unit Tests (≥80% coverage) | Auto | Auto | Auto | Auto |
| CodeQL SAST | Auto | Auto | Auto | Auto |
| Snyk SCA (npm + NuGet) | Auto | Auto | Auto | Auto |
| npm audit + dotnet vuln | Auto | Auto | Auto | Auto |
| GitLeaks Secrets Scan | Auto | Auto | Auto | Auto |
| PHI Audit Check | Auto | Auto | Auto | Auto |
| EF Core Migration Validation | Auto | Auto | Auto | Auto |
| Integration Tests | Skip | Auto | Auto | Auto |
| E2E Tests (Playwright) | Skip | Auto | Auto | Auto |
| Performance Tests (k6) | Skip | Skip | Auto | Auto |
| Prerequisite Validation | Auto | Auto | Auto | Auto |
| Manual Approval | No | No | Yes (1) | Yes (2) |
| EF Core Migration Apply | Auto | Auto | After Approval | After Approval |
| Deploy Frontend (FTPS) | Auto | Auto | After Approval | After Approval |
| Deploy Backend (WebDeploy) | Auto | Auto | After Approval | After Approval |
| Smoke Tests | Auto | Auto | Auto | Auto |
| Automated Rollback (on failure) | No | No | Auto | Auto |
| Git Tag + GitHub Release | No | No | No | Auto |
| Artifact Archival (pre-deploy) | Yes | Yes | Yes | Yes |

---

## Security Gates Configuration

| Gate | Tool | Threshold | Blocking | Environments | SARIF Upload |
|------|------|-----------|----------|--------------|--------------|
| SAST | CodeQL (`csharp` + `javascript-typescript`) | 0 Critical, 0 High | Yes | All | Yes |
| SCA | Snyk (npm + NuGet) | 0 Critical, 0 High | Yes | All | Yes |
| Dependency Audit | npm audit + dotnet vuln | 0 Critical | Yes | All | No |
| Secrets Detection | GitLeaks v2.3 | 0 findings | Yes | All | Yes |
| PHI Log Audit | Custom grep/regex step | 0 PHI pattern matches | Yes | All | No |
| Code Coverage | Jest + coverlet | ≥ 80% (both layers) | Yes | All | No |
| Container Scan | N/A (no containers) | — | — | — | — |
| IaC Scan | N/A (no IaC) | — | — | — | — |

> **OWASP Alignment**: Security gates enforce OWASP A02 (cryptographic failures via secrets scanning), A05 (misconfiguration via SAST), A06 (vulnerable components via SCA), and A03 (injection via SAST CodeQL).

---

## Deployment Strategy

### Strategy Per Environment

| Environment | Strategy | Approval | Rollback | Smoke Tests |
|-------------|----------|----------|----------|-------------|
| dev | Direct replacement (FTP + WebDeploy) | None | Manual (rerun previous build) | Basic (health + root) |
| qa | Direct replacement (FTP + WebDeploy) | None | Manual (rerun previous build) | Basic (health + root) |
| staging | Snapshot + replace with pre-deploy archive | 1 approver | Automated (archive restore) | Full suite |
| prod | Snapshot + replace with pre-deploy archive | 2 approvers | Automated (archive restore) | Full suite + performance |

### Production Deployment Procedure (Adapted Canary for IIS)

Since true canary routing is not supported on MonsterASP shared hosting, production deployment uses a **staged approval pattern**:

1. Full E2E + performance test suite executes against **staging** environment
2. Staging must pass for ≥ 30 minutes with zero smoke test failures
3. **2 human approvals** required (GitHub Environment Protection)
4. Backend package deployed via WebDeploy; frontend `dist/` deployed via FTPS
5. Smoke tests execute immediately post-deployment (CICD-052)
6. If smoke tests fail within 5 minutes → automated rollback (CICD-053)
7. If smoke tests pass → Git tag + GitHub Release created (CICD-054)

---

## Rollback Procedures

### Automated Rollback Triggers

| Trigger | Threshold | Environments |
|---------|-----------|--------------|
| Smoke test failure | Any failure within 5 min of deployment | staging, prod |
| Health endpoint failure | 3 consecutive failures (30s apart) | staging, prod |
| EF Core migration failure | Any migration error | All |

> **Note**: Error rate monitoring (>5%) and P99 latency monitoring require external observability tooling (e.g., Datadog, New Relic). These are deferred to Phase 2 due to free-tier constraint (NFR-010). For Phase 1, rollback relies on smoke test failure detection.

### Rollback Steps

1. **Detect**: Smoke test failure triggers rollback job automatically via GitHub Actions `needs` failure condition
2. **Backend Restore**: Re-upload archived `.zip` package to MonsterASP via WebDeploy (artifact downloaded from GitHub Actions storage)
3. **Frontend Restore**: Restore archived `dist/` snapshot to InfinityFree via FTPS
4. **Database Restore**: Apply previous EF Core migration target via `dotnet ef database update {PreviousMigration}` — DBA review REQUIRED for destructive schema rollbacks; documented as manual step for data-loss migrations
5. **Verify**: Re-run smoke tests against restored deployment
6. **Notify**: Trigger Critical notification to on-call team and create GitHub Issue automatically
7. **Document**: Rollback event logged with timestamp, environment, version rolled back from/to, and smoke test failure details

### Artifact Retention Policy

| Environment | Deployment Archives | Test Reports | Build Artifacts |
|-------------|--------------------|-----------|----|
| dev | 7 days | 7 days | 7 days |
| qa | 30 days | 30 days | 14 days |
| staging | 90 days | 90 days | 30 days |
| prod | 365 days | 365 days | 90 days |

---

## Branch Strategy and Pipeline Triggers

### Branch Triggers

| Branch Pattern | Pipeline Type | Target Environment | Notes |
|----------------|--------------|-------------------|-------|
| `feature/*` | CI only (Build + Lint + Unit + Security) | No deployment | PR gate only |
| `develop` | CI + CD | dev → qa | Integration + E2E on qa |
| `release/*` | CI + CD | staging | 1 approval required |
| `main` | CI + CD | prod | 2 approvals required |
| `hotfix/*` | CI + CD (expedited) | staging → prod | Same approval requirements; no skipping security gates |

### Pull Request Gates (Required Status Checks)

All PRs targeting `develop`, `release/*`, or `main` MUST pass:
- Build (frontend + backend)
- ESLint + StyleCop
- Unit tests (≥80% coverage)
- CodeQL SAST
- Snyk SCA
- GitLeaks secrets scan
- PHI audit check

---

## Secrets Configuration

### Required Secrets (GitHub Repository / Environment Secrets)

| Secret Name | Purpose | Scope | Rotation |
|-------------|---------|-------|----------|
| `{ENV}_SUPABASE_CONNECTION_STRING` | PostgreSQL connection (per environment) | Environment | 90 days |
| `{ENV}_UPSTASH_REDIS_URL` | Redis cache connection | Environment | 90 days |
| `GEMINI_API_KEY` | Google AI Gemini API access | Repository | 365 days |
| `{ENV}_JWT_SECRET_KEY` | JWT signing key (per environment) | Environment | 90 days |
| `{ENV}_DATA_PROTECTION_KEY` | ASP.NET Core AES-256-GCM key | Environment | 90 days |
| `{ENV}_SMTP_USERNAME` | Email sender credentials | Environment | 90 days |
| `{ENV}_SMTP_PASSWORD` | Email sender credentials | Environment | 90 days |
| `GOOGLE_CLIENT_ID` | OAuth 2.0 calendar integration | Repository | On rotation |
| `GOOGLE_CLIENT_SECRET` | OAuth 2.0 calendar integration | Repository | On rotation |
| `MICROSOFT_CLIENT_ID` | OAuth 2.0 calendar integration | Repository | On rotation |
| `MICROSOFT_CLIENT_SECRET` | OAuth 2.0 calendar integration | Repository | On rotation |
| `{ENV}_INFINITYFREE_FTP_USERNAME` | Frontend FTP deploy | Environment | 90 days |
| `{ENV}_INFINITYFREE_FTP_PASSWORD` | Frontend FTP deploy | Environment | 90 days |
| `{ENV}_MONSTERAPSP_WEBDEPLOY_URL` | Backend WebDeploy endpoint | Environment | On change |
| `{ENV}_MONSTERAPSP_USERNAME` | Backend WebDeploy credentials | Environment | 90 days |
| `{ENV}_MONSTERAPSP_PASSWORD` | Backend WebDeploy credentials | Environment | 90 days |
| `SNYK_TOKEN` | SCA scanning | Repository | 365 days |
| `GITLEAKS_LICENSE` | GitLeaks action license | Repository | Annual |
| `SLACK_WEBHOOK_URL` | Deployment and rollback notifications | Repository | On rotation |

> **Security Policy**: All secrets are accessed exclusively via `${{ secrets.NAME }}` syntax. Secrets are NEVER echoed, logged, or included in artifact outputs. GitHub Actions secret masking is relied upon as a secondary defence only — primary protection is not logging secrets at all.

> **OWASP A02**: Secrets are sourced from environment variables at runtime, never committed to source code (DR-001, TR-015).

### Environments

GitHub Actions Environments configured:
- `dev` — No protection rules; auto-deploy on push to `develop`
- `qa` — No protection rules; auto-deploy after `dev` succeeds
- `staging` — Required reviewers: 1; wait timer: 0; deployment branch: `release/*`
- `prod` — Required reviewers: 2; wait timer: 0; deployment branch: `main`

---

## Notification Strategy

| Event | Recipients | Channel | Priority |
|-------|------------|---------|----------|
| Build / Lint failure | Commit author, team | GitHub Actions email + Slack | High |
| Security gate failure (any) | Commit author, security team | GitHub Actions email + Slack | Critical |
| PHI audit failure | Commit author, security lead | GitHub Actions email + Slack | Critical |
| Integration / E2E test failure | Commit author, QA team | GitHub Actions email + Slack | High |
| Deployment started | Operations team | Slack | Info |
| Deployment completed successfully | Operations team, stakeholders | Slack | Info |
| Approval required | Designated approvers | GitHub Environment email + Slack | High |
| Approval expired | Designated approvers, DevOps | GitHub Environment email + Slack | High |
| Rollback executed | On-call team, management, security | Slack (Critical) + GitHub Issue auto-created | Critical |
| Production release tagged | All stakeholders | GitHub Release notification | Info |

---

## Requirement Traceability

| CICD ID | Description | Source Requirement(s) |
|---------|-------------|----------------------|
| CICD-001 | Build all artifacts | TR-001 (React), TR-002 (.NET) |
| CICD-002 | Dependency resolution | TR-001, TR-002 |
| CICD-003 | Build metadata generation | TR-012 (CI/CD), NFR-007 (audit traceability) |
| CICD-004 | Fail on compilation error | TR-002, NFR-012 |
| CICD-010 | Linting (ESLint + StyleCop) | NFR-012 (maintainability) |
| CICD-011 | 80% code coverage threshold | NFR-012 (testability, clean architecture) |
| CICD-012 | Fail on quality degradation | NFR-012 |
| CICD-013 | Quality report generation | NFR-012 |
| CICD-014 | EF Core migration validation | TR-003, DR-002 (referential integrity) |
| CICD-020 | SAST via CodeQL | NFR-004, NFR-011 (HIPAA), NFR-005 (RBAC) |
| CICD-021 | SCA via Snyk | NFR-004, NFR-011, OWASP A06 |
| CICD-022 | npm audit + dotnet vuln | NFR-004, NFR-011, OWASP A06 |
| CICD-023 | Secrets detection via GitLeaks | DR-001, TR-015, NFR-004 (secrets never in source) |
| CICD-024 | Block on Critical/High findings | NFR-004, NFR-011 (HIPAA security) |
| CICD-025 | PHI audit check | DR-001, NFR-007, NFR-011 (HIPAA compliance) |
| CICD-030 | Unit tests with coverage | NFR-012 (testability) |
| CICD-031 | Integration tests | NFR-002 (P95 ≤ 500ms), NFR-009 (concurrency) |
| CICD-032 | E2E tests (Playwright) | FR-001–FR-005, FR-011, FR-015, FR-032, FR-037–FR-039 |
| CICD-033 | Performance tests (k6) | NFR-002 (P95 ≤ 500ms, slot reads ≤ 100ms) |
| CICD-034 | Test report artifacts | NFR-012, TR-012 |
| CICD-040 | Secrets presence validation | DR-001, TR-012, TR-015 |
| CICD-041 | EF Core migration apply | TR-003, DR-002, DR-003 |
| CICD-042 | Deployment artifact checksum | NFR-004 (integrity), TR-012 |
| CICD-043 | Pre-deploy archive | NFR-001 (99.9% uptime via rollback capability) |
| CICD-050 | Deploy frontend + backend | TR-001, TR-002, TR-012 |
| CICD-051 | Prerequisite validation | NFR-001, NFR-002 |
| CICD-052 | Smoke tests post-deployment | NFR-001 (uptime validation) |
| CICD-053 | Automated rollback | NFR-001 (99.9% uptime preservation) |
| CICD-054 | Git tag + GitHub Release | TR-012, NFR-007 (auditability) |
| CICD-060 | 1 approval for staging | TR-012, organisational deployment policy |
| CICD-061 | 2 approvals for prod | TR-012, organisational deployment policy |
| CICD-062 | Approval timeouts | Deployment governance policy |
| CICD-063 | Approver notifications | TR-012, deployment governance |
| CICD-064 | Approval audit log | NFR-007 (immutable audit), TR-012 |

---

## HIPAA-Specific Pipeline Considerations

| Concern | Pipeline Control | Requirement |
|---------|-----------------|-------------|
| PHI never in source code | GitLeaks + PHI audit check (CICD-023, CICD-025) | DR-001, TR-015 |
| Encryption key management | Secrets validation (CICD-040); keys sourced from GitHub Secrets | TR-015, NFR-004 |
| Dependency vulnerabilities | Snyk + npm audit + dotnet vuln (CICD-021, CICD-022) | NFR-011, OWASP A06 |
| Immutable audit trail | GitHub Actions audit log; deployment Git tags (CICD-054, CICD-064) | NFR-007 |
| Access control to prod secrets | GitHub Environment `prod` — restricted to 2 approvers only | NFR-005, NFR-011 |
| BAA-covered infrastructure only | Pipeline validates deployment targets only (InfinityFree + MonsterASP) | NFR-011 |

---

## Human Review Checklist

### Security

- [ ] All security gates (CICD-020–025) are configured as non-skippable blocking steps
- [ ] No secrets appear in any pipeline YAML file or step definition
- [ ] `continue-on-error: true` is absent from all security job steps
- [ ] PHI audit check (CICD-025) regex patterns are reviewed and up-to-date
- [ ] Staging (1) and production (2) approval requirements match organisational policy
- [ ] GitHub Environment protection rules are configured for `staging` and `prod`
- [ ] SARIF upload confirmed for CodeQL, Snyk, and GitLeaks findings

### Testing

- [ ] 80% coverage threshold is appropriate for both frontend and backend
- [ ] Playwright E2E tests cover all critical user journeys per CICD-032
- [ ] k6 performance baselines (P95 ≤ 500ms, slot reads ≤ 100ms) are set per NFR-002
- [ ] Integration test database isolation is confirmed (test Supabase project or in-memory)

### Deployment

- [ ] Rollback procedure is documented and tested against staging environment
- [ ] Pre-deploy archive step confirmed before every deployment (CICD-043)
- [ ] EF Core migration rollback procedure reviewed for data-loss scenarios (manual DBA step documented)
- [ ] Smoke test suite (CICD-052) covers all critical health endpoints
- [ ] FTP credentials use FTPS (encrypted) — plain FTP is prohibited

### Notifications

- [ ] Slack webhook configured and tested for Critical events (rollback, security failure)
- [ ] GitHub Issues auto-creation on rollback is configured
- [ ] All Critical notification paths have been validated with a test deployment

---

## Approval

| Role | Name | Date | Signature |
|------|------|------|-----------|
| DevOps Engineer | | | |
| Security Engineer | | | |
| Development Lead | | | |
