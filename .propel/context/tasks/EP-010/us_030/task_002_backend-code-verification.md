# Task - TASK_002

## Requirement Reference
- **User Story:** us_030
- **Story Location:** .propel/context/tasks/EP-010/us_030/us_030.md
- **Acceptance Criteria:**
  - AC-001: `POST /api/v1/codes/verify` accepts { suggestionId, decision, verifiedCode? }; creates `VerifiedMedicalCode` record (immutable)
  - AC-002: decision="Accepted" → VerifiedMedicalCode with original suggestedCode; immutable `CODE_VERIFIED` audit; row unchangeable after this
  - AC-003: decision="Modified" → validate `verifiedCode` against ICD-10/CPT codeset; if valid → VerifiedMedicalCode with decision="Modified"; if invalid → HTTP 422 "Code not found in codeset"; no record created
  - AC-004: decision="Rejected" → VerifiedMedicalCode with decision="Rejected"; immutable audit
  - AC-005: All suggestions for encounter rejected → `ExtractedClinicalData.CodingStatus = "PendingManualCoding"`; immutable mass-rejection audit entry; no billing processing until a code is verified
- **Edge Cases:**
  - Encounter already "Finalized" → HTTP 422 "This encounter has been finalised"; no new VerifiedMedicalCode
  - Patient POSTs to verify endpoint → HTTP 403
  - Attempt to modify/re-verify a row that has an existing VerifiedMedicalCode → HTTP 409 "Code already verified"

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
| **AI Impact** | Yes |
| **AIR Requirements** | AIR-005 |
| **AI Pattern** | N/A |
| **Prompt Template Path** | N/A |
| **Guardrails Config** | Immutable audit trail; no UPDATE/DELETE on VerifiedMedicalCode or audit rows; codeset validation before record creation |
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
| Backend | .NET Web API (ASP.NET Core) | 8.0 LTS | TR-002 — code verification endpoint |
| Database | PostgreSQL via Supabase | 15 | TR-003 — VerifiedMedicalCode; immutable audit; Encounter status |

---

## Task Overview
Implement the Staff-only `POST /api/v1/codes/verify` endpoint that creates immutable `VerifiedMedicalCode` records. The handler validates the codeset for "Modified" decisions, checks for duplicate verification (HTTP 409), and checks for finalised encounter (HTTP 422). After creation it checks if all `MedicalCodeSuggestion` records for the encounter now have corresponding "Rejected" `VerifiedMedicalCode` entries — if so, sets `Encounter.status = "PendingManualCoding"`. A `GET /api/v1/codes/validate` utility endpoint is also added for real-time frontend codeset validation. All writes produce immutable audit entries (no UPDATE/DELETE permitted at DB row level).

A companion `GET /api/v1/code-suggestions?encounterId=` endpoint is added to the existing `CodeSuggestionsController` so SCR-014 can load rows.

## Dependent Tasks
- `task_003_database-verified-medical-code.md` (US_030) — `VerifiedMedicalCode.Decision`, `VerifiedMedicalCode.OriginalSuggestedCode`, and `ExtractedClinicalData.CodingStatus` columns must be applied via migration before handler logic references them
- `task_001_ai-code-suggestion.md` (US_029) — `MedicalCodeSuggestion` entity exists; `CodeSuggestionsController` exists
- `task_001_backend-jwt-auth.md` (US_007) — `StaffPolicy` must be registered

## Impacted Components
- `backend/src/UPACIP.Application/Commands/Codes/VerifyCodeCommand.cs` — new command
- `backend/src/UPACIP.Application/Handlers/Codes/VerifyCodeHandler.cs` — new handler
- `backend/src/UPACIP.API/Controllers/CodeVerificationController.cs` — new controller; POST /verify + GET /validate
- `backend/src/UPACIP.API/Controllers/CodeSuggestionsController.cs` — add GET /code-suggestions?encounterId

