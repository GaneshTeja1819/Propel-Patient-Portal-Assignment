# Task - TASK_002

## Requirement Reference
- **User Story:** us_025
- **Story Location:** .propel/context/tasks/EP-007/us_025/us_025.md
- **Acceptance Criteria:**
  - AC-001: `GET /api/v1/admin/audit-log` returns paginated list (15/page) of audit events, newest-first; `GET /api/v1/admin/audit-log/stats` returns today's summary counts
  - AC-002: Filter query parameters: `from`, `to`, `category`, `role`, `status`, `q` (free-text ILIKE on actorName, actorEmail, resource); all optional; combined with AND logic
  - AC-005: `GET /api/v1/admin/audit-log/export` returns a CSV file stream for all events matching current filters; the export action is itself written as `AUDIT_LOG_EXPORTED` to the audit log
  - AC-006: AuditLog table has no UPDATE or DELETE permissions granted to the application role; backend enforces HTTP 405 on non-GET methods; the endpoint never accepts a body that could mutate a log entry
  - AC-007: All audit log endpoints are protected by `[Authorize(Policy = "AdminPolicy")]`; Staff/Patient requests return HTTP 403
  - AC-008: Calling `GET /api/v1/admin/audit-log` writes an `AUDIT_LOG_VIEWED` entry to the audit log (actor = calling Admin, resource = "audit_log", timestamp)
- **Edge Cases:**
  - Filter returns 0 rows → HTTP 200 with `{ data: [], total: 0, page: 1, pageSize: 15 }` (no 404)
  - Export with 0 rows → HTTP 200 CSV with headers only; `AUDIT_LOG_EXPORTED` entry still written
  - Large export (>10,000 rows) → endpoint streams the response using `IAsyncEnumerable<AuditLogEntry>` to avoid memory pressure
  - Admin session expires mid-request → JWT expired → HTTP 401; no audit data returned

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
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | TR-002 — Admin audit log read endpoints |
| Database | PostgreSQL via Supabase | 15 | TR-003 — AuditLog entity; dedicated `audit` schema |

---

## Task Overview
Implement the Admin Audit Log read API under `[Authorize(Policy = "AdminPolicy")]`. Three endpoints are exposed:
1. `GET /api/v1/admin/audit-log` — filtered, paginated event list
2. `GET /api/v1/admin/audit-log/stats` — today's summary statistics
3. `GET /api/v1/admin/audit-log/export` — CSV file stream of filtered events

The `audit.AuditLog` table is INSERT-only for the application write role. A dedicated read-only admin role (`admin_read_role`) is granted SELECT on `audit.*` exclusively. The write `IAuditLogService` is used to log the `AUDIT_LOG_VIEWED` and `AUDIT_LOG_EXPORTED` meta-events. No mutation operations are exposed.

## Dependent Tasks
- `task_002_backend-admin-user-mgmt.md` (US_024) — `AdminPolicy`, `IAuditLogService`, and `admin_read_role` PostgreSQL role must be provisioned from EP-DATA and US_024
- EP-DATA backend task — `audit` schema, `AuditLog` entity EF Core migration, INSERT-only application role, and `admin_read_role` SELECT grant must exist

## Impacted Components
- `backend/src/UPACIP.API/Controllers/AdminAuditLogController.cs` — new controller
- `backend/src/UPACIP.Application/Queries/AuditLog/GetAuditLogQuery.cs` — new query
- `backend/src/UPACIP.Application/Queries/AuditLog/GetAuditLogStatsQuery.cs` — new stats query
- `backend/src/UPACIP.Application/Handlers/AuditLog/GetAuditLogHandler.cs` — new handler
- `backend/src/UPACIP.Application/Handlers/AuditLog/GetAuditLogStatsHandler.cs` — new handler
- `backend/src/UPACIP.Application/DTOs/AuditLogEntryDto.cs` — new DTO
- `backend/src/UPACIP.Application/DTOs/AuditLogStatsDto.cs` — new DTO
- `backend/src/UPACIP.Infrastructure/Repositories/AuditLogReadRepository.cs` — new read repository (uses `admin_read_role` connection)

## Implementation Plan
1. **PostgreSQL setup** (EP-DATA dependency — confirm before starting):
   - Verify `audit` schema exists with INSERT-only for the application role
   - Create `admin_read_role` PostgreSQL role with `GRANT SELECT ON audit.audit_log TO admin_read_role`
   - Register a second `DbContext` or connection string (`AuditReadDbContext`) that uses `admin_read_role` credentials; inject it via DI with a named key `"AuditRead"` to prevent accidental writes

2. Create `AuditLogEntryDto.cs`:
   ```csharp
   record AuditLogEntryDto(
       string EventId, string CorrelationId, string SessionId,
       DateTime Timestamp, string ActorId, string ActorName,
       string ActorEmail, string ActorRole, string IpAddress,
       string UserAgent, string ActionType, string ActionCategory,
       string Resource, string TargetId, string PayloadJson,
       string Status, bool PhiAccess
   );
   ```
   - `PhiAccess` = `true` when `ActionType` ∈ `["PROFILE_VIEWED", "DOCUMENT_UPLOADED", "INTAKE_SUBMITTED", "CODES_VERIFIED"]`
   - `PayloadJson` must never contain decrypted PHI values; masked fields return `"[PHI-REDACTED]"` for any field tagged as PHI in the AuditLog entity

