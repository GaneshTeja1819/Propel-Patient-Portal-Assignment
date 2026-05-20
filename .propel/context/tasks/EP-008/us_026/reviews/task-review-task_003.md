# Implementation Analysis — task_003_frontend-extraction-status

**Task file:** `.propel/context/tasks/EP-008/us_026/task_003_frontend-extraction-status.md`  
**User story:** US_026  
**Date:** 2026-05-19  
**Analyst:** GitHub Copilot (PropelIQ analyze-implementation workflow)

---

## Verdict

**Status:** Conditional Pass

The implementation delivers all three production files required by TASK_003 and satisfies AC-004 with both specified edge cases. TypeScript compilation and the Vite production build pass cleanly (0 errors). The hook architecture is sound: stable `useCallback` dependency chains, a `useEffect` cleanup that correctly clears both the polling interval and the 5-minute timeout, ARIA roles correctly assigned (`role="status"` for non-urgent states, `role="alert"` for failure states), and all fetch calls use `credentials: 'include'`. The condition blocking a full Pass is the absence of any unit or integration tests — the task's Implementation Validation Strategy specifies three test suites, none of which were created. Until those tests exist, a regression in retry logic, state transitions, or the UXR-603 banner rendering cannot be caught automatically.

---

## Traceability Matrix

| Requirement / AC | Evidence (file : line) | Result |
|-----------------|------------------------|--------|
| AC-004: "Extraction failed — contact staff or retry" banner | `ExtractionStatusBanner.tsx:57` — message string | **Pass** |
| AC-004: Retry button present on failed state | `ExtractionStatusBanner.tsx:75-87` — `<button>↺ Retry extraction</button>` | **Pass** |
| AC-004: Amber banner (UXR-603) | `ExtractionStatusBanner.tsx:38-44` — `--color-warning-bg`, `--color-warning`, `--color-warning-text` | **Pass** |
| AC-004: role="alert" on failed state | `ExtractionStatusBanner.tsx:65` — `role={isAlert ? 'alert' : 'status'}` | **Pass** |
| AC-004: Clicking Retry calls POST /retry-extraction | `useExtractionStatus.ts:88-92` — fetch POST with credentials | **Pass** |
| AC-004: On retry success, polling resumes | `useExtractionStatus.ts:95` — `startPolling(documentId)` in .then() | **Pass** |
| Edge case — timeout after 5 min | `useExtractionStatus.ts:75-78` — `window.setTimeout(…, 300_000)` → `setStatus('timeout')` | **Pass** |
| Edge case — timeout message "check back later" | `ExtractionStatusBanner.tsx:61` — message string | **Pass** |
| Edge case — Retry button on timeout state | `ExtractionStatusBanner.tsx:31` — `showRetry = isAlert` (timeout is alert) | **Pass** |
| Edge case — retry 4xx/5xx → "Retry failed — contact support" | `useExtractionStatus.ts:96-99` — `setFailureNote(...)` on !res.ok | **Pass** |
| Edge case — retry network error → same message | `useExtractionStatus.ts:100-102` — `.catch(() => setFailureNote(...))` | **Pass** |
| Edge case — no infinite retry loop | `retry()` sets failureNote and returns; no recursive call | **Pass** |
| Edge case — 409 Conflict on retry → no-op | `useExtractionStatus.ts:93` — `if (res.status === 409) return` | **Pass** |
| Processing state: spinner + "AI extraction in progress…" | `ExtractionStatusBanner.tsx:50,55` — ⏳ icon + message | **Pass** |
| Completed state: green "Extraction complete" | `ExtractionStatusBanner.tsx:33-37,56` — `--color-success-bg`, ✓ icon | **Pass** |
| failureNote rendered below banner | `ExtractionStatusBanner.tsx:91-100` — `<p>{failureNote}</p>` | **Pass** |
| credentials: 'include' on polling fetch | `useExtractionStatus.ts:56` — `{ credentials: 'include' }` | **Pass** |
| credentials: 'include' on retry fetch | `useExtractionStatus.ts:89` — `{ credentials: 'include' }` | **Pass** |
| Polling stops on completed | `useExtractionStatus.ts:61-63` — `stopPolling()` after `setStatus('completed')` | **Pass** |
| Polling stops on failed | `useExtractionStatus.ts:64-66` — `stopPolling()` after `setStatus('failed')` | **Pass** |
| useEffect cleanup clears interval + timeout | `useExtractionStatus.ts:113` — `return stopPolling` | **Pass** |
| Banner rendered only after upload success | `DocumentUploadPage.tsx:224` — `{documentId !== null && <ExtractionStatusBanner .../>}` | **Pass** |
| DocumentUploadPage uses documentId from useDocumentUpload | `DocumentUploadPage.tsx:19,22` — direct prop pass (no shadow state) | **Pass** |
| data-uxr="UXR-603" attribute on banner | `ExtractionStatusBanner.tsx:66` — `data-uxr="UXR-603"` | **Pass** |
| Unit tests — useExtractionStatus | No test file found | **Gap** |
| Unit tests — ExtractionStatusBanner | No test file found | **Gap** |
| Integration test — polling mock | No test file found | **Gap** |

