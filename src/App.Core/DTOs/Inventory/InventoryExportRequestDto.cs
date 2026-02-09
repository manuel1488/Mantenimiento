namespace App.Core.DTOs.Inventory;

public class InventoryExportRequestDto
{
    public string? SearchString { get; set; }
    public int? WarehouseId { get; set; }
    public bool? HasStock { get; set; }
    public bool? BelowMinStock { get; set; }
    public bool? AboveMaxStock { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? MovementType { get; set; }
    public int PageSize { get; set; }
}