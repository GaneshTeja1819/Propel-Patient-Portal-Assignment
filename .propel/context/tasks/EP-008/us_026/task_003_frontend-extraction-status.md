# Task - TASK_003

## Requirement Reference
- **User Story:** us_026
- **Story Location:** .propel/context/tasks/EP-008/us_026/us_026.md
- **Acceptance Criteria:**
  - AC-004: When Gemini extraction fails after all retries, surface "Extraction failed — contact staff or retry" banner with a retry button on the document upload or profile screen (AIR-007, UC-021 Extension 3a, UXR-603)
- **Edge Cases:**
  - Extraction status remains "Processing" indefinitely (job stuck) → polling times out after 5 min; "Extraction is taking longer than expected — check back later" message shown; retry button available
  - Retry request fails (backend returns 4xx/5xx) → "Retry failed — contact support" message; no infinite retry loop

---

## Design References [CONDITIONAL: UI Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-010-document-upload.html |
| **Screen Spec** | SCR-010 |
| **UXR Requirements** | UXR-603 |
| **Design Tokens** | `--color-error`, `--color-warning`, `--color-success` from variables.css |

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
| Frontend | React (SPA) | 18.x | TR-001 — Extraction status banner and retry CTA on SCR-010 |

---

## Task Overview
Add an extraction status indicator to the document upload page (SCR-010). After a successful PDF upload, the `DocumentUploadPage` polls `GET /api/v1/documents/{id}/extraction-status` at a 5-second interval. While `extractionStatus = "Processing"`, a spinner with "AI extraction in progress…" is shown. When `extractionStatus = "Completed"`, a green "Extraction complete" banner replaces the spinner. When `extractionStatus = "Failed"`, an amber UXR-603 banner is rendered: "Extraction failed — contact staff or retry" with a "Retry" button. Clicking Retry calls `POST /api/v1/documents/{id}/retry-extraction`; on success, polling resumes. Maximum polling duration: 5 minutes before timing out gracefully.

## Dependent Tasks
- `task_002_backend-extraction-retry-downstream.md` (US_026) — `GET /api/v1/documents/{id}/extraction-status` and `POST /api/v1/documents/{id}/retry-extraction` endpoints must exist
- `task_001_frontend-document-upload.md` (US_025) — `DocumentUploadPage` must exist to host the status indicator

## Impacted Components
- `frontend/src/hooks/useExtractionStatus.ts` — new polling hook
- `frontend/src/components/documents/ExtractionStatusBanner.tsx` — new UXR-603 status banner component
- `frontend/src/pages/DocumentUploadPage.tsx` — integrate `useExtractionStatus` and render `ExtractionStatusBanner`

## Implementation Plan
1. Create `useExtractionStatus(documentId: string | null)` hook:
   - When `documentId` is null, does nothing (upload not yet completed)
   - Uses a `useRef<number | null>(null)` to store the interval handle; starts polling via `setInterval` at 5 000 ms to call `GET /api/v1/documents/{documentId}/extraction-status` with `credentials: 'include'`
   - Returns `{ status: 'idle' | 'processing' | 'completed' | 'failed' | 'timeout', failureNote: string | null, retry: () => void }`
   - `useEffect` cleanup function (`return () => clearInterval(intervalRef.current)`) clears the interval on component unmount and whenever `documentId` changes — prevents memory leaks
   - Clears interval internally when `status` reaches `'completed'` or `'failed'`
   - After 300 000 ms (5 min) without terminal state → set `status = 'timeout'`; clear interval
   - `retry()`: calls `POST /api/v1/documents/{documentId}/retry-extraction` with `credentials: 'include'`; on 200 → resets status to `'processing'`; restarts polling; on 409 Conflict → no-op (already processing); on other error → sets local `retryError` state (AC-004 edge case)
2. Create `ExtractionStatusBanner` component:
   - `status === 'processing'` → spinner + "AI extraction in progress…" (neutral)
   - `status === 'completed'` → green check icon + "Extraction complete" (`--color-success`)
   - `status === 'failed'` → amber warning icon + "Extraction failed — contact staff or retry" + "Retry" `<button>` (`--color-error`); UXR-603
   - `status === 'timeout'` → info icon + "Extraction is taking longer than expected — check back later" + "Retry" button
   - `retryError` truthy → inline error text "Retry failed — contact support" beneath the banner
   - All states use ARIA `role="status"` or `role="alert"` appropriately for screen readers
3. In `DocumentUploadPage`:
   - Add `const [uploadedDocId, setUploadedDocId] = useState<string | null>(null)`
   - On upload success, extract `documentId` from the API response; call `setUploadedDocId(documentId)`
   - Render `<ExtractionStatusBanner status={...} failureNote={...} onRetry={retry} />` below the upload form once `uploadedDocId` is non-null
4. Wire `credentials: 'include'` on all fetch calls so the `__Host-access` JWT cookie is forwarded through the Vite proxy (AC-004)

## Current Project State
- `frontend/src/pages/DocumentUploadPage.tsx` — created by task_001_frontend-document-upload (US_025)
- `frontend/src/hooks/useDocumentUpload.ts` — created by task_001_frontend-document-upload (US_025)
- Backend endpoints `GET /api/v1/documents/{id}/extraction-status` and `POST /api/v1/documents/{id}/retry-extraction` — created by task_002_backend-extraction-retry-downstream (US_026)

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | `frontend/src/hooks/useExtractionStatus.ts` | Polling hook — 5 s interval; handles processing/completed/failed/timeout states; exposes retry() |
| CREATE | `frontend/src/components/documents/ExtractionStatusBanner.tsx` | UXR-603 status banner — four visual states; retry CTA; ARIA roles |
| MODIFY | `frontend/src/pages/DocumentUploadPage.tsx` | Add `uploadedDocId` state; integrate `useExtractionStatus`; render `ExtractionStatusBanner` after upload |

## External References
- [React useState / useEffect docs](https://react.dev/reference/react)
- [ARIA live regions (role="status", role="alert")](https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/ARIA_Live_Regions)

## Build Commands
- Refer to [Frontend build commands](.propel/build/frontend.md)

## Implementation Validation Strategy
- [ ] `useExtractionStatus` unit tests: mock `fetch`; assert polling starts on non-null documentId; stops on completed/failed; calls retry endpoint on `retry()`
- [ ] `ExtractionStatusBanner` unit tests: render each status variant; assert correct text and ARIA roles
- [ ] Integration test: mock `GET /extraction-status` to return `failed`; assert retry button renders and calls retry endpoint

## Implementation Checklist
- [x] Create `useExtractionStatus` hook with 5 s polling, timeout after 5 min, and `retry()` function (AC-004)
- [x] Create `ExtractionStatusBanner` with processing/completed/failed/timeout states and UXR-603 amber banner + retry CTA (AC-004, UXR-603)
- [x] Integrate `ExtractionStatusBanner` into `DocumentUploadPage` triggered by upload success (AC-004)
- [x] Apply `--color-error` token to failed state and `--color-success` to completed state (UXR-603 visual standard)
- [x] Add `role="alert"` on failed/timeout banners and `role="status"` on processing/completed for accessibility
- [x] Verify `credentials: 'include'` on all polling and retry fetch calls (auth cookie forwarded through Vite proxy)
