# Implementation Analysis — task_001_frontend-ai-intake-chat.md

## Verdict

**Status:** Conditional Pass

The implementation delivers all three acceptance criteria (AC-001, AC-003, AC-005) and both
edge cases, produces a clean TypeScript build, and passes ESLint with zero warnings. Two
medium-severity issues prevent an unconditional pass: the `isConfirming` prop is hardcoded
to `false`, creating a broken error-recovery path in the confirmation flow; and the
`data-uxr="UXR-502"` attribute passed to `IntakeProgressBar` is silently discarded because
the component does not forward it to its root DOM element.

## Traceability Matrix

| Requirement / Acceptance Criterion | Evidence (file: fn / line) | Result |
|---|---|---|
| AC-001: First Gemini question rendered as bot bubble on session init | `useAIIntake.ts: startSession()` L102–169; `IntakePage.tsx: useEffect` L85–90; `AIIntakeChat.tsx: AiBubble` L64–94 | Pass |
| AC-003: Summary of captured fields displayed for review + edit; no persistence until confirmed | `IntakeSummaryReview.tsx: FieldCard` L38–110; `IntakeSummaryReview.tsx: onConfirm → confirmIntake()` L168–172; `useAIIntake.ts: confirmIntake()` L298–316 | Pass |
| AC-005: Progress bar "Question N of M" with CSS fill % | `IntakeProgressBar.tsx` L23–49; `IntakePage.tsx: IntakeProgressBar` L218–224 | Pass |
| Edge Case: Mid-session browser refresh → session resumed | `useAIIntake.ts: saveToStorage / loadFromStorage` L47–60; `startSession()` resume branch L102–142 | Pass |
| Edge Case: Gemini rate-limit → `ManualFieldFallback` inline; mode → AI-Partial | `useAIIntake.ts: submitAnswer catch` L260–281; `AIIntakeChat.tsx: ManualFieldFallback` L18–54 | Pass |
| UXR-502: Accessible progress bar (`role="progressbar"`, `aria-valuenow/min/max`) | `IntakeProgressBar.tsx` L35–46 | Pass |
| UXR-402: PHI annotation — 🔒 icon + `--color-surface-phi` | `AIIntakeChat.tsx: phiNote` L75, `phiInput` CSS class; `IntakeSummaryReview.tsx: phiField` class L68 | Pass |
| UXR-103: No data loss on AI ↔ Manual switch | `IntakePage.tsx: handleModeSwitch` L95–102; `capturedFields` preserved in `useAIIntake` state | Pass |
| Route `/intake/:appointmentId` registered in App | `App.tsx` L6 | Pass |
| `sessionStorage` persistence keyed by `appointmentId` | `useAIIntake.ts: storageKey()` L44; `saveToStorage()` L47 | Pass |
| JWT bearer token on every API request | `useAIIntake.ts: buildHeaders()` L82–87 | Pass |
| Enter-to-send, Shift+Enter for newline | `AIIntakeChat.tsx: handleKeyDown` L155–159 | Pass |

## Logical & Design Findings

**Business Logic:**

- **[MEDIUM] F-001 — Broken error recovery after failed `confirmIntake()`:** When the
  user clicks "Confirm & Submit", `confirmIntake()` sets `phase = 'loading'`. Because
  `IntakeSummaryReview` is rendered only when `phase === 'summary'`, the summary screen
  disappears during the async call. On failure, `confirmIntake()` sets `phase = 'error'`,
  so the summary screen is permanently gone and the patient cannot retry confirmation
  without refreshing the page. The `isConfirming={false}` hardcoding in `IntakePage.tsx`
  (L235) is the proximate cause; see Fix Plan item 1.

- **[LOW] F-002 — `handleFallbackSubmit` double-processes fallback fields:** `IntakePage.tsx:
  handleFallbackSubmit` (L105–109) calls both `submitAnswer(value)` and
  `updateFieldValue(fieldKey, value)`. `submitAnswer()` already updates `capturedFields`
  on success; the `updateFieldValue` call is redundant on the success path. On the failure
  path (backend rejects the fallback answer too), `submitAnswer()` appends a second
  fallback message, creating a loop. Correct behaviour for a manual fallback value is to
  persist it locally and advance without a backend round-trip.

**Security:**

