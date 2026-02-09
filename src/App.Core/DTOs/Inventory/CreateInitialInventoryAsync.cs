namespace App.Core.DTOs.Inventory;

public class BulkInitialLoadRequestDto
{
    public int WarehouseId { get; set; }
    public List<BulkInventoryLoadDto> Items { get; set; } = new();
}