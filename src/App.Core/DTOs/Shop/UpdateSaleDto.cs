using System.ComponentModel.DataAnnotations;
using App.Core.Enums.Shop;

namespace App.Core.DTOs.Shop;

public class UpdateSaleDto
{
    public App.Core.Enums.Shop.SaleStatus Status { get; set; }
    
    [Required]
    [StringLength(20)]
    public string PaymentMethod { get; set; } = null!;
    
    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; }
    
    public string? DiscountAuthorizedBy { get; set; }
}