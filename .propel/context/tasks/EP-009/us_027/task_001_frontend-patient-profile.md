# Task - TASK_001

## Requirement Reference
- **User Story:** us_027
- **Story Location:** .propel/context/tasks/EP-009/us_027/us_027.md
- **Acceptance Criteria:**
  - AC-001: Staff views SCR-013 with vitals, medications, diagnoses, visit history; data from all ExtractedClinicalData records; empty sections show "No data available"
  - AC-002: Patient views SCR-009 in read-only mode; "AI-extracted" labels on AI fields; no Staff-only controls visible
  - AC-004: No documents → "No clinical data available" + upload CTA linking to SCR-010
  - AC-005: API responds ≤ 500 ms P95; UI fully rendered within 2 s
- **Edge Cases:**
  - De-duplication pipeline not yet completed → profile renders with raw entries; "Profile consolidation in progress" banner shown
  - > 50 entries per section → pagination at 20 per page; ≤ 20 rows in DOM at once
  - Staff and Patient access profile concurrently → each sees their respective view; no locking

---

## Design References [CONDITIONAL: UI Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-009-patient-profile.html |
| **Screen Spec** | SCR-009, SCR-013 |
| **UXR Requirements** | UXR-106, UXR-402, UXR-403 |
| **Design Tokens** | `--color-ai-label`, `--color-primary` from variables.css |

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
| Frontend | React (SPA) | 18.x | TR-001 — patient profile on SCR-009 (patient) + SCR-013 (staff) |

---

## Task Overview
Build two profile views sharing the same `ClinicalSections` component: SCR-009 (Patient read-only at `/profile`) and SCR-013 (Staff view at `/staff/patients/:id`). The Patient view shows an "AI-extracted" badge on AI-sourced fields and hides conflict resolution controls. The Staff view shows all sections plus a Conflicts subsection (populated in US_028). Both views handle empty data gracefully with section-level "No data available" and a page-level upload CTA when no documents exist. Sections with > 20 entries use client-side pagination (previous/next). A "Profile consolidation in progress" amber banner appears when `deduplicationStatus = "Processing"`.

## Dependent Tasks
- `task_001_frontend-login.md` (US_010) — `AuthContext` for role-based view switching
- `task_001_frontend-document-upload.md` (US_025) — upload CTA must link to existing `/documents/upload` route

## Impacted Components
- `frontend/src/pages/PatientProfilePage.tsx` — new SCR-009 patient-facing profile page
- `frontend/src/pages/StaffPatientProfilePage.tsx` — new SCR-013 staff patient profile page
- `frontend/src/components/profile/ClinicalSections.tsx` — new shared clinical sections component
- `frontend/src/components/profile/ClinicalSection.tsx` — new single section with pagination
- `frontend/src/hooks/usePatientProfile.ts` — new hook: GET profile data by patientId

## Implementation Plan
1. Create `usePatientProfile(patientId)` hook: calls `GET /api/v1/profile/{patientId}`; returns `{ vitals, medications, diagnoses, visitHistory, deduplicationStatus, hasDocuments }`
2. Create `ClinicalSection.tsx`: renders a section header, a list of items, "No data available" empty state if empty; if `items.length > 20` → paginated view with page state; renders ≤ 20 DOM rows at a time
3. Create `ClinicalSections.tsx`: accepts `{ data, mode: 'patient' | 'staff' }`; renders VitalsSection, MedicationsSection, DiagnosesSection, VisitHistorySection (each a `ClinicalSection`); in `patient` mode — append `--color-ai-label` "AI-extracted" badge on AI-sourced fields; in `staff` mode — render `<ConflictsSection>` placeholder slot (implemented in US_028)
4. Create `PatientProfilePage.tsx` at `/profile`: calls `usePatientProfile(currentUser.id)`; if `!hasDocuments` → render no-data state with "Upload documents" CTA to `/documents/upload`; if `deduplicationStatus === "Processing"` → render amber "Profile consolidation in progress" banner; renders `<ClinicalSections mode="patient" />`
5. Create `StaffPatientProfilePage.tsx` at `/staff/patients/:id`: calls `usePatientProfile(params.id)`; same empty-state / dedup-banner logic; renders `<ClinicalSections mode="staff" />`; patient-role redirect guard (HTTP 403 handled in hook)
6. Add CSS token `--color-ai-label` (blue-tint); add to `variables.css`

## Current Project State
```
frontend/
  src/
    context/AuthContext.tsx
    pages/DocumentUploadPage.tsx  (from US_025)
    styles/variables.css
    App.tsx
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/pages/PatientProfilePage.tsx | SCR-009 read-only patient profile |
| CREATE | frontend/src/pages/StaffPatientProfilePage.tsx | SCR-013 staff patient profile |
| CREATE | frontend/src/components/profile/ClinicalSections.tsx | Shared clinical sections container |
| CREATE | frontend/src/components/profile/ClinicalSection.tsx | Section with pagination and empty state |
| CREATE | frontend/src/hooks/usePatientProfile.ts | GET /api/v1/profile hook |
| MODIFY | frontend/src/styles/variables.css | Add --color-ai-label token |
| MODIFY | frontend/src/App.tsx | Add /profile and /staff/patients/:id routes |

## External References
- [wireframe-SCR-009-patient-profile.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-009-patient-profile.html)
- [WCAG 2.2 SC 1.3.1 Info and Relationships](https://www.w3.org/WAI/WCAG22/Understanding/info-and-relationships.html)

## Build Commands
- Refer to [frontend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Patient views /profile: sections rendered; AI-extracted fields show badge; no Staff controls visible
- [ ] No documents → "No clinical data available" + upload CTA shown
- [ ] deduplicationStatus = "Processing" → amber banner visible in both views
- [ ] Section with > 20 items → pagination controls; only 20 DOM rows rendered

## Implementation Checklist
- [ ] Build `ClinicalSection` with pagination at 20 items; "No data available" empty state (AC-001, AC-002, AC-005)
- [ ] Build `ClinicalSections` with patient/staff mode toggle; AI-extracted badge in patient mode (AC-002, UXR-403)
- [ ] Patient profile page: no-document CTA + dedup banner (AC-004, edge case)
- [ ] Staff profile page: dedup banner; Conflicts section slot for US_028 (AC-001)
- [ ] `usePatientProfile` hook with React Query; profile data fetched on mount (AC-001, AC-005)
- [ ] Add `--color-ai-label` token (AC-002, UXR-403)
