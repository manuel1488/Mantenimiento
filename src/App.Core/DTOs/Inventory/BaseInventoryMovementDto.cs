namespace App.Core.DTOs.Inventory;

public class BaseInventoryMovementDto
{
    public long ProductId { get; set; }
    public int WarehouseId { get; set; }
    public decimal Quantity { get; set; }
    public string? Reference { get; set; }
    public string? Document { get; set; }
    public string Reason { get; set; } = null!;
    public decimal? UnitCost { get; set; }
    public string? RelatedParty { get; set; }
    public DateTime? MovementDate { get; set; }
}