- JWT bearer token attached to every fetch: **Pass**
- No direct Gemini calls from the browser: **Pass**
- `sessionStorage` (session-scoped) used instead of `localStorage` for partial PHI fields: **Pass**
- React JSX escapes all rendered field values — no XSS risk: **Pass**
- `sessionStorage` writes wrapped in `try/catch` to prevent QuotaExceeded leaking state: **Pass**
- OWASP A01 (Broken Access Control): All intake endpoints require JWT — enforced server-side; **Pass**
- OWASP A03 (Injection): All user text is passed as JSON body strings, not interpolated into SQL or HTML: **Pass**

**Error Handling:**

- `startSession()`, `submitAnswer()`, `confirmIntake()` each have `try/catch`: **Pass**
- `submitAnswer()` catch converts failures to an inline `ManualFieldFallback`: **Pass**
- `confirmIntake()` catch sets `phase = 'error'` — but the summary screen is then
  inaccessible (see F-001): **Gap**
- No retry logic for transient network errors — acceptable for MVP scope.

**Data Access:**

- No direct database calls from frontend. All writes deferred to backend via
  `POST /api/v1/intake/confirm`: **Pass**
- `capturedFields` uses functional state updates (`prev => [...]`) in `submitAnswer` to
  avoid stale closures: **Pass**

**Frontend:**

- **[LOW] F-003 — `data-uxr="UXR-502"` silently dropped by `IntakeProgressBar`:**
  `IntakePage.tsx` passes `data-uxr="UXR-502"` to `IntakeProgressBar` (L221) but
  `IntakeProgressBarProps` does not include a `data-uxr` field and the component does not
  spread extra props onto its root `<div>`. TypeScript permits `data-*` props on JSX
  elements but the attribute never reaches the DOM, so UXR compliance tooling will not
  find it on the progress bar element.
- All `useCallback` dep arrays are correct and verified lint-clean: **Pass**
- Accessibility: `role="progressbar"`, `aria-valuenow/min/max`, `aria-live="polite"` on
  chat scroll area, `role="log"` on message list, `role="alert"` on error banner: **Pass**
- `autoFocus` replaced with `useRef + useEffect` for edit focus (jsx-a11y compliance): **Pass**
- `useId()` used for stable `id`/`htmlFor` pairing in `IntakeSummaryReview`: **Pass**

**Performance:**

- All hook callbacks wrapped in `useCallback` with correct dep arrays: **Pass**
- `submitAnswer` dep array includes `phase` — creates a new reference on each phase
  transition. Since `phase` is in the array for correctness (guard `phase !== 'conversation'`),
  this is intentional and acceptable.
- No `React.memo` on `AIIntakeChat` or sub-components — acceptable given the single
  consumer (`IntakePage`) and message-list rendering pattern.

**Patterns & Standards:**

- CSS Modules pattern consistent with `BaselineDemo.module.css` baseline: **Pass**
- All token-eligible values use semantic CSS custom properties (`var(--...)`): **Pass**
- No prop drilling beyond two levels; `useAIIntake` hook correctly encapsulates API state: **Pass**
- **[INFO] Scope additions beyond Expected Changes table:** `frontend/src/types/intake.ts`
  and `frontend/src/context/AuthContext.tsx` (US_010 stub) were created but not listed in
  the task's Expected Changes. Both are justified (shared types, dependency stub) and
  reviewed as in-scope; noted for sprint tracking.

## Test Review

**Existing Tests:** None for intake components. Workspace baseline is `BaselineDemo.test.tsx`
(smoke render only).

**Missing Tests (must add):**

- [ ] Unit: `IntakeProgressBar` — renders "Question N of M" label; `aria-valuenow` equals
  `Math.round((current/total)*100)` for representative inputs (0/5, 3/5, 5/5)
- [ ] Unit: `IntakeSummaryReview` — renders one `FieldCard` per captured field; Edit button
  enters edit mode; Save calls `onFieldSave` with correct key/value; Escape cancels edit
- [ ] Unit: `AIIntakeChat` — user message appended on Send; Enter key triggers send;
  Shift+Enter does not trigger send; `ManualFieldFallback` rendered when
  `fallbackRequired === true`
- [ ] Unit: `useAIIntake` — `startSession()` on first call → `POST /api/v1/intake/start`;
  resume branch invoked when `GET /api/v1/intake/session/:id` returns 200 with fields
