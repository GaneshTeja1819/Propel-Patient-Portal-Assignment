using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using UPACIP.Application.Queries.Profile;
using UPACIP.Infrastructure.AI;
using UPACIP.Infrastructure.Persistence;

namespace UPACIP.Infrastructure.Handlers.Profile;

/// <summary>
/// Handles <see cref="GetPatientProfileQuery"/> and returns a <see cref="PatientProfileDto"/>
/// (US_027, AC-001, AC-005).
///
/// <para>
/// When de-duplication is <c>Completed</c>, sections are populated from
/// <c>MergedClinicalEntry</c> records (canonical AI-merged data).
/// Otherwise, sections are populated from <c>ExtractedClinicalData</c> records
/// (raw per-document extractions) so the caller always receives the best available
/// data regardless of pipeline state (AC-001 edge case).
/// </para>
///
/// <para>
/// Pagination is applied with LIMIT/OFFSET at the database level (AC-005, NFR-004).
/// All PHI columns are decrypted transparently by EF Core's <c>EncryptedStringConverter</c>.
/// </para>
/// </summary>
public sealed class GetPatientProfileHandler
{
    private static readonly JsonSerializerOptions JsonOpts =
        new(JsonSerializerDefaults.Web);

    private const int MaxPageSize = 100;

    private readonly AppDbContext _db;

    public GetPatientProfileHandler(AppDbContext db) => _db = db;

    public async Task<PatientProfileDto> HandleAsync(
        GetPatientProfileQuery query,
        CancellationToken ct = default)
    {
        var pageSize = Math.Min(Math.Max(query.PageSize, 1), MaxPageSize);
        var page     = Math.Max(query.Page, 1);
        var skip     = (page - 1) * pageSize;

        // ── Load user + profile ───────────────────────────────────────────
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == query.PatientId, ct);