3. Create `GetAuditLogQuery.cs`:
   - Parameters: `DateFrom?`, `DateTo?`, `ActionCategory?`, `Role?`, `Status?`, `SearchText?`, `Page` (default 1), `PageSize` (default 15, max 100)

4. Implement `GetAuditLogHandler.cs`:
   - Uses `AuditReadDbContext` (read-only role)
   - Build `IQueryable<AuditLog>` with AND-chained WHERE clauses:
     - `DateFrom`: `entry.Timestamp >= DateFrom.Value.Date` (UTC start of day)
     - `DateTo`: `entry.Timestamp < DateTo.Value.Date.AddDays(1)` (UTC end of day)
     - `ActionCategory`: map category name to set of `ActionType` values (e.g., "CLINICAL" → `["PROFILE_VIEWED","DOCUMENT_UPLOADED","INTAKE_SUBMITTED","CODES_VERIFIED"]`)
     - `Role`: `entry.ActorRole == Role`
     - `Status`: `entry.Status == Status`
     - `SearchText`: ILIKE on `ActorName`, `ActorEmail`, `Resource` (PostgreSQL `EF.Functions.ILike`)
   - `OrderByDescending(e => e.Timestamp)` then `Skip((Page-1)*PageSize).Take(PageSize)`
   - Return `PagedResult<AuditLogEntryDto>` with `{ Data, Total, Page, PageSize }`
   - Write `AUDIT_LOG_VIEWED` via `IAuditLogService` after query executes (fire-and-forget; does not block response)

5. Create `GetAuditLogStatsQuery.cs` and `GetAuditLogStatsHandler.cs`:
   - Single query scoped to UTC today's date
   - Returns `AuditLogStatsDto`: `{ EventsToday, UniqueUsers, PhiAccessEvents, FailedAttempts }`
   - `PhiAccessEvents`: count where `ActionType` IN `["PROFILE_VIEWED","DOCUMENT_UPLOADED","INTAKE_SUBMITTED","CODES_VERIFIED"]` AND `Timestamp >= today`
   - `FailedAttempts`: count where `Status == "FAILURE"` AND `Timestamp >= today`
   - Uses `AuditReadDbContext`

6. Implement `GetAuditLogHandler.cs` — CSV Export path:
   - `GET /api/v1/admin/audit-log/export?[same filter params as list]`
   - Returns `FileStreamResult` with `Content-Disposition: attachment; filename="audit_log_{date}.csv"`
   - Uses `IAsyncEnumerable` to stream rows; avoids loading all rows into memory
   - CSV columns: EventId, Timestamp, ActorName, ActorEmail, ActorRole, ActionType, Resource, IpAddress, Status, PhiAccess
   - PHI field values are not included in the CSV; `PayloadJson` column excluded from export
   - After streaming begins: write `AUDIT_LOG_EXPORTED` via `IAuditLogService` with `{ "date": today, "filters": queryParams, "format": "csv" }` in payload (fire-and-forget)

7. Create `AdminAuditLogController.cs`:
   ```csharp
   [ApiController]
   [Route("api/v1/admin/audit-log")]
   [Authorize(Policy = "AdminPolicy")]
   public class AdminAuditLogController : ControllerBase
   {
       [HttpGet]        // list + pagination
       [HttpGet("stats")]   // summary stats
       [HttpGet("export")]  // CSV stream
   }
   ```
   - No `[HttpPost]`, `[HttpPut]`, `[HttpPatch]`, `[HttpDelete]` methods — HTTP 405 returned for any other verb by default
   - `[ProducesResponseType(StatusCodes.Status403Forbidden)]` declared on controller-level

8. **Security hardening**:
   - `AuditReadDbContext` connection string uses `admin_read_role` credentials stored in GitHub Secrets; never use the write role for reads on this controller
   - Add `[ResponseCache(NoStore = true)]` to all audit endpoints (HIPAA: no cached PHI in proxy)
   - Validate all query parameter inputs; reject malformed dates (return HTTP 400) before querying
   - Confirm `PayloadJson` masking: any field with `phiTagged = true` in the domain entity is replaced with `"[PHI-REDACTED]"` before serialization; unit test this masking

