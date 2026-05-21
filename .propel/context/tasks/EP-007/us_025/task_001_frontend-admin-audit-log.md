# Task - TASK_001

## Requirement Reference
- **User Story:** us_025
- **Story Location:** .propel/context/tasks/EP-007/us_025/us_025.md
- **Acceptance Criteria:**
  - AC-001: Paginated audit log table (15 rows/page) with stat strip; sorted newest-first; columns: timestamp, user, role, action badge, resource, IP, status
  - AC-002: Filter bar — date from/to, action category, role, status, free-text search; Reset clears all filters
  - AC-003: Inline row expand showing event ID, correlation ID, session ID, ISO 8601 timestamp, actor, payload JSON, immutable-record notice; single expand at a time; Escape closes
  - AC-004: 🔒 PHI indicator on PHI-access events; HIPAA notice banner always visible at top
  - AC-005: "Export CSV" triggers download; toast confirmation shown; export is Admin-only
  - AC-006: Zero edit/delete/modify controls on the UI; read-only view
  - AC-007: Non-Admin users redirected to their dashboard on access attempt
  - AC-008: Viewing the screen itself is logged (backend concern visible to frontend: no state mutation in UI)
- **Edge Cases:**
  - No events match filters → empty state "No events match the selected filters"; no error
  - Admin session expires mid-browse → on next API call, 401 returns; redirect to `/login?redirect=/admin/audit-log`
  - Export with 0 rows → CSV with headers only; toast still shows

---

## Design References [CONDITIONAL: UI Impact = Yes]
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-017-admin-audit-log.html |
| **Screen Spec** | SCR-017 |
| **UXR Requirements** | UXR-105 |
| **Design Tokens** | `--c-err`, `--c-ok`, `--cb`, `--c-warn`, `--c-ai`, `--ct-s` from variables.css |

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
| Frontend | React (SPA) | 18.x | TR-001 — Admin audit log viewer on SCR-017 |

---

## Task Overview
Build SCR-017 — the Admin Audit Log viewer page. The page is fully read-only; all mutation controls are absent. It uses the same Admin Portal top-nav shell as SCR-015 with "👥 Users" and "📋 Audit Log" (active) links. The view consists of: (1) a HIPAA notice banner, (2) a 4-card stat strip (events today, unique users, PHI access, failed attempts), (3) a filter bar with date range / action category / role / status / text search, (4) a paginated table (15 rows/page) with inline row-expand detail panels, and (5) a CSV export button.

## Dependent Tasks
- `task_001_frontend-admin-user-mgmt.md` (US_024) — `AdminPortalLayout` component and `<RequireRole role="Admin" />` guard must already exist
- `task_002_backend-admin-audit-log.md` (US_025) — API endpoints must be available before full integration testing

## Impacted Components
- `frontend/src/pages/AdminAuditLogPage.tsx` — new SCR-017 Admin-only page
- `frontend/src/components/admin/AuditLogTable.tsx` — new paginated audit event table
- `frontend/src/components/admin/AuditEventDetailRow.tsx` — new inline expand row detail panel
- `frontend/src/components/admin/AuditFilterBar.tsx` — new filter bar component
- `frontend/src/components/admin/AuditStatStrip.tsx` — new 4-card summary stat strip
- `frontend/src/hooks/useAuditLog.ts` — new hook for audit log fetch, filter state, pagination, and CSV export
- `frontend/src/App.tsx` — add `/admin/audit-log` route with Admin guard

## Implementation Plan
1. Create `AdminAuditLogPage.tsx` at `/admin/audit-log`; guard with `<RequireRole role="Admin" />`; render Admin nav with "👥 Users" link and "📋 Audit Log" active link; role badge "🔑 Admin"

2. Create `AuditStatStrip.tsx`:
   - 4 stat cards: "Events Today" (GET `/api/v1/admin/audit-log/stats?date=today`), "Unique Users", "PHI Access Events", "Failed Attempts"
   - `stat-value.err` style on failed-attempts count if > 0

3. Create `AuditFilterBar.tsx`:
   - Date "From" (type=date, default today), Date "To" (type=date, default today)
   - Select "Action Category": All / Authentication / Account Management / Scheduling / Clinical+PHI / Integration / Security
   - Select "Role": All / Admin / Staff / Patient / System
   - Select "Status": All / Success / Failure
   - Search input (debounced 300 ms): free-text matching user name, email, or resource
   - "✕ Reset" button restores all defaults; calls `resetFilters()` from `useAuditLog`

4. Create `useAuditLog.ts` hook:
   - State: `filters` (dateFrom, dateTo, actionCategory, role, status, search), `page` (1-based), `pageSize` (15)
   - `fetchEvents()`: `GET /api/v1/admin/audit-log?from=&to=&category=&role=&status=&q=&page=&pageSize=` — called on filter or page change (debounce 300 ms on search)
   - `fetchStats()`: `GET /api/v1/admin/audit-log/stats` — called once on mount
   - `exportCSV()`: `GET /api/v1/admin/audit-log/export?[current filters]` — triggers browser download via `<a download>` blob URL; shows toast on success
   - On 401: redirect to `/login?redirect=/admin/audit-log`
   - On 403: redirect to role-appropriate dashboard

5. Create `AuditLogTable.tsx`:
   - Renders the event table with sticky `<thead>`
   - Columns: Timestamp (date + time), User (name + email), Role (badge), Action (action-badge), Resource/Target, IP (monospace), Status (badge), Detail toggle
   - Action badges: AUTH (blue) · CREATE (green) · UPDATE (amber) · DELETE/DEACTIVATE (red) · VIEW (grey) · EXPORT (purple) · FAILURE (red border)
   - Role badges: Admin (amber) · Staff (green) · Patient (blue) · System (grey)
   - Status badges: ● Success (green) · ✕ Failure (red)
   - PHI indicator: if `event.phiAccess === true`, render `🔒 PHI` inline badge next to action text
   - Row highlight on hover (`--cs-m` background)
   - "Detail ▾" button: calls `toggleExpand(eventId)`; sets `aria-expanded`; Escape key listener closes all expand rows

