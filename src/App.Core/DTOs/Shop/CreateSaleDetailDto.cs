using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Shop;

public class CreateSaleDetailDto
{
    [Required]
    public long ProductId { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Quantity { get; set; }
    
    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; } = 0;
    public bool IsDiscountAuthorized { get; set; }
    public string? DiscountAuthorizedBy { get; set; }
    public string? DiscountAuthorizerId { get; set; }
    public DateTime? DiscountAuthorizedAt { get; set; }
    public bool IsCustomPrice { get; set; } = false;

    /// <summary>
    /// Selected partial sale fraction ID (for fractional sales).
    /// </summary>
    public int? PartialSaleFractionId { get; set; }
}