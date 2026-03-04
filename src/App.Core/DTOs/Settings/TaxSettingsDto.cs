namespace App.Core.DTOs.Settings;

public class TaxSettingsDto
{
    public int Id { get; set; }
    public string CountryCode { get; set; } = null!;
    public string BusinessName { get; set; } = null!;
    public string TaxId { get; set; } = null!;
    public string FiscalRegime { get; set; } = null!;
    public string? PostalCode { get; set; }
    public string? Address { get; set; }
    
    // Mexico
    public string? MxDefaultCfdiUse { get; set; }
    public string? MxDefaultPaymentMethod { get; set; }
    public string? MxDefaultPaymentType { get; set; }
    
    // Canada
    public string? CaGstNumber { get; set; }
    public string? CaPstNumber { get; set; }
    public string? CaHstNumber { get; set; }
    public string? CaQstNumber { get; set; }
    
    // Postal code timezone info (read-only, populated from CFDI catalog)
    public string? PostalCodeTimeZoneName { get; set; }
    public string? PostalCodeIanaTimeZoneId { get; set; }
    public int? PostalCodeOffsetWinter { get; set; }
    public int? PostalCodeOffsetSummer { get; set; }

    public string CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}