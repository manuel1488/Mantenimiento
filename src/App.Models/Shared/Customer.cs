using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using App.Core.Base;

namespace App.Models.Shared;

[Table("shd_customers")]
public class Customer : BaseEntity<long>
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [StringLength(150)]
    public string? LegalName { get; set; } = null;

    [StringLength(20)]
    public string? TaxId { get; set; }

    [StringLength(100)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

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

    [StringLength(20)]
    public string? CaGstNumber { get; set; }

    [StringLength(20)]
    public string? CaPstNumber { get; set; }

    [StringLength(20)]
    public string? CaHstNumber { get; set; }

    [StringLength(20)]
    public string? CaQstNumber { get; set; } 

    [Required]
    [StringLength(3)]
    public string CountryCode { get; set; } = null!;

    // Mexico fiscal data
    [StringLength(5)]
    public string? FiscalRegime { get; set; }

    public bool HasFiscalData { get; set; }

    /// <summary>Default c_UsoCFDI code (e.g. G03) pre-filled when generating invoices for this customer.</summary>
    [StringLength(10)]
    public string? DefaultCfdiUse { get; set; }

    /// <summary>When true and HasFiscalData is set, an invoice is automatically generated after each sale.</summary>
    public bool AutoInvoice { get; set; }

    /// <summary>When true, the stamped invoice XML/PDF is automatically emailed to the customer.</summary>
    public bool SendInvoiceEmail { get; set; }
}