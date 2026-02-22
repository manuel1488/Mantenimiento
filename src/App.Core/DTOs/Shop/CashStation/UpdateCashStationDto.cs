using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Shop.CashStation;

public class UpdateCashStationDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    public bool IsActive { get; set; }
}
