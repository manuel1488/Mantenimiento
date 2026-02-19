using App.Core.Constants;

namespace App.Core.DTOs.Inventory;

public class CreateInventoryTransferDto : BaseInventoryMovementDto
{
    public int DestinationLocationId { get; set; }
    public string TransferType { get; set; } = InventoryMovementSubType.StandardTransfer;
}