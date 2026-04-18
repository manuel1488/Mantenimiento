using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Customer;

public class UpsertCustomerFiscalProfileDto
{
    [Required]
    [StringLength(20)]
    public string TaxId { get; set; } = null!;

    [Required]
    [StringLength(150)]
    public string LegalName { get; set; } = null!;

    [EmailAddress]
    [StringLength(100)]
    public string? FiscalEmail { get; set; }

    // Fiscal address
    [StringLength(100)]
    public string? Street { get; set; }

    [StringLength(20)]
    public string? ExteriorNumber { get; set; }

    [StringLength(20)]
    public string? InteriorNumber { get; set; }

    [StringLength(100)]
    public string? Neighborhood { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? State { get; set; }

    [StringLength(10)]
    public string? PostalCode { get; set; }

    // Mexico
    [StringLength(5)]
    public string? FiscalRegime { get; set; }

    [StringLength(10)]
    public string? DefaultCfdiUse { get; set; }

    public bool AutoInvoice { get; set; }
    public bool SendInvoiceEmail { get; set; }

    // Canada
    [StringLength(20)]
    public string? CaGstNumber { get; set; }

    [StringLength(20)]
    public string? CaPstNumber { get; set; }

    [StringLength(20)]
    public string? CaHstNumber { get; set; }

    [StringLength(20)]
    public string? CaQstNumber { get; set; }
}
