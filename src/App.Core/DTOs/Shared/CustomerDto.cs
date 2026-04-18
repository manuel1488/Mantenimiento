using App.Core.DTOs.Customer;
using App.Core.DTOs.Shared;

namespace App.Core.DTOs.Customer;

public class CustomerDto : AuditableDto
{
    public long Id { get; set; }

    // Commercial fields
    public string Name { get; set; } = null!;
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Street { get; set; }
    public string? ExteriorNumber { get; set; }
    public string? InteriorNumber { get; set; }
    public string? Neighborhood { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string CountryCode { get; set; } = null!;
    public bool IsActive { get; set; }

    // Optional fiscal profile
    public CustomerFiscalProfileDto? FiscalProfile { get; set; }

    // Convenience accessors — derived from FiscalProfile
    public bool HasFiscalData => FiscalProfile != null;
    public string? TaxId => FiscalProfile?.TaxId;
    public string? LegalName => FiscalProfile?.LegalName;
}
