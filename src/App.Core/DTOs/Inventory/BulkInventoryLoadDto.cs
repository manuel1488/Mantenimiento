namespace App.Core.DTOs.Inventory;

public class BulkInventoryLoadDto : BaseInventoryMovementDto
{
    public string ProductCode { get; set; } = null!;
    public decimal? MinStock { get; set; }
    public decimal? MaxStock { get; set; }
}