---

## Logical & Design Findings

### Business Logic
The polling lifecycle is implemented correctly. `startPolling(id)` accepts the document ID as a parameter rather than closing over `documentId` state — this keeps the `useCallback` dependency list minimal (`[stopPolling]` only) and prevents stale-closure state bugs when `documentId` changes. The `useEffect` correctly returns `stopPolling` as its cleanup function, ensuring the interval and timeout are cleared on both unmount and `documentId` change.

The `retry()` function calls `setFailureNote(null)` before the fetch, immediately clearing any prior retry-error message. On 409 Conflict, it returns early without restarting polling (correct — already processing). On any other non-OK response, it sets the failure note and returns without entering any loop.

One logical concern: when `reset()` is called from `DocumentUploadPage` (before a new upload), `documentId` in `useDocumentUpload` transitions to `null`. This triggers `useExtractionStatus`'s `useEffect`, which calls `stopPolling()` and `setStatus('idle')`. The `ExtractionStatusBanner` then returns `null` (no render). This is the correct cleanup path. ✅

**Minor**: The task specification says `status === 'success'` in `DocumentUploadPage` shows "Document uploaded successfully. AI extraction queued." When extraction later completes, both this banner AND the "Extraction complete" ExtractionStatusBanner will be visible simultaneously. The "AI extraction queued" text becomes redundant/misleading once the extraction is complete. This is a UX duplication gap.

### Security
- `credentials: 'include'` applied on both `GET /extraction-status` and `POST /retry-extraction` fetch calls — `__Host-access` JWT cookie forwarded correctly through the Vite dev proxy. ✅
- `documentId` is interpolated into the URL path (`/api/v1/documents/${id}/extraction-status`). The value originates from `JSON.parse(xhr.responseText)` in `useDocumentUpload` (trusted server response on HTTP 201). No client-side UUID format validation. Path traversal risk is mitigated by: (1) the server controlling the value on 201 response; (2) any `/` or `..` in the path would hit an unmatched route on the backend. **Low risk** — no action required, but a `UUID_REGEX.test(id)` guard in `startPolling` would be defensive best practice.
- No secrets, API keys, or credentials hardcoded. ✅
- No `dangerouslySetInnerHTML` usage — all user-visible text is static strings from the component. ✅

### Error Handling
- Transient server errors during polling (`!res.ok`): silently ignored — polling continues. Correct for 5xx transient errors; slightly risky for persistent 4xx (e.g., 404 Not Found if document deleted). After 5 min the timeout produces "check back later" which is an acceptable UX trade-off.
- Network error during polling: `.catch()` swallows the error — polling continues. ✅
- JSON parse error (server returns non-JSON HTML error page): the Promise chain will catch the parse error in `.catch()` — polling continues. ✅
- **Missing**: `retry()` fetch has no `AbortController`. If the component unmounts while a retry fetch is in-flight, the `.then()` callback will call `startPolling()` or `setFailureNote()` on the unmounted component, generating a React stale-state warning. See GAP-2 below.

### Frontend
- State management: minimal — two `useState` calls (`status`, `failureNote`) plus two `useRef` calls for handles. No unnecessary state derived from other state.
- API contract: expects `{ extractionStatus: string }` JSON from `GET /extraction-status`. Uses `.toLowerCase()` comparison for defensive case-insensitive matching. ✅
- **Accessibility gap**: `processing` state uses `role="status"` (correct) but lacks `aria-busy="true"`. Screen reader users would hear "AI extraction in progress…" announced once but have no mechanism to know it's still actively loading. Adding `aria-busy="true"` on the processing state `<div>` would be more semantically complete.
- **Note**: `processing` state renders with amber colors (same as `failed`/`timeout`). The wireframe specifies amber for `badge-processing`; this follows the wireframe. However, amber conventionally implies a warning. Users may interpret "processing" as a warning state. A neutral `--color-brand-primary` / `--color-brand-primary-light` color for the `processing` state would be semantically clearer.

