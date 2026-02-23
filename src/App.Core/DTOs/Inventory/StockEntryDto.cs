namespace App.Core.DTOs.Inventory;

public class StockEntryDto
{
    public long Id { get; set; }
    public string MovementType { get; set; } = null!;
    public string MovementSubType { get; set; } = null!;
    public int LocationId { get; set; }
    public string LocationName { get; set; } = null!;
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? DocumentNumber { get; set; }
    public string? Reference { get; set; }
    public string Reason { get; set; } = null!;
    public DateTime EntryDate { get; set; }
    public string? AttachmentFileName { get; set; }
    public string? AttachmentMimeType { get; set; }
    public List<StockEntryItemResultDto> Items { get; set; } = [];
}

public class StockEntryItemResultDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string ProductCode { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public long? InventoryMovementId { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}
