namespace App.Core.DTOs.Inventory;

public class CreateAdjustmentEntryItemDto
{
    public long ProductId { get; set; }
    public decimal NewQuantity { get; set; }
}