## Implementation Plan
1. Create `VerifyCodeCommand.cs`: `{ SuggestionId, Decision, VerifiedCode?, ActorStaffId }`
2. Implement `VerifyCodeHandler.HandleAsync`:
   - Load `MedicalCodeSuggestion` by `SuggestionId`; check `Encounter.status == "Finalized"` → HTTP 422
   - Check existing `VerifiedMedicalCode` for same `SuggestionId` → HTTP 409 "Code already verified"
   - If `Decision == "Modified"`: validate `VerifiedCode` against reference codeset (`IcdCptReferenceService.IsValidCodeAsync(code, codeType)`); if invalid → HTTP 422 "Code not found in codeset"
   - Create `VerifiedMedicalCode` (INSERT only; no UPDATE/DELETE logic in handler; DB column `is_immutable = true` or trigger-based):
     - `suggestionId`, `decision`, `verifiedCode` (VerifiedCode for Modified; suggestedCode for Accepted; null for Rejected), `verifiedAt`, `verifiedById`, `originalSuggestedCode`
   - Check "all rejected" condition: count `MedicalCodeSuggestion` for the same `ClinicalDataId` where no "Accepted" or "Modified" `VerifiedMedicalCode` exists; if all have Rejected → set `ExtractedClinicalData.CodingStatus = "PendingManualCoding"` (column added by task_003); write `MASS_REJECTION` audit
   - Write immutable `CODE_VERIFIED` audit: `actorRole = "Staff"`, `actorId`, `suggestionId`, `decision`, `verifiedCode`, `originalSuggestedCode`, `timestamp`
3. Create `CodeVerificationController`:
   - `POST /api/v1/codes/verify` — `[Authorize(Policy = "StaffPolicy")]`; HTTP 403 for Patient
   - `GET /api/v1/codes/validate?code=X&type=ICD10|CPT` — `[Authorize]`; calls `IcdCptReferenceService.IsValidCodeAsync`; returns `{ valid, description? }`
4. Create `IcdCptReferenceService.cs`: loads reference data from embedded JSON or DB table; `IsValidCodeAsync(code, codeType)` → bool
5. Add `GET /api/v1/code-suggestions?encounterId=` to `CodeSuggestionsController`: returns `MedicalCodeSuggestionDto[]` ordered by rank; Staff only

## Current Project State
```
backend/
  src/
    UPACIP.API/Controllers/CodeSuggestionsController.cs  (from US_029)
    UPACIP.Domain/Entities/MedicalCodeSuggestion.cs
    UPACIP.Domain/Entities/Encounter.cs
    UPACIP.Infrastructure/Audit/AuditLogService.cs
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/UPACIP.Application/Commands/Codes/VerifyCodeCommand.cs | Verify code command |
| CREATE | backend/src/UPACIP.Application/Handlers/Codes/VerifyCodeHandler.cs | Verify handler: codeset check, immutable write, all-rejected logic |
| CREATE | backend/src/UPACIP.API/Controllers/CodeVerificationController.cs | POST /verify + GET /validate endpoints |
| CREATE | backend/src/UPACIP.Infrastructure/Reference/IcdCptReferenceService.cs | ICD-10/CPT codeset validation service |
| MODIFY | backend/src/UPACIP.API/Controllers/CodeSuggestionsController.cs | Add GET /code-suggestions?encounterId |

## External References
- [OWASP A01 — Access Control](https://owasp.org/Top10/A01_2021-Broken_Access_Control/)
- [OWASP A04 — Insecure Design — immutable audit](https://owasp.org/Top10/A04_2021-Insecure_Design/)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] POST verify with decision="Accepted" → VerifiedMedicalCode row created; CODE_VERIFIED audit; second POST on same suggestion → HTTP 409
- [ ] decision="Modified" with invalid code → HTTP 422; no record; with valid code → record created with verifiedCode
- [ ] All suggestions rejected → Encounter.status = "PendingManualCoding"; MASS_REJECTION audit
- [ ] Encounter status = "Finalized" → HTTP 422 "This encounter has been finalised"
- [ ] Patient POST → HTTP 403
- [ ] Verify immutable audit: no UPDATE or DELETE permitted on VerifiedMedicalCode or CODE_VERIFIED audit rows at DB level

## Implementation Checklist
- [x] Duplicate verification check: HTTP 409 if VerifiedMedicalCode already exists for suggestionId (edge case)
- [x] Finalized encounter guard: HTTP 422 (edge case)
- [x] Codeset validation for "Modified" decision: HTTP 422 on invalid; no record (AC-003)
- [x] VerifiedMedicalCode INSERT-only; no UPDATE/DELETE handler logic (AC-002, AC-004, AIR-005)
- [x] Immutable CODE_VERIFIED audit with all required fields (AC-002, AC-004, AIR-005)
- [x] All-rejected → Encounter.status = "PendingManualCoding" + MASS_REJECTION audit (AC-005)
- [x] HTTP 403 for Patient on POST /verify (AC-005 edge case, OWASP A01)
- [x] GET /validate utility endpoint for frontend codeset validation (AC-003 support)
