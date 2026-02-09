namespace App.Core.Constants;

public static class InventoryMovementType
{
    // Inventory inputs
    public const string StockIn = "STOCK_IN";           // Generic input
    public const string Purchase = "PURCHASE";          // Supplier purchase
    public const string Return = "RETURN";             // Customer return

    // Inventory outputs
    public const string StockOut = "STOCK_OUT";         // Generic output
    public const string Sale = "SALE";                 // Sale
    public const string ReturnToSupplier = "RETURN_SUPPLIER"; // Return to supplier

    // Internal movements
    public const string Transfer = "TRANSFER";          // Between warehouses
    public const string Adjustment = "ADJUSTMENT";       // Inventory adjustment
    public const string InitialLoad = "INITIAL_LOAD";    // Initial load   
}