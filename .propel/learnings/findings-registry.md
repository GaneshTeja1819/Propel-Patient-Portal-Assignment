<!-- Schema: ./findings-registry-schema.md -->
# Findings Registry

## Index

| File | Finding IDs |
|------|-------------|
| `.env` | F001 |
| `backend/src/UPACIP.API/appsettings.json` | F002 |
| `frontend/src/hooks/useAIIntake.ts` | F003 |
| `frontend/src/context/AuthContext.tsx` | F004 |
| `frontend/src/components/intake/` | F005 |
| `backend/src/UPACIP.API/Controllers/IntakeController.cs` | F006 |
| `backend/src/UPACIP.Infrastructure/UPACIP.Infrastructure.csproj` | F007 |
| `backend/src/UPACIP.Infrastructure/AI/GeminiIntakeAdapter.cs` | F008 |
| `frontend/src/App.tsx` | F009, F010 |
| `frontend/src/hooks/useAIIntake.ts` (test gap) | F011 |
| `backend/src/UPACIP.API/appsettings.json` (git history) | F012 |
| `frontend/src/components/auth/ProtectedRoute.tsx` | F013 |
| `frontend/src/hooks/useAIIntake.ts` (abort leak) | F014 |
| `frontend/src/pages/IntakePage.tsx` | F015, F016, F017 |
| `backend/src/UPACIP.API/Controllers/DocumentsController.cs` | F018 |
| `backend/src/UPACIP.Infrastructure/AI/GeminiIntakeAdapter.cs` | F008, F019 |
| `backend/src/UPACIP.Infrastructure/AI/IntakeSessionService.cs` | F020 |
| `backend/src/UPACIP.Application/Handlers/Documents/UploadDocumentHandler.cs` | F021 |

## Entries

