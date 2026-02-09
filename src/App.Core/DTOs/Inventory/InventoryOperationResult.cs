namespace App.Core.DTOs.Inventory;

public class InventoryOperationResult<T>
{
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }
    public T? Data { get; private set; }

    public static InventoryOperationResult<T> Ok(T data) => 
        new() { Success = true, Data = data };

    public static InventoryOperationResult<T> Error(string message) => 
        new() { Success = false, ErrorMessage = message };

    public static InventoryOperationResult<T> Error(string message, T data) => 
        new() { Success = false, ErrorMessage = message, Data = data };
}