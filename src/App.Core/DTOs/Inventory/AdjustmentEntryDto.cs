namespace App.Core.DTOs.Inventory;

public class AdjustmentEntryDto
{
    public long Id { get; set; }
    public string AdjustmentType { get; set; } = null!;
    public int LocationId { get; set; }
    public string LocationName { get; set; } = null!;
    public string? Reference { get; set; }
    public string Reason { get; set; } = null!;
    public DateTime AdjustmentDate { get; set; }
    public List<AdjustmentEntryItemResultDto> Items { get; set; } = [];
}

public class AdjustmentEntryItemResultDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string ProductCode { get; set; } = null!;
    public decimal NewQuantity { get; set; }
    public decimal PreviousQuantity { get; set; }
    public long? InventoryMovementId { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? AlertType { get; set; }
    public decimal? AlertCurrentStock { get; set; }
    public decimal? AlertThreshold { get; set; }
}