```yaml
- id: F001
  file: .env
  cat: code-review
  type: finding
  severity: CRITICAL
  issue: Live DB credentials and API key committed to VCS
  cause: .env file with real Supabase password and Context7 key not in .gitignore
  date: 2026-05-18
  workflow: review-code

- id: F002
  file: backend/src/UPACIP.API/appsettings.json
  cat: code-review
  type: finding
  severity: CRITICAL
  issue: Production database password hardcoded in committed appsettings
  cause: DefaultConnection connection string contains plaintext live DB credentials
  date: 2026-05-18
  workflow: review-code
  resolved: partial
  resolved_date: 2026-05-19
  resolved_note: File is now clean (commit b89fd5d), but git history (HEAD~1) still contains plaintext password. See F012 for tracking.

- id: F003
  file: frontend/src/hooks/useAIIntake.ts
  cat: code-review
  type: finding
  severity: HIGH
  issue: PHI captured fields stored as plaintext in sessionStorage
  cause: saveToStorage serialises capturedFields as unencrypted JSON without Web Crypto
  date: 2026-05-18
  workflow: review-code
  resolved: true
  resolved_date: 2026-05-19
  resolved_in: commit b89fd5d — stripPhiValues() strips all field values before storage

- id: F004
  file: frontend/src/context/AuthContext.tsx
  cat: code-review
  type: finding
  severity: HIGH
  issue: Auth stub defaults to null token allowing unauthenticated API calls
  cause: AuthContext is a placeholder with no real AuthProvider wrapping the app
  date: 2026-05-18
  workflow: review-code

- id: F005
  file: frontend/src/components/intake/
  cat: code-review
  type: finding
  severity: HIGH
  issue: Zero unit test coverage for all US_018 intake components
  cause: No test files created alongside 11 new source files in US_018 implementation
  date: 2026-05-18
  workflow: review-code

- id: F006
  file: backend/src/UPACIP.API/Controllers/IntakeController.cs
  cat: code-review
  type: finding
  severity: CRITICAL
  issue: Broken appointment ownership — any authenticated patient can access any session
  cause: ValidateAppointmentOwnership only checks sub claim presence; does not query DB to verify appointment belongs to caller
  date: 2026-05-18
  workflow: review-code

- id: F007
  file: backend/src/UPACIP.Infrastructure/UPACIP.Infrastructure.csproj
  cat: code-review
  type: finding
  severity: HIGH
  issue: Newtonsoft.Json 11.0.1 — known HIGH vulnerability GHSA-5crp-9r3c-p9vr (ReDoS)
  cause: Outdated package version pinned; upgrade to >= 13.0.3 required
  date: 2026-05-18
  workflow: review-code

- id: F008
  file: backend/src/UPACIP.Infrastructure/AI/GeminiIntakeAdapter.cs
  cat: code-review
  type: finding
  severity: HIGH
  issue: No per-session rate limiting on Gemini API calls — unbounded API spend possible
  cause: POST /intake/answer calls Gemini on every request with no session-level call count cap
  date: 2026-05-18
  workflow: review-code

- id: F009
  file: frontend/src/App.tsx
  cat: code-review
  type: finding
  severity: HIGH
  issue: Missing /login route — ProtectedRoute redirect target renders blank page
  cause: App.tsx registers /intake/:appointmentId and / but no /login route; React Router v6 renders null for unmatched paths causing silent blank screen on auth redirect
  date: 2026-05-19
  workflow: review-code
  note: Risk escalated in commit b89fd5d — ProtectedRoute now actively wired; /login still absent

- id: F010
  file: frontend/src/App.tsx
  cat: code-review
  type: finding
  severity: HIGH
  issue: No AuthProvider wrapping App — useAuth() returns null context; all API calls are unauthenticated
  cause: App.tsx mounts BrowserRouter + Routes without wrapping in AuthProvider; default context has token=null
  date: 2026-05-19
  workflow: review-code

- id: F011
  file: frontend/src/hooks/useAIIntake.ts
  cat: code-review
  type: finding
  severity: HIGH
  issue: Zero unit test coverage for 358-line useAIIntake hook including HIPAA-critical stripPhiValues
  cause: No useAIIntake.test.ts created alongside the hook; F005 continues for new hook file
  date: 2026-05-19
  workflow: review-code

- id: F012
  file: backend/src/UPACIP.API/appsettings.json
  cat: code-review
  type: finding
  severity: CRITICAL
  issue: Supabase production credentials remain readable in git history (HEAD~1) after partial fix commit b89fd5d
  cause: git filter-repo not run; git show HEAD~1:backend/src/UPACIP.API/appsettings.json exposes password qSQ8Q28zcNxHRRVw
  date: 2026-05-19
  workflow: review-code
  review_ref: review_20260519_225736 CR-001
  action_required: Rotate Supabase DB password AND purge credential from git history using git filter-repo

- id: F013
  file: frontend/src/components/auth/ProtectedRoute.tsx
  cat: code-review
  type: finding
  severity: HIGH
  issue: Hard-wired DEV auth bypass unconditionally skips all authentication in development environments
  cause: if (import.meta.env.DEV) return children bypasses token check; safe in Vite production but risky in misconfigured staging/CI
  date: 2026-05-19
  workflow: review-code
  review_ref: review_20260519_225736 CR-005

- id: F014
  file: frontend/src/hooks/useAIIntake.ts
  cat: code-review
  type: finding
  severity: HIGH
  issue: AbortController created in startSession is never aborted on component unmount causing state update on unmounted component
  cause: ac.abort() never called; IntakePage useEffect has no cleanup return; React StrictMode causes double POST to /intake/start
  date: 2026-05-19
  workflow: review-code
  review_ref: review_20260519_225736 CR-004

- id: F015
  file: frontend/src/pages/IntakePage.tsx
  cat: code-review
  type: finding
  severity: MEDIUM
  issue: srOnly inline style object duplicates .sr-only global CSS class added in same commit — DRY violation
  cause: IntakePage.tsx defines const srOnly at module scope (lines 26-36) with identical properties to global.css .sr-only class added in b89fd5d
  date: 2026-05-19
  workflow: review-code
  review_ref: review_20260519_232015 CR-006

- id: F016
  file: frontend/src/pages/IntakePage.tsx
  cat: code-review
  type: finding
  severity: MEDIUM
  issue: document.title hardcoded to 'AI Intake | Patient Portal' regardless of intake mode — WCAG 2.4.2 violation in manual mode
  cause: useEffect depends only on [confirmed]; mode is not a dependency; title never updated when patient switches to manual mode
  date: 2026-05-19
  workflow: review-code
  review_ref: review_20260519_232015 CR-008

- id: F017
  file: frontend/src/pages/IntakePage.tsx
  cat: code-review
  type: finding
  severity: MEDIUM
  issue: h1 always reads 'AI Conversational Intake' even when mode === 'manual' — WCAG 1.3.1 violation
  cause: Visually hidden h1 at line 268 is static string not conditional on mode; screen reader users in manual mode receive incorrect heading
  date: 2026-05-19
  workflow: review-code
  review_ref: review_20260519_232015 CR-009
  resolved: true
  resolved_date: 2026-05-20
  resolved_note: h1 is now conditional on mode in current IntakePage.tsx

- id: F018
  file: backend/src/UPACIP.API/Controllers/DocumentsController.cs
  cat: code-review
  type: finding
  severity: HIGH
  issue: No MIME type or magic-bytes validation — any file type accepted as PDF
  cause: file.ContentType is attacker-controlled; no PDF magic-bytes check before Supabase storage and PdfPig dispatch
  date: 2026-05-20
  workflow: review-code
  review_ref: review_20260520_120000 CR-003

- id: F019
  file: backend/src/UPACIP.Infrastructure/AI/GeminiIntakeAdapter.cs
  cat: code-review
  type: finding
  severity: MEDIUM
  issue: Prompt injection fence breakable — </patient_answer> not escaped in patient answer
  cause: rawAnswer interpolated directly without XML-escaping; attacker can break fence and inject model instructions
  date: 2026-05-20
  workflow: review-code
  review_ref: review_20260520_120000 CR-010

- id: F020
  file: backend/src/UPACIP.Infrastructure/AI/IntakeSessionService.cs
  cat: code-review
  type: finding
  severity: MEDIUM
  issue: Static _memoryStore has no TTL — PHI accumulates indefinitely in process memory when Redis absent
  cause: ConcurrentDictionary<string,string> is static; entries never evicted; PHI remains in heap until process restart
  date: 2026-05-20
  workflow: review-code
  review_ref: review_20260520_120000 CR-011

- id: F021
  file: backend/src/UPACIP.Application/Handlers/Documents/UploadDocumentHandler.cs
  cat: code-review
  type: finding
  severity: LOW
  issue: Audit metadata JSON built with string interpolation — unescaped double-quote corrupts JSON
  cause: $"{{\"documentType\":\"{command.DocumentType}\"..." — user-supplied values not serialized via JsonSerializer
  date: 2026-05-20
  workflow: review-code
  review_ref: review_20260520_120000 CR-013
```