### Performance
- `setInterval` at 5 s is appropriate for extraction status polling. No performance concern.
- No memory leaks: `clearInterval` + `clearTimeout` in every termination path (completed, failed, timeout, unmount, documentId change). ✅
- First poll fires immediately on mount (`poll()` before `setInterval`) — good UX for fast extractions. ✅

### Patterns & Standards
- `stopPolling` defined with `useCallback([])` — stable reference, never recreates. ✅
- `startPolling` defined with `useCallback([stopPolling])` — only changes if `stopPolling` changes (never). ✅
- `retry` defined with `useCallback([documentId, startPolling])` — updates when `documentId` changes. ✅
- `useEffect` deps `[documentId, startPolling, stopPolling]` — correct; triggers re-run only when `documentId` changes. ✅
- Pattern follows `useDocumentUpload.ts` hook conventions (single-concern, `useCallback`-memoized handlers, clean return type). ✅

---

## Test Review

### Existing Tests
- `frontend/src/components/BaselineDemo.test.tsx` — 4 tests for the baseline demo page. Not related to this task.
- **No tests exist** for `useExtractionStatus`, `ExtractionStatusBanner`, or the updated `DocumentUploadPage`.

### Missing Tests (must add)

- [ ] **Unit — `useExtractionStatus`** (`src/hooks/useExtractionStatus.test.ts`):
  - Mock `global.fetch` with `vi.fn()` returning `{ extractionStatus: 'Processing' }` / `'Completed'` / `'Failed'`
  - Assert polling starts when `documentId` changes from null to non-null
  - Assert `setInterval` called with 5 000 ms interval
  - Assert polling stops (`clearInterval` called) on 'Completed' response
  - Assert polling stops on 'Failed' response; `status === 'failed'`
  - Assert `clearInterval` called on unmount (cleanup)
  - Assert `status === 'timeout'` after advancing fake timers by 300 000 ms without terminal state
  - Assert `retry()` calls `POST /retry-extraction`
  - Assert `retry()` with 409 response → status remains `'failed'`; no `startPolling` call
  - Assert `retry()` with 500 response → `failureNote === 'Retry failed — contact support'`
  - Assert `retry()` on network error → `failureNote === 'Retry failed — contact support'`

- [ ] **Unit — `ExtractionStatusBanner`** (`src/components/documents/ExtractionStatusBanner.test.tsx`):
  - `status='idle'` → renders null (no DOM element)
  - `status='processing'` → `role="status"`, text "AI extraction in progress…", ⏳ present, no Retry button
  - `status='completed'` → `role="status"`, text "Extraction complete", ✓ present, no Retry button
  - `status='failed'` → `role="alert"`, text "Extraction failed — contact staff or retry", Retry button present
  - `status='timeout'` → `role="alert"`, text "check back later", Retry button present
  - `failureNote='Retry failed — contact support'` → additional `<p>` visible
  - Retry button click calls `onRetry`
  - `data-uxr="UXR-603"` attribute present on container

- [ ] **Integration — `DocumentUploadPage`** (mocked server responses):
  - After upload success (`documentId` set), `GET /extraction-status` is polled
  - When mock returns `{ extractionStatus: 'Failed' }` → ExtractionStatusBanner `failed` state renders
  - Retry button click triggers `POST /retry-extraction`
  - After successful retry, banner transitions back to `processing`

- [ ] **Negative/Edge**:
  - `useExtractionStatus(null)` → no fetch calls, `status === 'idle'`
  - Rapid `documentId` changes → only the latest `documentId` is polled (no stale intervals)
  - Component unmounts during in-flight polling → no React state update after unmount

---

## Validation Results

| Command | Outcome |
|---------|---------|
| `npx tsc --noEmit` (frontend) | ✅ Exit 0 — 0 TypeScript errors |
| `npx vite build` (frontend) | ✅ Exit 0 — 50 modules, 0 errors, built in 841ms |
| `npm test` (vitest) | ⚠ Not run — no tests for new files; existing BaselineDemo tests pass |

---

## Gap Register

