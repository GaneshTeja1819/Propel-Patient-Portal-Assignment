namespace UPACIP.Application.Queries.Profile;

/// <summary>
/// Query to retrieve a patient's clinical profile (US_027, AC-001, AC-005).
/// Returns merged canonical entries when de-duplication has completed, or
/// raw extracted data when it is still pending/processing.
/// </summary>
/// <param name="PatientId">Identifier of the patient whose profile to fetch.</param>
/// <param name="Page">1-based page number (default 1).</param>
/// <param name="PageSize">Items per page (default 20, max 100).</param>
public sealed record GetPatientProfileQuery(
    Guid PatientId,
    int  Page     = 1,
    int  PageSize = 20);

// ── Result DTO ─────────────────────────────────────────────────────────────

/// <summary>Hydrated patient profile returned to the caller (US_027, AC-001).</summary>
public sealed record PatientProfileDto
{
    public Guid   PatientId           { get; init; }
    public string DisplayName         { get; init; } = string.Empty;
    public string Initials            { get; init; } = string.Empty;
    public string Email               { get; init; } = string.Empty;
    public string DeduplicationStatus { get; init; } = "Pending";
    public bool   HasDocuments        { get; init; }

    public IReadOnlyList<ProfileItemDto> Vitals       { get; init; } = [];
    public IReadOnlyList<ProfileItemDto> Medications  { get; init; } = [];
    public IReadOnlyList<ProfileItemDto> Diagnoses    { get; init; } = [];
    public IReadOnlyList<ProfileItemDto> VisitHistory { get; init; } = [];

    public PaginationDto Pagination { get; init; } = new();
}

/// <summary>A single clinical data item inside a profile section.</summary>
public sealed record ProfileItemDto
{
    public Guid     Id                { get; init; }
    public string   Label             { get; init; } = string.Empty;
    public string   Value             { get; init; } = string.Empty;
    public bool     IsAiExtracted     { get; init; }
    public bool     IsPhiField        { get; init; }
    public string[] SourceDocumentIds { get; init; } = [];
    public double?  Confidence        { get; init; }
}

public sealed record PaginationDto
{
    public int Page      { get; init; } = 1;
    public int PageSize  { get; init; } = 20;
    public int Total     { get; init; }
}
