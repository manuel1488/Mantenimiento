using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Shop.CashStation;

public class CreateCashStationDto
{
    [Required]
    public int LocationId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;
}
