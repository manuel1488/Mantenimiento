namespace App.Core.DTOs.Inventory;

public class InventoryHistoryExportRequestDto
{
    public string? SearchString { get; set; }
    public int? LocationId { get; set; }
    public string? MovementType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int PageSize { get; set; }
}