---
task: task_002_database-medical-code-suggestion
us: us_029
reviewer: GitHub Copilot (analyze-implementation workflow)
reviewed_at: 2026-05-20
verdict: Conditional Pass
---

# Implementation Analysis — task_002_database-medical-code-suggestion.md

## Verdict

**Status:** Conditional Pass

**Summary:** The entity extension and EF Core configuration for `MedicalCodeSuggestion` are correctly implemented: all four new properties (`Rank`, `Status`, `ModelVersion`, `PromptHash`) are present in the Domain entity and mapped to PostgreSQL columns (`rank`, `status`, `model_version`, `prompt_hash`) with appropriate constraints and defaults. The migration file adds all four columns and creates the composite index. Build passes with zero errors. One material gap blocks an unconditional Pass: the `AppDbContextModelSnapshot.cs` was not updated to reflect the new columns or the composite index, which is explicitly listed as a validation criterion and will cause EF Core tooling to treat the four columns as "pending" additions on the next `dotnet ef migrations add` invocation. A secondary concern is the mixed PascalCase / snake_case column naming in the migration index definition.

---

## Traceability Matrix

| Requirement / Acceptance Criterion | Evidence (file : fn / line) | Result |
|---|---|---|
| **AC-002 `Rank` column** — ranked position (1 = highest) | `MedicalCodeSuggestion.cs` L11: `public int Rank { get; set; }` | Pass |
| **AC-002 `Rank` EF config** — `HasColumnName("rank").IsRequired()` | `AppDbContext.cs` L241 | Pass |
| **AC-002 `Rank` migration** — `integer`, defaultValue 0 | `20260520140000…cs` L17–23 | Pass |
| **AC-002 `ConfidenceScore`** — pre-existing field unchanged | `MedicalCodeSuggestion.cs` L10: `public double ConfidenceScore { get; set; }` | Pass |
| **AC-002 `ClinicalDataId` FK (derivedFrom)** — pre-existing FK unchanged | `AppDbContext.cs` L257–L260: `HasForeignKey(m => m.ClinicalDataId)` | Pass |
| **Status column** — drives idempotency guard; default "Pending" | `MedicalCodeSuggestion.cs` L12: `public string Status { get; set; } = "Pending"` | Pass |
| **Status EF config** — `HasColumnName("status").HasMaxLength(32).HasDefaultValue("Pending").IsRequired()` | `AppDbContext.cs` L242–L246 | Pass |
| **Status migration** — `character varying(32)`, defaultValue "Pending" | `20260520140000…cs` L25–32 | Pass |
| **AC-004 / AIR-006 `ModelVersion`** — Gemini model identifier per record | `MedicalCodeSuggestion.cs` L13: `public string ModelVersion { get; set; } = string.Empty` | Pass |
| **AC-004 / AIR-006 `ModelVersion` EF config** — `HasColumnName("model_version").HasMaxLength(128).IsRequired()` | `AppDbContext.cs` L247–L250 | Pass |
| **AC-004 / AIR-006 `ModelVersion` migration** — `character varying(128)` | `20260520140000…cs` L34–40 | Pass |
| **AC-004 / AIR-006 `PromptHash`** — SHA-256 of prompt template per record | `MedicalCodeSuggestion.cs` L14: `public string PromptHash { get; set; } = string.Empty` | Pass |
| **AC-004 / AIR-006 `PromptHash` EF config** — `HasColumnName("prompt_hash").HasMaxLength(64).IsRequired()` | `AppDbContext.cs` L251–L254 | Pass |
| **AC-004 / AIR-006 `PromptHash` migration** — `character varying(64)` | `20260520140000…cs` L42–48 | Pass |
| **Edge — idempotency index** — `IX_MedicalCodeSuggestion_ClinicalDataId_Status` on `(ClinicalDataId, status)` | `AppDbContext.cs` L255–L256; `20260520140000…cs` L50–54 | Pass |
| **Edge — low-confidence storage** — no DB-level filter constraint | No CHECK constraint or partial index filtering by `ConfidenceScore` in migration | Pass |
| **Migration Down()** — reversible; drops all 4 columns and index | `20260520140000…cs` L58–80 | Pass |
| **Validation criterion 1** — `get_errors` on Domain + Infrastructure = 0 errors | Verified: 0 errors | Pass |
| **Validation criterion 2** — migration contains all 4 column additions + index; no drops | `20260520140000…cs`: 4 `AddColumn` + 1 `CreateIndex` in Up(); no `DropColumn` | Pass |
| **Validation criterion 3** — `AppDbContextModelSnapshot.cs` updated | Snapshot at line ~525 still reflects pre-task_002 state — `Rank`, `Status`, `ModelVersion`, `PromptHash` absent; `IX_MedicalCodeSuggestion_ClinicalDataId_Status` absent | **Gap** |

---

## Logical & Design Findings

**Business Logic:**

No business logic concerns — this is a schema extension task. Entity properties, EF Core mappings, and migration are all structurally correct. Default values are appropriate: `Status = "Pending"` (correct — all newly inserted suggestions await Staff action); `rank` defaults to `0` in the migration (integer default; not an application-visible value since `CodeSuggestionJob` always sets `Rank` explicitly from Gemini output).

**Security:** No PHI introduced. `model_version` and `prompt_hash` are technical audit fields. No input validation required at the schema layer. No concerns.

**Error Handling:** N/A — schema migration is non-conditional.

