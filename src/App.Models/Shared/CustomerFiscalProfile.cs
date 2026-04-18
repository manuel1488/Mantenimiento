using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;

namespace App.Models.Shared;

[Table("shd_customer_fiscal_profiles")]
public class CustomerFiscalProfile : BaseEntity<long>
{
    public long CustomerId { get; set; }
    public virtual Customer Customer { get; set; } = null!;

    [Required]
    [StringLength(20)]
    public string TaxId { get; set; } = null!;

    [Required]
    [StringLength(150)]
    public string LegalName { get; set; } = null!;

    /// <summary>Fiscal email — used for invoice delivery when different from the commercial email.</summary>
    [StringLength(100)]
    [EmailAddress]
    public string? FiscalEmail { get; set; }

    // Fiscal address (may differ from commercial address)
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

    /// <summary>Fiscal postal code — used for CFDI stamping (may differ from commercial PostalCode).</summary>
    [StringLength(10)]
    public string? PostalCode { get; set; }

    // Mexico fiscal data
    [StringLength(5)]
    public string? FiscalRegime { get; set; }

    [StringLength(10)]
    public string? DefaultCfdiUse { get; set; }

    /// <summary>When true, an invoice is automatically generated after each sale for this customer.</summary>
    public bool AutoInvoice { get; set; }

    /// <summary>When true, the stamped invoice XML/PDF is automatically emailed to the customer.</summary>
    public bool SendInvoiceEmail { get; set; }

    // Canada tax numbers
    [StringLength(20)]
    public string? CaGstNumber { get; set; }

    [StringLength(20)]
    public string? CaPstNumber { get; set; }

    [StringLength(20)]
    public string? CaHstNumber { get; set; }

    [StringLength(20)]
    public string? CaQstNumber { get; set; }
}
