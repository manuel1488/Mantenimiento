using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Shop;

public class ConsolidateRemissionsDto
{
    [Required]
    public long CustomerId { get; set; }

    [Required]
    [MinLength(1)]
    public List<long> RemissionIds { get; set; } = [];

    [Required]
    public int LocationId { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateSalePaymentDto> Payments { get; set; } = [];
}
