namespace UPACIP.Domain.Entities;

/// <summary>
/// A canonical clinical data entry produced by the AI de-duplication pipeline (US_027, AC-003, AIR-004).
///
/// <para>
/// Created by <c>DeduplicationJob</c> after Gemini merges overlapping <see cref="ExtractedClinicalData"/>
/// records for the same patient. Each entry represents a single normalised clinical fact linked
/// back to every source document that contributed to it.
/// </para>
///
/// <para>
/// PHI fields are encrypted at rest via the EF Core <c>EncryptedStringConverter</c>
/// (same pattern as <see cref="ExtractedClinicalData.EncryptedExtractedJson"/>).
/// </para>
/// </summary>
public class MergedClinicalEntry : BaseEntity
{
    /// <summary>Patient this entry belongs to.</summary>
    public Guid PatientId { get; set; }

    /// <summary>
    /// Profile section: "Vitals" | "Medications" | "Diagnoses" | "VisitHistory".
    /// Used by <c>GetPatientProfileQueryHandler</c> to group entries by section.
    /// </summary>
    public string SectionType { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable label for the entry (e.g. "Blood pressure", "Metformin 500 mg").
    /// PHI — stored encrypted.
    /// </summary>
    public string EncryptedLabel { get; set; } = string.Empty;

    /// <summary>
    /// Normalised canonical value (e.g. "120/80 mmHg", "500 mg once daily").
    /// PHI — stored encrypted.
    /// </summary>
    public string EncryptedCanonicalValue { get; set; } = string.Empty;

    /// <summary>
    /// JSON array of document IDs that contributed to this canonical entry
    /// (e.g. <c>["guid-1","guid-2"]</c>). Stored as plain JSON — not encrypted
    /// because document IDs alone are not PHI.
    /// </summary>
    public string SourceDocumentIds { get; set; } = "[]";

    /// <summary>AI confidence score 0–1 for this canonical entry (AIR-004).</summary>
    public double Confidence { get; set; }

    /// <summary>True when the entry label/value contains PHI (used by the profile API for UXR-402 rendering).</summary>
    public bool IsPhiField { get; set; } = true;

    /// <summary>UTC timestamp when the dedup pipeline produced this entry.</summary>
    public DateTimeOffset MergedAt { get; set; } = DateTimeOffset.UtcNow;

    // ── Navigation ─────────────────────────────────────────────────────────

    /// <summary>Patient user associated with this entry.</summary>
    public User Patient { get; set; } = null!;
}
