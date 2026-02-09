using System.ComponentModel.DataAnnotations;

namespace App.Core.DTOs.Inventory;

public class UpdateInventoryDto
{
    [Range(0, double.MaxValue, ErrorMessage = "Min stock must be greater than or equal to 0")]
    public decimal? MinStock { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Max stock must be greater than or equal to 0")]
    public decimal? MaxStock { get; set; }
}