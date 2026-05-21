# Task - TASK_003

## Requirement Reference
- **User Story:** us_030
- **Story Location:** .propel/context/tasks/EP-010/us_030/us_030.md
- **Acceptance Criteria:**
  - AC-002: VerifiedMedicalCode must record `decision = "Accepted"` and `originalSuggestedCode` for traceability
  - AC-003: VerifiedMedicalCode must record `decision = "Modified"` and both `originalSuggestedCode` and the final `verifiedCode`
  - AC-004: VerifiedMedicalCode must record `decision = "Rejected"`
  - AC-005: When all suggestions for an encounter are rejected, `ExtractedClinicalData.CodingStatus` must be settable to `"PendingManualCoding"` by the backend handler
- **Edge Cases:**
  - Existing `VerifiedMedicalCode` rows (if any) must default `decision = ""` (empty, not null) so the NOT NULL constraint does not break existing data
  - `CodingStatus` on `ExtractedClinicalData` defaults to `"Pending"` for all existing rows

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
| Database | PostgreSQL via Supabase | 15 | DR-001–DR-007; TR-003 — VerifiedMedicalCode and ExtractedClinicalData schema extension |
| Backend | EF Core | 9.0.x | TR-002 — entity model + migration toolchain |

---

## Task Overview
Extend two existing Domain entities to support the Staff code verification workflow (US_030):

1. **`VerifiedMedicalCode`** — add `Decision` (string: "Accepted" | "Modified" | "Rejected") and `OriginalSuggestedCode` (nullable string) columns. `Decision` records the Staff's action; `OriginalSuggestedCode` captures the AI-suggested value before modification, enabling full traceability for "Modified" decisions (AC-002, AC-003, AC-004, AIR-005).

2. **`ExtractedClinicalData`** — add `CodingStatus` (string, default "Pending") column. The `VerifyCodeHandler` sets this to `"PendingManualCoding"` when all suggestions for the encounter are rejected, and `"Complete"` when at least one code is Accepted/Modified (AC-005).

Configure column mappings in `AppDbContext`, then generate and apply an EF Core migration. This task is a prerequisite for `task_002_backend-code-verification.md`.

## Dependent Tasks
- None — this task unblocks `task_002_backend-code-verification.md`

## Impacted Components
- `backend/src/UPACIP.Domain/Entities/VerifiedMedicalCode.cs` — add `Decision` (string) and `OriginalSuggestedCode` (string?)
- `backend/src/UPACIP.Domain/Entities/ExtractedClinicalData.cs` — add `CodingStatus` (string, default "Pending")
- `backend/src/UPACIP.Infrastructure/Persistence/AppDbContext.cs` — EF Core config: column mappings for 3 new fields; `CodingStatus` default value
- `backend/src/UPACIP.Infrastructure/Migrations/` — new migration file

## Implementation Plan
1. Open `VerifiedMedicalCode.cs` and add two properties:
   ```csharp
   public string Decision { get; set; } = string.Empty;              // AC-002/03/04: "Accepted" | "Modified" | "Rejected"
   public string? OriginalSuggestedCode { get; set; }                // AC-003: AI-suggested value before modification; null for Accepted/Rejected
   ```
2. Open `ExtractedClinicalData.cs` and add one property:
   ```csharp
   public string CodingStatus { get; set; } = "Pending";             // AC-005: "Pending" | "PendingManualCoding" | "Complete"
   ```
3. In `AppDbContext.cs`, within the `VerifiedMedicalCode` EF configuration block, add:
   - `.Property(v => v.Decision).HasColumnName("decision").HasMaxLength(32).HasDefaultValue("").IsRequired()`
   - `.Property(v => v.OriginalSuggestedCode).HasColumnName("original_suggested_code").HasMaxLength(32)`
4. In `AppDbContext.cs`, within the `ExtractedClinicalData` EF configuration block (create block if not present), add:
   - `.Property(e => e.CodingStatus).HasColumnName("coding_status").HasMaxLength(32).HasDefaultValue("Pending").IsRequired()`
