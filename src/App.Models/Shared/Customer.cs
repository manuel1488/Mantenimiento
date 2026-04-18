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

    /// <summary>Optional contact person name (e.g. person to address quotations/invoices to).</summary>
    [StringLength(100)]
    public string? ContactName { get; set; }

    [StringLength(100)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    // Commercial address (delivery / contact address)
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

    [Required]
    [StringLength(3)]
    public string CountryCode { get; set; } = null!;

    // Optional fiscal profile — null means customer has no fiscal data
    public virtual CustomerFiscalProfile? FiscalProfile { get; set; }
}