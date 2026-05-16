# GitHub Actions Pipeline — Unified Patient Access & Clinical Intelligence Platform

## Overview

Production-ready CI/CD pipelines for the Unified Patient Access & Clinical Intelligence Platform.
Platform: **GitHub Actions** | Stack: **React 18 + ASP.NET Core 8** | Deployment: **InfinityFree (FTPS) + MonsterASP IIS (FTPS)**

---

## Workflow Files

| File | Purpose | Trigger | Environment |
|------|---------|---------|-------------|
| `ci.yml` | Build, lint, unit tests, all security gates | Push/PR to any branch | — |
| `cd-dev.yml` | Deploy to dev | CI passes on `develop` | `dev` (no approval) |
| `cd-qa.yml` | Integration, E2E, deploy to QA | Dev deploy succeeds on `develop` | `qa` (no approval) |
| `cd-staging.yml` | Deploy to staging | Push to `release/**` or `hotfix/**` | `staging` (**1 approval**) |
| `cd-prod.yml` | Deploy to production | Push to `main` | `production` (**2 approvals**) |
| `security-scan.yml` | Scheduled deep security scan | Weekly (Sun 00:00 UTC) + push to main/develop | — |

---

## CICD Requirement Coverage

| CICD ID | Requirement | Workflow |
|---------|-------------|----------|
| CICD-001 | Build all artifacts | `ci.yml` → build-frontend, build-backend |
| CICD-002 | Dependency resolution | `ci.yml` → npm ci, dotnet restore |
| CICD-003 | Build metadata | `ci.yml` → generate-metadata step |
| CICD-004 | Fail on compilation error | `ci.yml` → default exit behavior |
| CICD-010 | Lint (ESLint + StyleCop) | `ci.yml` → lint-frontend, lint-backend |
| CICD-011 | Coverage ≥ 80% | `ci.yml` → unit-test-frontend, unit-test-backend |
| CICD-012 | Fail on quality degradation | `ci.yml` → TreatWarningsAsErrors + coverage threshold |
| CICD-013 | Quality reports | `ci.yml` → upload-artifact (JUnit, LCOV, Cobertura) |
| CICD-014 | EF Core migration validation | `ci.yml` → validate-migrations |
| CICD-020 | SAST via CodeQL | `ci.yml` + `security-scan.yml` → sast-codeql |
| CICD-021 | SCA via Snyk | `ci.yml` + `security-scan.yml` → sca-snyk |
| CICD-022 | npm audit + dotnet vuln | `ci.yml` + `security-scan.yml` → dependency-audit |
| CICD-023 | GitLeaks secrets scan | `ci.yml` + `security-scan.yml` → secrets-detection |
| CICD-024 | Block on Critical/High + SARIF | All security jobs, upload-sarif steps |
| CICD-025 | PHI audit check (HIPAA) | `ci.yml` + `security-scan.yml` → phi-audit |
| CICD-030 | Unit tests | `ci.yml` → unit-test-frontend, unit-test-backend |
| CICD-031 | Integration tests | `cd-qa.yml` → integration-tests |
| CICD-032 | E2E Playwright tests | `cd-qa.yml` → e2e-playwright |
| CICD-033 | k6 Performance tests | `cd-staging.yml` + `cd-prod.yml` → performance-test |
| CICD-034 | Test reports | All CD workflows → artifact uploads |
| CICD-040 | Secrets validation | All CD workflows → validate-secrets |
| CICD-041 | EF Core migration apply | All CD workflows → apply-migrations |
| CICD-042 | Deployment checksums | All CD workflows → sha256sum steps |
| CICD-043 | Pre-deploy archive | `cd-staging.yml` + `cd-prod.yml` → pre-deploy-archive |
| CICD-050 | Deploy frontend + backend | All CD workflows → deploy-frontend, deploy-backend |
| CICD-051 | Prerequisite validation | All CD workflows → prerequisite-check |
| CICD-052 | Smoke tests | All CD workflows → smoke-test |
| CICD-053 | Automated rollback | `cd-staging.yml` + `cd-prod.yml` → rollback |
| CICD-054 | Git tag + GitHub Release | `cd-prod.yml` → tag-release |
| CICD-060 | 1 approval (staging) | `cd-staging.yml` → GitHub Environment: staging |
| CICD-061 | 2 approvals (production) | `cd-prod.yml` → GitHub Environment: production |
| CICD-062 | Approval timeouts | GitHub Environment protection rules (24h staging, 72h prod) |
| CICD-063 | Approver notifications | Slack webhook on all approval events |
| CICD-064 | Approval audit log | GitHub Actions built-in environment audit trail |