| ID | Severity | Description | File | Effort |
|----|----------|-------------|------|--------|
| GAP-1 | **HIGH** | No unit or integration tests for `useExtractionStatus`, `ExtractionStatusBanner`, or updated `DocumentUploadPage`. Task's Validation Strategy items left unchecked. | `src/hooks/useExtractionStatus.test.ts` (missing), `src/components/documents/ExtractionStatusBanner.test.tsx` (missing) | 4h |
| GAP-2 | **MEDIUM** | `retry()` fetch lacks `AbortController`. If component unmounts mid-fetch, `.then()` callbacks (`startPolling`, `setFailureNote`) execute on unmounted component — React stale-state warning. Same pattern as registry F014. | `useExtractionStatus.ts:88-103` | 30 min |
| GAP-3 | LOW | `processing` state uses amber colors (same hue as failed/timeout). Amber conventionally implies warning. Wireframe prescribes amber; implementation follows wireframe. Neutral/brand color for `processing` would improve semantic clarity. | `ExtractionStatusBanner.tsx:38-44` | 15 min |
| GAP-4 | LOW | `processing` state missing `aria-busy="true"`. Screen readers announce initial text but cannot indicate ongoing async operation without `aria-busy`. | `ExtractionStatusBanner.tsx:64` | 5 min |
| GAP-5 | LOW | When extraction completes, both the upload success banner ("AI extraction queued.") and the extraction complete banner ("Extraction complete") are simultaneously visible. The sub-text "AI extraction queued." becomes misleading. | `DocumentUploadPage.tsx:186-196` | 15 min |
| GAP-6 | LOW | `documentId` interpolated into fetch URL without UUID format validation on client. Low risk — value originates from trusted server response. | `useExtractionStatus.ts:56,89` | 15 min |

---

## Risk Register

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Retry logic regression (state machine broken by refactor) | Medium | High | Implement GAP-1 tests immediately |
| React stale-state warning surfaces in Sentry/logs | Low | Medium | AbortController in retry() (GAP-2) |
| Interval not cleared on unmount in edge case | Low | Medium | Covered by existing `return stopPolling` cleanup; low residual risk |
| Backend returns documentId with path traversal characters | Very Low | Low | UUID validation guard in startPolling |
| Amber processing color causes user confusion ("is something wrong?") | Low | Low | UX copy clarification or color change (GAP-3) |

---

## Fix Plan (Prioritized)

1. **[HIGH] Create `useExtractionStatus.test.ts`** — `src/hooks/useExtractionStatus.test.ts`  
   Use `vi.useFakeTimers()` + `vi.fn()` for fetch. Cover all 11 test cases listed in Test Review. Removes regression risk from HIPAA-adjacent polling logic.  
   **ETA:** 3 h · **Risk:** High

2. **[HIGH] Create `ExtractionStatusBanner.test.tsx`** — `src/components/documents/ExtractionStatusBanner.test.tsx`  
   Use `@testing-library/react render()`. Assert role, text content, button presence for all 5 render states (idle→null, processing, completed, failed, timeout) plus failureNote.  
   **ETA:** 1 h · **Risk:** Medium

3. **[MEDIUM] Add `AbortController` to `retry()` fetch** — `useExtractionStatus.ts:88`  
   Store controller ref; abort in `useEffect` cleanup. Prevents stale-state updates on unmount mid-retry.  
   **ETA:** 30 min · **Risk:** Medium

4. **[LOW] Add `aria-busy="true"` on processing state** — `ExtractionStatusBanner.tsx:64`  
   Add `aria-busy={status === 'processing' ? 'true' : undefined}` on the banner container.  
   **ETA:** 5 min · **Risk:** Low

5. **[LOW] Suppress "AI extraction queued." sub-text post-extraction** — `DocumentUploadPage.tsx:193`  
   Conditionally hide the sub-text when `extractionStatus !== 'idle' && extractionStatus !== 'processing'`.  
   **ETA:** 15 min · **Risk:** Low

---

## Appendix

### Context7 References
- Not invoked (React 18 hook patterns, ARIA live region semantics, and Vitest mock patterns are well-known stable APIs; no documentation fetch required for this analysis depth)

### Search Evidence

| Pattern | Matches | Key Paths |
|---------|---------|-----------|
| `ExtractionStatusBanner\|useExtractionStatus` in `DocumentUploadPage.tsx` | 14 | lines 14, 16, 22, 224–228 |
| `credentials: 'include'` in `useExtractionStatus.ts` | 2 | lines 56, 89 |
| `stopPolling\|clearInterval` in `useExtractionStatus.ts` | 7 | lines 37–46 (stopPolling body), 63, 66, 75 |
| `role.*alert\|role.*status` in `ExtractionStatusBanner.tsx` | 1 | line 65 |
| `*.test.*` in `frontend/src/**` | 1 | `BaselineDemo.test.tsx` only |
| `vitest\|@testing-library` in `package.json` | 5 | lines 11, 22–24, 39 |
