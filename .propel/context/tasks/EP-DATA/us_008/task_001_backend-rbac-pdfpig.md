# Task - TASK_001

## Requirement Reference
- **User Story:** us_008
- **Story Location:** .propel/context/tasks/EP-DATA/us_008/us_008.md
- **Acceptance Criteria:**
  - AC-001: Patient calling `[Authorize(Policy = "StaffPolicy")]` endpoint receives HTTP 403; JWT role claim logged; no business logic executes
  - AC-002: Unauthenticated request to any protected endpoint returns HTTP 401; no data returned; missing JWT logged
  - AC-005: `IPdfTextExtractor` implementation extracts text from a sample PDF as a non-null string; corrupted PDF returns empty string (logged)
- **Edge Cases:**
  - Role claim missing from JWT → HTTP 401 before policy evaluation; no role-specific logic executes
  - Gemini API key not configured → startup configuration exception; application exits non-zero
  - PdfPig encounters corrupted PDF → catch exception, log document ID and error, return empty string

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
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | TR-008, NFR-005 — RBAC policy enforcement via ASP.NET Core authorization middleware |
| Backend | PdfPig | Latest stable | TR-017, NFR-008 — server-side PDF text extraction for AI pipeline; open-source, free-tier |

---

## Task Overview
Register three ASP.NET Core RBAC policies (`PatientPolicy`, `StaffPolicy`, `AdminPolicy`) that evaluate the `role` JWT claim. All protected controller actions receive the appropriate `[Authorize(Policy = "...")]` attribute as a standard convention. Integrate the `PdfPig` library in the Infrastructure layer via the `IPdfTextExtractor` interface — handling corrupted PDFs gracefully by returning an empty string and logging the error.

## Dependent Tasks
- `task_001_backend-jwt-auth.md` (US_007) — JWT authentication must be configured before RBAC policies can evaluate role claims

## Impacted Components
- `backend/src/UPACIP.API/Program.cs` — RBAC policy registrations
- `backend/src/UPACIP.Application/Interfaces/IPdfTextExtractor.cs` — new interface
- `backend/src/UPACIP.Infrastructure/Documents/PdfTextExtractor.cs` — new PdfPig implementation
- `backend/src/UPACIP.API/Controllers/HealthController.cs` — add `[Authorize]` baseline (example policy application)

## Implementation Plan
1. Register `PatientPolicy` (`role == "Patient"`), `StaffPolicy` (`role == "Staff"`), `AdminPolicy` (`role == "Admin"`) using `AddAuthorization` in `Program.cs`; policies evaluate the `role` claim from the JWT
2. Apply `[Authorize(Policy = "PatientPolicy")]` to Patient-facing endpoints, `[Authorize(Policy = "StaffPolicy")]` to Staff endpoints, `[Authorize(Policy = "AdminPolicy")]` to Admin endpoints; add `[Authorize]` baseline to all controllers (HTTP 401 for unauthenticated)
3. Verify cross-role rejection: an authenticated Patient calling a StaffPolicy endpoint returns HTTP 403; the role claim is logged at the `Warning` level without exposing other JWT claims
4. Define `IPdfTextExtractor` in Application layer: `ExtractText(byte[] pdfBytes, Guid documentId): string`
5. Add `PdfPig` NuGet package to Infrastructure project
6. Implement `PdfTextExtractor` in Infrastructure: open PDF with `PdfDocument.Open(pdfBytes)`; concatenate all page text; on any exception (including corrupted PDF), log `documentId` and exception message at `Error` level and return `string.Empty`

## Current Project State
```
backend/
  src/
    UPACIP.API/Program.cs  (JWT bearer configured from US_007)
    UPACIP.Application/Interfaces/  (IEncryptionService, IAuditLogService, IAuthService)
    UPACIP.Infrastructure/  (Auth, Caching, Security modules)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | backend/src/UPACIP.API/Program.cs | Register PatientPolicy, StaffPolicy, AdminPolicy; add authorization middleware |
| MODIFY | backend/src/UPACIP.API/Controllers/HealthController.cs | Add [Authorize] baseline attribute |
| CREATE | backend/src/UPACIP.Application/Interfaces/IPdfTextExtractor.cs | PDF text extraction interface |
| MODIFY | backend/src/UPACIP.Infrastructure/UPACIP.Infrastructure.csproj | Add PdfPig NuGet reference |
| CREATE | backend/src/UPACIP.Infrastructure/Documents/PdfTextExtractor.cs | PdfPig implementation; corrupted PDF → empty string + log |

## External References
- [ASP.NET Core Authorization Policies](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies)
- [PdfPig NuGet Package](https://www.nuget.org/packages/PdfPig)
- [PdfPig Docs](https://uglytoad.github.io/PdfPig/)
- [OWASP Broken Access Control (A01)](https://owasp.org/Top10/A01_2021-Broken_Access_Control/)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] Authenticated Patient calls `[Authorize(Policy = "StaffPolicy")]` endpoint → HTTP 403; role claim logged
- [ ] Unauthenticated request to `[Authorize]` endpoint → HTTP 401; no data in response body
- [ ] `PdfTextExtractor.ExtractText` with a valid PDF returns non-null, non-empty string
- [ ] `PdfTextExtractor.ExtractText` with a corrupted PDF byte array returns `string.Empty`; error logged with document ID

## Implementation Checklist
- [ ] Register PatientPolicy, StaffPolicy, AdminPolicy in `Program.cs`; each evaluates `role` JWT claim (AC-001, AC-002)
- [ ] Apply `[Authorize]` baseline to all controllers; apply role-specific `[Authorize(Policy = "...")]` to actions (AC-002)
- [ ] Verify Patient → StaffPolicy endpoint returns HTTP 403; role claim logged at Warning level (AC-001)
- [ ] Verify unauthenticated → any protected endpoint returns HTTP 401 (AC-002)
- [ ] Add PdfPig NuGet; define `IPdfTextExtractor` interface in Application layer (AC-005)
- [ ] Implement `PdfTextExtractor`: extract text from all pages; return `string.Empty` and log on exception (AC-005)