---

## Branch Strategy

```
feature/*  →  CI only (no deployment)
    ↓
develop  →  CI + deploy dev + deploy qa (auto)
    ↓
release/*  →  CI + deploy staging (1 approval required)
    ↓
main  →  CI + performance test + deploy prod (2 approvals required)
```

Hotfixes: `hotfix/**` → staging approval → prod approval (same gates, no bypassing).

---

## GitHub Environment Setup

Configure these environments in **Repository Settings → Environments**:

### `dev`
- **Protection rules:** None
- **Deployment branch:** `develop`
- **Secrets:** `DEV_*` prefix (see secrets table)

### `qa`
- **Protection rules:** None
- **Deployment branch:** `develop`
- **Secrets:** `QA_*` prefix

### `staging`
- **Protection rules:** Required reviewers: **1**
- **Deployment branch:** `release/**`, `hotfix/**`
- **Wait timer:** 0 minutes
- **Secrets:** `STAGING_*` prefix
- **Note:** Configure 24-hour timeout in environment protection rules (CICD-062)

### `production`
- **Protection rules:** Required reviewers: **2** (DevOps + Dev Lead or Security Engineer)
- **Deployment branch:** `main`
- **Prevent self-review:** Enabled
- **Wait timer:** 0 minutes
- **Secrets:** `PROD_*` prefix
- **Note:** Configure 72-hour timeout in environment protection rules (CICD-062)

---

## Required GitHub Secrets

### Repository-Level Secrets

| Secret | Purpose |
|--------|---------|
| `GEMINI_API_KEY` | Google AI Gemini API |
| `GOOGLE_CLIENT_ID` | Google Calendar OAuth |
| `GOOGLE_CLIENT_SECRET` | Google Calendar OAuth |
| `MICROSOFT_CLIENT_ID` | Microsoft Graph OAuth |
| `MICROSOFT_CLIENT_SECRET` | Microsoft Graph OAuth |
| `SNYK_TOKEN` | Snyk SCA scanning |
| `GITLEAKS_LICENSE` | GitLeaks action |
| `SLACK_WEBHOOK_URL` | Deployment notifications |

### Environment-Scoped Secrets (`{ENV}` = DEV / QA / STAGING / PROD)

| Secret Pattern | Purpose |
|----------------|---------|
| `{ENV}_SUPABASE_CONNECTION_STRING` | PostgreSQL connection |
| `{ENV}_UPSTASH_REDIS_URL` | Redis cache |
| `{ENV}_JWT_SECRET_KEY` | JWT signing key |
| `{ENV}_DATA_PROTECTION_KEY` | AES-256-GCM encryption key |
| `{ENV}_SMTP_USERNAME` | Email SMTP credentials |
| `{ENV}_SMTP_PASSWORD` | Email SMTP credentials |
| `{ENV}_INFINITYFREE_FTP_HOST` | Frontend FTP host |
| `{ENV}_INFINITYFREE_FTP_USERNAME` | Frontend FTPS username |
| `{ENV}_INFINITYFREE_FTP_PASSWORD` | Frontend FTPS password |
| `{ENV}_MONSTERAPSP_FTP_HOST` | Backend FTP host |
| `{ENV}_MONSTERAPSP_USERNAME` | Backend FTPS username |
| `{ENV}_MONSTERAPSP_PASSWORD` | Backend FTPS password |
| `{ENV}_BACKEND_URL` | Backend base URL (e.g., `https://api.dev.example.com`) |
| `{ENV}_FRONTEND_URL` | Frontend base URL (e.g., `https://dev.example.com`) |

---

## Project Structure Assumptions

The workflows assume the repository has this layout:

```
/
├── frontend/          # React 18 SPA (Vite)
│   ├── package.json
│   └── package-lock.json
├── backend/           # ASP.NET Core 8 Web API
│   ├── src/
│   │   ├── API/
│   │   ├── Application/
│   │   ├── Domain/
│   │   └── Infrastructure/
│   └── tests/
│       ├── Unit/      # Category=Unit
│       └── Integration/  # Category=Integration
├── e2e/               # Playwright E2E tests
│   ├── package.json
│   └── package-lock.json
└── tests/
    └── performance/   # k6 scripts
        ├── smoke.js
        └── slot-availability.js
```

---

## Security Gates

> **HIPAA COMPLIANCE**: All security gates are **unconditionally blocking** on all environments.
> `continue-on-error: true` is **prohibited** on any security job step.
> Source: `cicd-pipeline-standards` rule, CICD-024.

| Gate | Tool | Threshold | Environments | SARIF |
|------|------|-----------|--------------|-------|
| SAST | CodeQL (C# + JS/TS) | 0 Critical/High | All | ✅ |
| SCA | Snyk (npm + NuGet) | 0 Critical/High | All | ✅ |
| Dependency Audit | npm audit + dotnet vuln | 0 Critical | All | — |
| Secrets | GitLeaks v2 | 0 findings | All | ✅ |
| PHI Audit | Custom regex (HIPAA) | 0 violations | All | — |
| Coverage | Jest + coverlet | ≥ 80% (both) | All | — |

---

## Smoke Test Endpoints

Post-deployment smoke tests verify:

| Endpoint | Expected | Verifies |
|----------|----------|---------|
| `GET /api/v1/health` | HTTP 200 | API is running |
| `GET /api/v1/health/db` | HTTP 200 | Database connectivity |
| `GET {FRONTEND_URL}` | HTTP 200 | SPA is served |
| `POST /api/v1/auth/token` (invalid body) | HTTP 400/422 | Auth pipeline active |

---

## Rollback Procedure

### Automatic (staging + production only)
1. Smoke test failure detected within 5 minutes of deployment
2. `rollback` job triggers automatically via `needs` failure condition
3. Previous artifacts restored via FTPS
4. Smoke tests re-run to confirm rollback
5. GitHub Issue created automatically
6. Critical Slack alert sent

### Manual (dev + qa)
1. Re-run previous passing workflow from GitHub Actions → Re-run workflow

### Database Schema Rollback
EF Core schema rollbacks require manual DBA review for data-loss migrations:
```bash
dotnet ef database update {PreviousMigrationName} \
  --connection "your_connection_string"
```
**Note:** Always take a Supabase PITR snapshot before running destructive migrations.

---

## Performance Test Scripts

k6 scripts must export a `default` function and `options`:

### `tests/performance/smoke.js`
```javascript
export const options = {
  thresholds: {
    http_req_duration: ['p(95)<500'],  // NFR-002: P95 ≤ 500ms
    http_req_failed: ['rate<0.01'],
  },
};
```

### `tests/performance/slot-availability.js`
```javascript
export const options = {
  thresholds: {
    http_req_duration: ['p(95)<100'],  // NFR-002: slot reads ≤ 100ms
    http_req_failed: ['rate<0.01'],
  },
};
```

---

## Human Review Checklist

### Before First Deployment
- [ ] All GitHub Environments created with correct protection rules
- [ ] All required secrets configured in each environment
- [ ] SNYK_TOKEN provisioned and tested
- [ ] GITLEAKS_LICENSE obtained and set
- [ ] SLACK_WEBHOOK_URL configured and tested
- [ ] `staging` environment: 1 required reviewer assigned
- [ ] `production` environment: 2 required reviewers assigned (prevent self-review enabled)
- [ ] 24-hour timeout configured for staging (CICD-062)
- [ ] 72-hour timeout configured for production (CICD-062)
- [ ] k6 performance scripts created at `tests/performance/`
- [ ] Playwright E2E tests present at `e2e/`
- [ ] Backend project paths match: `src/Infrastructure` and `src/API`

### Security Verification
- [ ] No `continue-on-error: true` on any security job step
- [ ] No secrets in any YAML file
- [ ] SARIF uploads configured for CodeQL, Snyk findings
- [ ] GitHub Code Scanning alerts enabled in repository settings
- [ ] PHI audit regex patterns reviewed with security lead

### Post-Deployment Validation
- [ ] All smoke test endpoints return expected status codes
- [ ] GitHub Release created with correct version tag
- [ ] Approval audit trail visible in GitHub Actions logs
