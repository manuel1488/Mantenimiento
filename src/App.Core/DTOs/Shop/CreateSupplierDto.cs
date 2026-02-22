using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Shop;

public class CreateSupplierDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [StringLength(150)]
    public string? LegalName { get; set; }

    [StringLength(20)]
    public string? TaxId { get; set; }

    [EmailAddress]
    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string? Street { get; set; }

    [StringLength(20)]
    public string? ExteriorNumber { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? State { get; set; }

    [StringLength(10)]
    public string? PostalCode { get; set; }

    [Required]
    [StringLength(3)]
    public string CountryCode { get; set; } = "MX";

    [StringLength(500)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
}
