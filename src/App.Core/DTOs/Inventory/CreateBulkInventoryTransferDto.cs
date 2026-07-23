using App.Core.Constants;

namespace App.Core.DTOs.Inventory;

public class BulkTransferLineDto
{
    public long ProductId { get; set; }
    public decimal Quantity { get; set; }
}

public class CreateBulkInventoryTransferDto
{
    public int LocationId { get; set; }
    public int DestinationLocationId { get; set; }
    public string TransferType { get; set; } = InventoryMovementSubType.StandardTransfer;
    public string Reason { get; set; } = null!;
    public string? Reference { get; set; }
    public List<BulkTransferLineDto> Lines { get; set; } = new();
}
