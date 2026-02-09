using App.Core.Constants;

namespace App.Core.DTOs.Inventory;

public class MovementOperationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public InventoryMovementDto? Movement { get; set; }
    public InventoryAlertInfo? Alert { get; set; }

    public static MovementOperationResult Successful(InventoryMovementDto movement, InventoryAlertInfo? alert = null)
    {
        return new MovementOperationResult
        {
            Success = true,
            Movement = movement,
            Alert = alert
        };
    }

    public static MovementOperationResult Failure(string message)
    {
        return new MovementOperationResult
        {
            Success = false,
            Message = message
        };
    }
}