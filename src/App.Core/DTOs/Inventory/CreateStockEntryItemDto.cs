namespace App.Core.DTOs.Inventory;

public class CreateStockEntryItemDto
{
    public long ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal? UnitCost { get; set; }
}