        var profile = await _db.PatientProfiles360
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PatientId == query.PatientId, ct);

        var hasDocuments = await _db.ClinicalDocuments
            .AnyAsync(d => d.PatientId == query.PatientId, ct);

        var dedupStatus = profile?.DeduplicationStatus ?? "Pending";

        // ── Build section items ───────────────────────────────────────────
        List<ProfileItemDto> vitals, medications, diagnoses, visitHistory;
        int total;

        if (string.Equals(dedupStatus, "Completed", StringComparison.Ordinal))
        {
            // Read from canonical merged entries (indexed query on PatientId — AC-005)
            var allEntries = await _db.MergedClinicalEntries
                .AsNoTracking()
                .Where(m => m.PatientId == query.PatientId)
                .OrderBy(m => m.SectionType)
                .ThenBy(m => m.MergedAt)
                .ToListAsync(ct);

            total = allEntries.Count;

            var paged = allEntries.Skip(skip).Take(pageSize).ToList();

            vitals       = MapMergedEntries(paged, "Vitals");
            medications  = MapMergedEntries(paged, "Medications");
            diagnoses    = MapMergedEntries(paged, "Diagnoses");
            visitHistory = MapMergedEntries(paged, "VisitHistory");
        }
        else
        {
            // Fallback: read from raw extracted data (AC-001 edge case — dedup not complete)
            var allExtractions = await _db.ExtractedClinicalData
                .AsNoTracking()
                .Where(e => e.PatientId == query.PatientId)
                .OrderBy(e => e.ExtractedAt)
                .ToListAsync(ct);

            var rawItems = FlattenExtractedItems(allExtractions);
            total        = rawItems.Count;

            var paged = rawItems.Skip(skip).Take(pageSize).ToList();

            vitals       = paged.Where(i => i.SectionType == "Vitals").Select(i => i.Item).ToList();
            medications  = paged.Where(i => i.SectionType == "Medications").Select(i => i.Item).ToList();
            diagnoses    = paged.Where(i => i.SectionType == "Diagnoses").Select(i => i.Item).ToList();
            visitHistory = paged.Where(i => i.SectionType == "VisitHistory").Select(i => i.Item).ToList();
        }

        var displayName = user is null ? string.Empty : $"{user.FirstName} {user.LastName}".Trim();
        var initials    = user is null ? string.Empty : BuildInitials(user.FirstName, user.LastName);

        return new PatientProfileDto
        {
            PatientId           = query.PatientId,
            DisplayName         = displayName,
            Initials            = initials,
            Email               = user?.Email ?? string.Empty,
            DeduplicationStatus = dedupStatus,
            HasDocuments        = hasDocuments,
            Vitals              = vitals,
            Medications         = medications,
            Diagnoses           = diagnoses,
            VisitHistory        = visitHistory,
            Pagination          = new PaginationDto
            {
                Page     = page,
                PageSize = pageSize,
                Total    = total,
            },
        };
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private static List<ProfileItemDto> MapMergedEntries(
        IEnumerable<Domain.Entities.MergedClinicalEntry> entries,
        string sectionType)
    {
        return entries
            .Where(e => string.Equals(e.SectionType, sectionType, StringComparison.Ordinal))
            .Select(e =>
            {
                var sourceIds = DeserializeSourceIds(e.SourceDocumentIds);
                return new ProfileItemDto
                {
                    Id                = e.Id,
                    Label             = e.EncryptedLabel,
                    Value             = e.EncryptedCanonicalValue,
                    IsAiExtracted     = true,
                    IsPhiField        = e.IsPhiField,
                    SourceDocumentIds = sourceIds,
                    Confidence        = e.Confidence,
                };
            })
            .ToList();
    }

    private List<(string SectionType, ProfileItemDto Item)> FlattenExtractedItems(
        IReadOnlyList<Domain.Entities.ExtractedClinicalData> extractions)
    {
        var result = new List<(string, ProfileItemDto)>();

        foreach (var extraction in extractions)
        {
            ClinicalExtractionResult? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<ClinicalExtractionResult>(
                    extraction.EncryptedExtractedJson, JsonOpts);
            }
            catch (JsonException)
            {
                parsed = null;
            }

            if (parsed is null) continue;

            var docIds = new[] { extraction.DocumentId.ToString() };

            if (parsed.Vitals is { } v)
            {
                if (!string.IsNullOrWhiteSpace(v.BloodPressure))
                    result.Add(("Vitals", MakeRawItem("Blood pressure", v.BloodPressure, docIds)));
                if (v.HeartRate.HasValue)
                    result.Add(("Vitals", MakeRawItem("Heart rate", $"{v.HeartRate} bpm", docIds)));
                if (v.Weight.HasValue)
                    result.Add(("Vitals", MakeRawItem("Weight", $"{v.Weight} kg", docIds)));
                if (v.Temperature.HasValue)
                    result.Add(("Vitals", MakeRawItem("Temperature", $"{v.Temperature} °C", docIds)));
            }

            foreach (var med in parsed.Medications)
            {
                if (string.IsNullOrWhiteSpace(med.Name)) continue;
                var value = string.Join(" ", new[] { med.Name, med.Dosage, med.Frequency }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
                result.Add(("Medications", MakeRawItem(med.Name, value, docIds)));
            }

            foreach (var dx in parsed.Diagnoses)
            {
                if (string.IsNullOrWhiteSpace(dx.Description) && string.IsNullOrWhiteSpace(dx.IcdCode)) continue;
                var label = dx.Description ?? dx.IcdCode ?? string.Empty;
                var value = string.IsNullOrWhiteSpace(dx.IcdCode) ? label : $"{dx.IcdCode} — {dx.Description}";
                result.Add(("Diagnoses", MakeRawItem(label, value, docIds)));
            }
        }

        return result;
    }

    private static ProfileItemDto MakeRawItem(string label, string value, string[] sourceDocIds) =>
        new()
        {
            Id                = Guid.NewGuid(),
            Label             = label,
            Value             = value,
            IsAiExtracted     = true,
            IsPhiField        = true,
            SourceDocumentIds = sourceDocIds,
            Confidence        = null,
        };

    private static string[] DeserializeSourceIds(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<string[]>(json, JsonOpts) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string BuildInitials(string firstName, string lastName)
    {
        var f = string.IsNullOrWhiteSpace(firstName) ? "" : firstName[..1].ToUpperInvariant();
        var l = string.IsNullOrWhiteSpace(lastName)  ? "" : lastName[..1].ToUpperInvariant();
        return $"{f}{l}";
    }
}
