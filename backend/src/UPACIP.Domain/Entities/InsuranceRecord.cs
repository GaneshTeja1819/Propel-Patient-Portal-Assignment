namespace UPACIP.Domain.Entities;

/// <summary>
/// Read-only reference data for supported insurance providers.
/// Seeded at migration time — not modifiable at runtime via API.
/// </summary>
public class InsuranceRecord : BaseEntity
{
    public string ProviderName { get; set; } = string.Empty;
    public string InsuranceIdPattern { get; set; } = string.Empty;  // Regex for member ID validation
    public string? PlanName { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; } = true;
}
