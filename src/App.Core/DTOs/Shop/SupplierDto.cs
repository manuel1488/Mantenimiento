using App.Core.DTOs.Shared;

namespace App.Core.DTOs.Shop;

public class SupplierDto : AuditableDto
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string? LegalName { get; set; }
    public string? TaxId { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Street { get; set; }
    public string? ExteriorNumber { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string CountryCode { get; set; } = null!;
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}