5. Hand-craft the EF Core migration file under `UPACIP.Infrastructure/Migrations/` following the timestamp pattern (`YYYYMMDDHHMMSS_AddDecisionCodingStatusFields.cs`):
   - `AddColumn<string>` for `decision` on `verified_medical_codes` (maxLength 32, defaultValue `""`)
   - `AddColumn<string?>` for `original_suggested_code` on `verified_medical_codes` (maxLength 32, nullable)
   - `AddColumn<string>` for `coding_status` on `extracted_clinical_data` (maxLength 32, defaultValue `"Pending"`)
   - Implement matching `Down()` with `DropColumn` for all three columns
6. Update `AppDbContextModelSnapshot.cs` to add the three new property blocks to the respective entity sections.

## Current Project State
```
backend/src/
  UPACIP.Domain/Entities/
    VerifiedMedicalCode.cs            (exists — SuggestionId, VerifiedById, CodeSystem, Code, Description, VerifiedAt, Notes)
    ExtractedClinicalData.cs          (exists — DocumentId, PatientId, EncryptedExtractedJson, ExtractionModel, ExtractedAt, ConfidenceScore)
  UPACIP.Infrastructure/Persistence/
    AppDbContext.cs                   (exists — VerifiedMedicalCodes DbSet at line 47; EF config block at line 313)
  UPACIP.Infrastructure/Migrations/
    20260517145221_InitialCreate.cs   (exists — baseline schema)
    20260520140000_AddRankStatusAuditFieldsToMedicalCodeSuggestion.cs (exists — MedicalCodeSuggestion extension)
    AppDbContextModelSnapshot.cs      (exists — VerifiedMedicalCode entity at line 619; ExtractedClinicalData at ~580)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | backend/src/UPACIP.Domain/Entities/VerifiedMedicalCode.cs | Add `Decision` (string) and `OriginalSuggestedCode` (string?) properties |
| MODIFY | backend/src/UPACIP.Domain/Entities/ExtractedClinicalData.cs | Add `CodingStatus` (string, default "Pending") property |
| MODIFY | backend/src/UPACIP.Infrastructure/Persistence/AppDbContext.cs | Column mappings for `decision`, `original_suggested_code`; `CodingStatus` config on ExtractedClinicalData |
| CREATE | backend/src/UPACIP.Infrastructure/Migrations/20260520150000_AddDecisionCodingStatusFields.cs | EF Core migration: adds `decision`, `original_suggested_code`, `coding_status` columns; Down() reverses |
| MODIFY | backend/src/UPACIP.Infrastructure/Migrations/AppDbContextModelSnapshot.cs | Add Decision, OriginalSuggestedCode to VerifiedMedicalCode block; add CodingStatus to ExtractedClinicalData block |

## External References
- [EF Core migrations — add column with default value](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [AIR-005 — Human verification gate](https://owasp.org/Top10/A04_2021-Insecure_Design/)

## Build Commands
- Refer to [backend build commands](.propel/build/)

## Implementation Validation Strategy
- [ ] `get_errors` on UPACIP.Domain and UPACIP.Infrastructure reports 0 errors after entity modification
- [ ] Migration file contains `decision` (varchar 32, default ""), `original_suggested_code` (varchar 32, nullable), `coding_status` (varchar 32, default "Pending"); no destructive operations
- [ ] `AppDbContextModelSnapshot.cs` updated to include all three new property blocks

## Implementation Checklist
- [x] `VerifiedMedicalCode.Decision` (string, default `""`) added — enables "Accepted" | "Modified" | "Rejected" storage (AC-002, AC-004)
- [x] `VerifiedMedicalCode.OriginalSuggestedCode` (string?, nullable) added — captures AI-suggested code before modification (AC-003, AIR-005 traceability)
- [x] `ExtractedClinicalData.CodingStatus` (string, default "Pending") added — enables "PendingManualCoding" flag when all suggestions rejected (AC-005)
- [x] EF Core column mappings configured (`decision`, `original_suggested_code`, `coding_status`) with correct constraints and defaults
- [x] Migration file created with all three `AddColumn` calls and reversible `Down()` implementation
- [x] `AppDbContextModelSnapshot.cs` updated to reflect the three new columns
