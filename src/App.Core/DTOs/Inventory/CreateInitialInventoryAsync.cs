namespace App.Core.DTOs.Inventory;

public class BulkInitialLoadRequestDto
{
    public int LocationId { get; set; }
    public List<BulkInventoryLoadDto> Items { get; set; } = new();
}