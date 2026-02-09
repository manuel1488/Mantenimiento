namespace App.Core.DTOs.Inventory;

public class CreateInventoryMovementDto : BaseInventoryMovementDto
{
    public string MovementType { get; set; } = null!;
    public string MovementSubType { get; set; } = null!;
}