using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Inventory;

public class CreateInventoryAdjustmentDto
{
    [Required]
    public long ProductId { get; set; }

    [Required]
    public int LocationId { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "New quantity must be greater than or equal to 0")]
    public decimal NewQuantity { get; set; }
    
    [Required]
    [StringLength(20)]
    public string AdjustmentType { get; set; } = null!;
    
    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = null!;
    
    [StringLength(50)]
    public string? Reference { get; set; }
    
    public DateTime? AdjustmentDate { get; set; }
}