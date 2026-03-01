using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Label;

public class CreateBulkLabelJobDto
{
    [Required]
    public long ProductId { get; set; }

    [Required]
    [Range(0.001, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Required]
    [StringLength(10)]
    public string UnitMeasureCode { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal TotalPrice { get; set; }

    [Range(1, 100)]
    public int LabelCount { get; set; } = 1;

    [StringLength(50)]
    public string? BatchNumber { get; set; }

    [StringLength(200)]
    public string? Notes { get; set; }
}