## Current Project State
```
backend/
  src/
    UPACIP.Infrastructure/Auth/JwtAuthService.cs       (AdminPolicy registered)
    UPACIP.Infrastructure/Audit/AuditLogService.cs     (IAuditLogService — write only)
    UPACIP.Domain/Entities/AuditLog.cs                 (from EP-DATA)
    UPACIP.Infrastructure/Persistence/AppDbContext.cs
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/UPACIP.API/Controllers/AdminAuditLogController.cs | Admin audit log read endpoints (GET only) |
| CREATE | backend/src/UPACIP.Application/Queries/AuditLog/GetAuditLogQuery.cs | Filtered + paginated audit log query |
| CREATE | backend/src/UPACIP.Application/Queries/AuditLog/GetAuditLogStatsQuery.cs | Today's summary stats query |
| CREATE | backend/src/UPACIP.Application/Handlers/AuditLog/GetAuditLogHandler.cs | Query handler + AUDIT_LOG_VIEWED meta-event |
| CREATE | backend/src/UPACIP.Application/Handlers/AuditLog/GetAuditLogStatsHandler.cs | Stats handler |
| CREATE | backend/src/UPACIP.Application/DTOs/AuditLogEntryDto.cs | Response DTO with PHI masking flag |
| CREATE | backend/src/UPACIP.Application/DTOs/AuditLogStatsDto.cs | Stats response DTO |
| CREATE | backend/src/UPACIP.Infrastructure/Repositories/AuditLogReadRepository.cs | Read repository using admin_read_role connection |
| MODIFY | backend/src/UPACIP.Infrastructure/Persistence/AuditReadDbContext.cs | Register read-only DbContext with admin_read_role credentials |
| MODIFY | backend/src/UPACIP.API/Program.cs | Register AuditReadDbContext DI; register query handlers |

## External References
- [HIPAA §164.312(b) — Audit Controls](https://www.hhs.gov/hipaa/for-professionals/security/guidance/index.html)
- [OWASP A01 — Broken Access Control](https://owasp.org/Top10/A01_2021-Broken_Access_Control/)
- [PostgreSQL — Role-based access control](https://www.postgresql.org/docs/current/user-manag.html)
- [EF Core — IAsyncEnumerable streaming](https://learn.microsoft.com/en-us/ef/core/querying/async)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] `GET /api/v1/admin/audit-log` as Admin → HTTP 200, paginated result, newest-first
- [ ] `GET /api/v1/admin/audit-log` as Staff → HTTP 403
- [ ] `GET /api/v1/admin/audit-log` as Patient → HTTP 403
- [ ] `GET /api/v1/admin/audit-log?status=FAILURE` → only FAILURE events returned
- [ ] `GET /api/v1/admin/audit-log?q=sarah` → only events with "sarah" in actorName/actorEmail/resource returned (case-insensitive)
- [ ] `GET /api/v1/admin/audit-log?from=2026-05-15&to=2026-05-15` → events scoped to that UTC day
- [ ] `GET /api/v1/admin/audit-log` with no matching filters → HTTP 200 `{ data: [], total: 0 }`
- [ ] `GET /api/v1/admin/audit-log/stats` → HTTP 200 with 4 integer fields
- [ ] `GET /api/v1/admin/audit-log/export` → HTTP 200, Content-Disposition: attachment, CSV headers present, rows stream
- [ ] `GET /api/v1/admin/audit-log` call writes `AUDIT_LOG_VIEWED` entry to audit log
- [ ] `GET /api/v1/admin/audit-log/export` call writes `AUDIT_LOG_EXPORTED` entry
- [ ] `POST /api/v1/admin/audit-log` → HTTP 405
- [ ] `DELETE /api/v1/admin/audit-log/[id]` → HTTP 405
- [ ] PHI fields in `PayloadJson` replaced with `"[PHI-REDACTED]"` in all responses
- [ ] `AuditReadDbContext` uses `admin_read_role` credentials; write operations on this context throw at DB layer

## Implementation Checklist
- [ ] `admin_read_role` PostgreSQL role provisioned with SELECT on `audit.*` only (EP-DATA dependency confirmed)
- [ ] `AuditReadDbContext` registered with `admin_read_role` connection string from GitHub Secrets (AC-006)
- [ ] `AdminAuditLogController` decorated with `[Authorize(Policy = "AdminPolicy")]`; Staff/Patient → HTTP 403 (AC-007)
- [ ] GET list: all filter params applied as AND-chained WHERE clauses; ordered newest-first; paginated (AC-001, AC-002)
- [ ] GET stats: scoped to UTC today; returns `EventsToday`, `UniqueUsers`, `PhiAccessEvents`, `FailedAttempts` (AC-001)
- [ ] GET export: CSV stream via `IAsyncEnumerable`; no PHI values in CSV; `AUDIT_LOG_EXPORTED` written (AC-005)
- [ ] `AUDIT_LOG_VIEWED` written per list request (fire-and-forget; does not block response) (AC-008)
- [ ] `[ResponseCache(NoStore = true)]` on all endpoints (HIPAA compliance)
- [ ] No `[HttpPost/Put/Patch/Delete]` on controller; HTTP 405 for all non-GET verbs (AC-006)
- [ ] `PayloadJson` PHI masking: unit tested for all `phiTagged` fields (AC-004)
- [ ] Empty result returns HTTP 200 `{ data: [], total: 0 }` not HTTP 404 (edge case)
