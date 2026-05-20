# Task - TASK_001

## Requirement Reference
- **User Story:** us_025
- **Story Location:** .propel/context/tasks/EP-008/us_025/us_025.md
- **Acceptance Criteria:**
  - AC-001: Valid PDF + document type → uploaded; ClinicalDocument record created; confirmation shown
  - AC-002: Non-PDF file → rejected before upload; "Only PDF files are accepted" inline error
  - AC-003: File > 10 MB → rejected client-side; "File exceeds maximum size of 10 MB" inline error; no server request made
- **Edge Cases:**
  - Supabase Storage limit reached → HTTP error from backend → show "Storage limit reached — contact support"; no partial record
  - Password-protected PDF → uploaded successfully; extraction failure handled in US_026

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
| **UXR Requirements** | N/A |
| **Design Tokens** | `--color-success`, `--color-error` from variables.css |

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
| Frontend | React (SPA) | 18.x | TR-001 — Document upload form on SCR-010 |

---

## Task Overview
Build SCR-010, the clinical document upload screen. A file input accepts only PDFs (validated by MIME type and extension). A document type selector (Historical / Post-visit) is required. Files over 10 MB are rejected client-side before any HTTP request is made. Valid uploads are sent to `POST /api/v1/documents/upload` as multipart/form-data. Upload progress is shown via a `<progress>` element using the `XMLHttpRequest.upload.onprogress` event (or Axios `onUploadProgress`). On success, a green "Document uploaded successfully" confirmation banner is shown. On storage error, the amber error banner is shown.

## Dependent Tasks
- `task_001_frontend-login.md` (US_010) — `AuthContext` for authenticated upload request

## Impacted Components
- `frontend/src/pages/DocumentUploadPage.tsx` — new SCR-010 page
- `frontend/src/components/documents/DocumentUploadForm.tsx` — new upload form with validation
- `frontend/src/hooks/useDocumentUpload.ts` — new hook: POST multipart + progress tracking
- `frontend/src/App.tsx` — add `/documents/upload` route

## Implementation Plan
1. Create `DocumentUploadForm.tsx`:
   - `<input type="file" accept=".pdf,application/pdf" />` — browsers filter non-PDF in file picker; additionally validate `file.type === 'application/pdf'` and `file.name.endsWith('.pdf')` on change (AC-002)
   - File size check on change: if `file.size > 10 * 1024 * 1024` → show inline error; clear input; no upload state set (AC-003)
   - Document type `<select>` with options "Historical" / "Post-visit"; required
   - "Upload" CTA disabled until file and document type are both selected
2. Create `useDocumentUpload` hook:
   - Builds `FormData` with `file` and `documentType`
   - Uses Axios POST with `onUploadProgress` callback updating a `0–100` progress state
   - On HTTP 201 → set `status = 'success'`
   - On HTTP 507 (storage limit) → set `status = 'storage-error'`
   - On other errors → set `status = 'error'`
3. Create `DocumentUploadPage.tsx`: renders `DocumentUploadForm`; shows `<progress>` bar during upload; renders success/error banners based on hook status
4. Add `/documents/upload` route

## Current Project State
```
frontend/
  src/
    context/AuthContext.tsx
    styles/variables.css
    App.tsx
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/pages/DocumentUploadPage.tsx | SCR-010 document upload page |
| CREATE | frontend/src/components/documents/DocumentUploadForm.tsx | File input with PDF + size validation |
| CREATE | frontend/src/hooks/useDocumentUpload.ts | Multipart POST with progress tracking |
| MODIFY | frontend/src/App.tsx | Add /documents/upload route |

## External References
- [wireframe-SCR-010-document-upload.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-010-document-upload.html)
- [WCAG 2.2 SC 3.3.1 Error Identification](https://www.w3.org/WAI/WCAG22/Understanding/error-identification.html)

## Build Commands
- Refer to [frontend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Select a .docx file → inline "Only PDF files are accepted" error; no upload initiated
- [ ] Select a PDF > 10 MB → inline size error; no server request (verify via network tab)
- [ ] Select valid PDF + document type → progress bar appears; on completion success banner shown
- [ ] Storage error from backend → "Storage limit reached — contact support" banner

## Implementation Checklist
- [x] MIME type + extension PDF validation on file change; inline error; no upload (AC-002)
- [x] Client-side size check (> 10 MB); inline error; no HTTP request (AC-003)
- [x] Document type selector required; "Upload" CTA disabled until both fields set (AC-001)
- [x] Upload progress bar via Axios `onUploadProgress` (AC-001 UX)
- [x] Success banner on HTTP 201; storage error banner on HTTP 507 (AC-001, edge case)
