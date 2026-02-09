namespace App.Core.DTOs.Inventory;

public class InitialInventoryLoadDto : BaseInventoryMovementDto
{
    public decimal? MinStock { get; set; }
    public decimal? MaxStock { get; set; }
}