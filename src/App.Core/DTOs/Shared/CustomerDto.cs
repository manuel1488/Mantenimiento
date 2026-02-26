using App.Core.DTOs.Shared;

namespace App.Core.DTOs.Customer;

public class CustomerDto : AuditableDto
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string LegalName { get; set; } = null!;
    public string? TaxId { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Street { get; set; }
    public string? ExteriorNumber { get; set; }
    public string? InteriorNumber { get; set; }
    public string? Neighborhood { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? CaGstNumber { get; set; }
    public string? CaPstNumber { get; set; }
    public string? CaHstNumber { get; set; }
    public string? CaQstNumber { get; set; }
    public string CountryCode { get; set; } = null!;
    public bool IsActive { get; set; }

    // Mexico fiscal data
    public string? FiscalRegime { get; set; }
    public bool HasFiscalData { get; set; }
}