**Data Access:**

- **GAP-001 (High): `AppDbContextModelSnapshot.cs` not updated.**
  The snapshot is the authoritative "current model" state used by EF Core's `dotnet ef` toolchain. Because the migration was hand-crafted rather than generated by `dotnet ef migrations add`, the snapshot was never regenerated. It still reflects the pre-task_002 `MedicalCodeSuggestion` state (only: `Id`, `ClinicalDataId`, `CodeSystem`, `ConfidenceScore`, `Description`, `IsVerified`, `PatientId`, `SuggestedAt`, `SuggestedCode`). Consequences:
  1. `dotnet ef migrations add <Next>` will detect `Rank`, `Status`, `ModelVersion`, `PromptHash` as new pending additions and generate a duplicate migration.
  2. `dotnet ef database update` validation may warn of snapshot/migration divergence.
  3. This is the task's own validation criterion 3, which remains `[ ]` unverified.
  Fix: manually add the four property blocks and `IX_MedicalCodeSuggestion_ClinicalDataId_Status` index to the `MedicalCodeSuggestion` entity block in `AppDbContextModelSnapshot.cs`.

- **GAP-002 (Low): Mixed column naming in migration index.**
  `migrationBuilder.CreateIndex(columns: new[] { "ClinicalDataId", "status" })` mixes `"ClinicalDataId"` (PascalCase — EF Core default, no explicit `HasColumnName`) with `"status"` (snake_case — explicit `HasColumnName`). Since no global `UseSnakeCaseNamingConvention()` is configured (`DependencyInjection.cs` L51 calls `UseNpgsql` only), this will function correctly — the FK column is `ClinicalDataId` and the status column is `status`. However, the inconsistency within the same index makes the naming convention implicit and fragile if a global convention is added later.

**Performance:**

The composite index `IX_MedicalCodeSuggestion_ClinicalDataId_Status` directly covers the idempotency query `WHERE ClinicalDataId = @id AND (Status = 'Pending' OR Status = 'Processing')` used in `CodeSuggestionJob`. Index column order (ClinicalDataId first, status second) is correct for the equality-then-filter query pattern. ✅

**Patterns & Standards:**

All new column names follow the snake_case convention used for explicitly named columns elsewhere in `AppDbContext.cs` (`rank`, `status`, `model_version`, `prompt_hash`). `HasMaxLength` sizes are appropriate: 32 for a status enum-like field, 128 for model version strings, 64 for a SHA-256 hex digest (64 chars exact). ✅

---

## Test Review

**Existing Tests:** No tests are expected for a pure schema migration task.

**Missing Tests (must add):**

- [ ] Integration: After applying migration against a test database, verify all 4 columns exist on `medical_code_suggestions`; verify `IX_MedicalCodeSuggestion_ClinicalDataId_Status` index exists; verify existing rows default `status = 'Pending'` and `rank = 0`.
- [ ] Integration: `AppDbContextModelSnapshot` drift check — run `dotnet ef migrations has-pending-model-changes`; assert no pending changes after migration is applied.

---

## Validation Results

**Commands Executed:** `get_errors` on `UPACIP.Domain` and `UPACIP.Infrastructure`.

**Outcomes:** 0 errors — build passes. Validation criteria 1 and 2 confirmed. Validation criterion 3 (`AppDbContextModelSnapshot.cs` updated) is **not satisfied**.

---

## Fix Plan (Prioritized)

1. **Update `AppDbContextModelSnapshot.cs`** — add four property blocks and composite index entry to the `MedicalCodeSuggestion` entity block — `AppDbContextModelSnapshot.cs` lines ~525–545 — ETA 0.5 h — Risk: **M** — Required to prevent EF Core toolchain from re-generating these columns as a pending migration; satisfies task validation criterion 3.

2. **Verify index column naming** — confirm in the live database that the `ClinicalDataId` FK column is named `ClinicalDataId` (PascalCase) as per EF Core default convention, and document this explicitly in a `HasColumnName` call or a code comment in `AppDbContext.cs` to prevent silent breakage if `UseSnakeCaseNamingConvention` is added later — ETA 0.25 h — Risk: **L**.

---

## Appendix

**Rules Applied:**

- `rules/security-standards-owasp.md` — no PHI in new columns; no sensitive data exposure
- `rules/backend-development-standards.md` — EF Core entity and migration patterns
- `rules/dotnet-architecture-standards.md` — entity in Domain layer; EF config in Infrastructure; migration in Migrations/
- `rules/code-anti-patterns.md` — no magic constants in EF config (column names are explicit)
- `rules/dry-principle-guidelines.md` — no duplicate column definitions; delta update approach
- `rules/performance-best-practices.md` — composite index covers exact idempotency query pattern
- `rules/database-standards.md` — snake_case column names; reversible migration (Down() implemented)

**Search Evidence:**

- `grep "Rank|ModelVersion|PromptHash|model_version|prompt_hash" AppDbContextModelSnapshot.cs` → 0 matches — confirms snapshot is stale
- `grep "rank|status|model_version|prompt_hash|IX_MedicalCodeSuggestion" AppDbContext.cs` → 15 matches — confirms EF config is present
- `grep "UseSnakeCaseNamingConvention" DependencyInjection.cs` → 0 matches — confirms no global naming convention; PascalCase FK column name in index is correct
- Build: `get_errors` on Domain + Infrastructure → 0 errors