6. Create `AuditEventDetailRow.tsx`:
   - Renders inline expand panel (`<tr>`) below the parent row
   - 3-column grid: (A) Event Identity — Event ID, Correlation ID, Session ID, ISO 8601 timestamp; (B) Actor — User ID, name, role, IP/user-agent; (C) Payload — resource, parameters JSON (pre-formatted code block), result
   - PHI events: label patient ID as "🔒 [id] (PHI)"; never expose PHI values directly
   - Immutable notice footer: amber strip "🔒 This record is immutable and cannot be modified or deleted."
   - Single expand: opening a new row closes all others

7. Pagination footer (inline in `AuditLogTable`):
   - Left: "Showing {start}–{end} of {total} events"
   - Right: ‹ prev | 1 2 3 … N | next ›
   - Active page styled with `--cb` background; disabled state on first/last pages

8. HIPAA banner (inline in `AdminAuditLogPage`):
   - Always visible; not dismissible
   - Text: "🔒 HIPAA Notice: This log contains PHI access records. Access is restricted to authorized administrators. Each view of this screen is itself logged."

9. Export CSV button ("⬇ Export CSV"):
   - Calls `useAuditLog.exportCSV()`
   - On success: blob download triggers; `<Toast>` with "✓ Audit log exported — audit_log_[date].csv" shown for 3.5 s
   - Button not disabled during export; spinner shown on the button during fetch

## Current Project State
```
frontend/
  src/
    context/AuthContext.tsx              (from US_010)
    components/admin/
      AdminPortalLayout.tsx              (from US_024, with nav links)
      UserSearchPanel.tsx                (from US_024)
    pages/AdminUserManagementPage.tsx    (from US_024)
    styles/variables.css
    App.tsx
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/pages/AdminAuditLogPage.tsx | SCR-017 Admin-only audit log viewer page |
| CREATE | frontend/src/components/admin/AuditLogTable.tsx | Paginated event table with expand rows |
| CREATE | frontend/src/components/admin/AuditEventDetailRow.tsx | Inline expand detail panel |
| CREATE | frontend/src/components/admin/AuditFilterBar.tsx | Filter controls: date, category, role, status, search |
| CREATE | frontend/src/components/admin/AuditStatStrip.tsx | 4-card summary stat strip |
| CREATE | frontend/src/hooks/useAuditLog.ts | Fetch, filter, pagination, export hook |
| MODIFY | frontend/src/App.tsx | Add `/admin/audit-log` route with Admin guard |
| MODIFY | frontend/src/components/admin/AdminPortalLayout.tsx | Add "📋 Audit Log" nav link pointing to /admin/audit-log |

## External References
- [wireframe-SCR-017-admin-audit-log.html](.propel/context/wireframes/Hi-Fi/wireframe-SCR-017-admin-audit-log.html)
- [HIPAA §164.312(b) — Audit Controls](https://www.hhs.gov/hipaa/for-professionals/security/guidance/index.html)
- [OWASP A01 — Access Control](https://owasp.org/Top10/A01_2021-Broken_Access_Control/)

## Build Commands
- Refer to [frontend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Navigate to `/admin/audit-log` as Admin → page renders with HIPAA banner, stat strip, filter bar, and paginated table
- [ ] Navigate to `/admin/audit-log` as Staff or Patient → redirected to respective dashboard; no audit data visible
- [ ] Apply date filter: only events in range shown; count label updates
- [ ] Apply "FAILURE" status filter → only failure events shown; action badge shows red
- [ ] Expand a PHI event row → 🔒 PHI indicator visible in action cell; detail panel labels patient ID as PHI
- [ ] Expand non-PHI row → no PHI indicator; detail shows payload JSON
- [ ] Open row A, then click expand on row B → row A closes; row B opens
- [ ] Press Escape → all expanded rows collapse
- [ ] Click "✕ Reset" → all filters cleared; full result set restored
- [ ] Click "Export CSV" → browser download triggers; toast appears; button re-enables after download
- [ ] Inspect API calls → only GET requests made; no POST/PATCH/DELETE to audit log endpoints

## Implementation Checklist
- [x] `AdminAuditLogPage` guarded with `<RequireRole role="Admin" />`; redirects non-Admin (AC-007)
- [x] HIPAA notice banner always visible; not dismissible (AC-004)
- [x] `AuditStatStrip`: 4 cards populated from stats endpoint; failed-attempts error colour when > 0 (AC-001)
- [x] `AuditFilterBar`: all 5 filter controls; debounced search (300 ms); Reset restores defaults (AC-002)
- [x] `AuditLogTable`: paginated 15 rows/page; action badge colours correct; role/status badges; PHI indicator on applicable events (AC-001, AC-004)
- [x] `AuditEventDetailRow`: event ID + ISO timestamp + actor + payload; PHI patient ID labelled; immutable notice (AC-003)
- [x] Single-expand constraint: opening new row closes previous (AC-003)
- [x] Escape key closes all expanded rows (AC-003)
- [x] `exportCSV`: download triggered; toast shown; button not permanently disabled (AC-005)
- [x] No edit/delete/modify controls anywhere on the page (AC-006)
- [x] 401 on session expiry → redirect to `/login?redirect=/admin/audit-log` (edge case)
- [x] Empty state "No events match the selected filters" when result count = 0 (edge case)
