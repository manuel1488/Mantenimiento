namespace App.Core.DTOs.Customer;

public class CustomerFiscalProfileDto
{
    public long Id { get; set; }
    public string TaxId { get; set; } = null!;
    public string LegalName { get; set; } = null!;
    public string? FiscalEmail { get; set; }

    // Fiscal address
    public string? Street { get; set; }
    public string? ExteriorNumber { get; set; }
    public string? InteriorNumber { get; set; }
    public string? Neighborhood { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }

    // Mexico
    public string? FiscalRegime { get; set; }
    public string? DefaultCfdiUse { get; set; }
    public bool AutoInvoice { get; set; }
    public bool SendInvoiceEmail { get; set; }

    // Canada
    public string? CaGstNumber { get; set; }
    public string? CaPstNumber { get; set; }
    public string? CaHstNumber { get; set; }
    public string? CaQstNumber { get; set; }
}