- [ ] Integration: `IntakePage` mounts → `startSession()` called; progress bar shows after
  first question received; mode switch preserves `capturedFields`
- [ ] Negative/Edge: `submitAnswer` fetch throws → fallback message rendered;
  `capturedFields` has `manualRequired: true` for failed field;
  `sessionStorage` value reflects `method: 'AI-Partial'`
- [ ] Negative/Edge: `confirmIntake()` fetch throws → error banner visible; user can
  still navigate back to summary (validates F-001 fix)

## Validation Results

**Commands Executed:**

```text
cd frontend && npm run build   # tsc && vite build
cd frontend && npm run lint    # eslint src --ext .ts,.tsx
```

**Outcomes:**

| Command | Result | Notes |
|---|---|---|
| `npm run build` | Pass | 44 modules; 189.69 kB JS; exit 0 |
| `npm run lint` | Pass | 0 errors, 0 warnings; exit 0 |
| Manual validation strategy (task file) | Not automated | 4 scenarios untested — no test runner executed |

**Manual Validation Gaps:**

| Scenario | Automated? | Status |
|---|---|---|
| Session opens; first question as bot bubble; progress bar "Question 1 of N" | No | Untested |
| After answer submission, progress bar advances | No | Untested |
| Browser refresh → session restored from `sessionStorage` | No | Untested |
| Summary step → all fields shown; inline edit; confirm CTA visible | No | Untested |

## Fix Plan (Prioritized)

1. **[MEDIUM] Fix broken confirmation error-recovery path (F-001)** —
   `frontend/src/pages/IntakePage.tsx` — ETA 0.5 h — Risk: Low

   Add `const [isConfirming, setIsConfirming] = useState(false)` to `IntakePage`.
   Wrap `handleConfirmIntake` body: `setIsConfirming(true)` before `await`, clear in
   `finally`. Change `IntakeSummaryReview` render condition to
   `(phase === 'summary' || isConfirming || phase === 'error') && mode === 'ai'`.
   Pass `isConfirming={isConfirming}` to `IntakeSummaryReview`.

2. **[LOW] Forward `data-uxr` attribute in `IntakeProgressBar` (F-003)** —
   `frontend/src/components/intake/IntakeProgressBar.tsx` — ETA 0.25 h — Risk: Low

   Extend `IntakeProgressBarProps` with `'data-uxr'?: string`. Spread
   `data-uxr={props['data-uxr']}` (or `...rest`) onto the root `<div className={styles.wrap}>`.

3. **[LOW] Decouple fallback submission from backend round-trip (F-002)** —
   `frontend/src/pages/IntakePage.tsx` — ETA 0.5 h — Risk: Low

   Replace `handleFallbackSubmit` to call only `updateFieldValue(fieldKey, value)` and
   then advance the conversation state locally (e.g. emit a synthetic next-question
   request or move to summary if all fields are filled). Remove the `submitAnswer(value)`
   call inside `handleFallbackSubmit`.

4. **[INFO] Add unit + integration tests for intake components** —
   `frontend/src/components/intake/*.test.tsx`, `frontend/src/hooks/useAIIntake.test.ts` —
   ETA 3 h — Risk: Low (no production code changes)

   Implement 7 test cases listed in Test Review section above. Use Vitest + React Testing
   Library (already in `package.json`). Mock `fetch` with `vi.stubGlobal`.

## Appendix

**Search Evidence:**

| Pattern searched | Files scanned | Key findings |
|---|---|---|
| `isConfirming` | `IntakePage.tsx` | Hardcoded `false` at L235 |
| `data-uxr` | `IntakePage.tsx`, `IntakeProgressBar.tsx` | Passed at L221; not forwarded in component |
| `handleFallbackSubmit` | `IntakePage.tsx` | Calls both `submitAnswer` + `updateFieldValue` at L107–108 |
| `saveToStorage`, `loadFromStorage` | `useAIIntake.ts` | Correct `try/catch` guard; keyed by `appointmentId` |
| `buildHeaders` | `useAIIntake.ts` | `useCallback([token])`; dep arrays correct |
| `aria-`, `role=` | `AIIntakeChat.tsx`, `IntakeProgressBar.tsx` | `aria-live`, `role="log"`, `role="progressbar"`, `aria-valuenow/min/max` all present |

**Context7 References:** Not required — React 18 hooks and ARIA patterns are within training data;
no framework API ambiguity encountered.
