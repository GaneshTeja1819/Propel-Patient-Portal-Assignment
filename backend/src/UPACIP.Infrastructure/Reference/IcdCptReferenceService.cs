using System.Text.RegularExpressions;
using UPACIP.Application.Interfaces;

namespace UPACIP.Infrastructure.Reference;

/// <summary>
/// Validates medical codes against the ICD-10, CPT, and SNOMED reference codesets (US_030, AC-003).
///
/// Phase 1: structural format validation using well-defined patterns for each code system.
/// This avoids embedding a full multi-MB codeset while still catching clearly invalid codes.
/// Phase 2 (planned): replace with DB-backed lookup against a reference table populated
/// from official CMS ICD-10-CM and AMA CPT code files.
///
/// ICD-10-CM format: letter + 2 digits + optional decimal + 1-4 alphanumeric chars (e.g. Z00.00, J18.9).
/// CPT format: 5 digits, optionally followed by F/T/U modifier (e.g. 99213, 99213F).
/// SNOMED CT format: 6-18 digit numeric concept ID (e.g. 38341003).
/// </summary>
public sealed class IcdCptReferenceService : IIcdCptReferenceService
{
    // Structural patterns — not a full codeset validation but sufficient for Phase 1.
    // ICD-10-CM allows alphanumeric characters in the decimal portion (7th-character extensions
    // such as S72.001XA are valid). Pattern updated from \d{1,4} to [A-Z0-9]{1,4} (GAP-002 fix).
    private static readonly Regex Icd10Pattern =
        new(@"^[A-Z]\d{2}(\.[A-Z0-9]{1,4})?[A-Z0-9]?$", RegexOptions.Compiled);

    private static readonly Regex CptPattern =
        new(@"^\d{5}[FTU]?$", RegexOptions.Compiled);

    private static readonly Regex SnomedPattern =
        new(@"^\d{6,18}$", RegexOptions.Compiled);

    /// <inheritdoc />
    public bool IsValidCode(string code, string codeType)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        var normalised = code.Trim().ToUpperInvariant();

        return codeType?.ToUpperInvariant() switch
        {
            "ICD10"  => Icd10Pattern.IsMatch(normalised),
            "CPT"    => CptPattern.IsMatch(normalised),
            "SNOMED" => SnomedPattern.IsMatch(normalised),
            _        => false   // Unknown code system — reject
        };
    }
}
