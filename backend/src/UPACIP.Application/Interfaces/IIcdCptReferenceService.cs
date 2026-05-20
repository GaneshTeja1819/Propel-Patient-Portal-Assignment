namespace UPACIP.Application.Interfaces;

/// <summary>
/// Validates medical codes against the ICD-10 and CPT reference codesets (US_030, AC-003).
/// Phase 1: format-pattern validation. Phase 2: full codeset lookup against reference DB.
/// </summary>
public interface IIcdCptReferenceService
{
    /// <summary>
    /// Returns <c>true</c> when <paramref name="code"/> is a structurally valid code
    /// for the specified <paramref name="codeType"/> (<c>"ICD10"</c>, <c>"CPT"</c>, or <c>"SNOMED"</c>).
    /// </summary>
    bool IsValidCode(string code, string codeType);
}
