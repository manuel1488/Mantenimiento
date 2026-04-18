using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Customer;

public class CreateCustomerDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [StringLength(100)]
    public string? ContactName { get; set; }

    [EmailAddress]
    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    // Commercial address
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

    // Optional fiscal profile — provide to create fiscal data along with the customer
    public UpsertCustomerFiscalProfileDto? FiscalProfile { get; set; }
}
