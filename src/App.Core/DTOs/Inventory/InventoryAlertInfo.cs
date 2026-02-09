using App.Core.Constants;

public class InventoryAlertInfo
{
    public string AlertType { get; set; } = null!;
    public decimal CurrentStock { get; set; }
    public decimal? Threshold { get; set; }  // MinStock o MaxStock depending on AlertType
    public string ProductName { get; set; } = null!;
    public string WarehouseName { get; set; } = null!;

    public static InventoryAlertInfo LowStock(string productName, string warehouseName, decimal currentStock, decimal minStock)
    {
        return new InventoryAlertInfo
        {
            AlertType = InventoryAlertType.LowStock,
            ProductName = productName,
            WarehouseName = warehouseName,
            CurrentStock = currentStock,
            Threshold = minStock
        };
    }

    public static InventoryAlertInfo OverStock(string productName, string warehouseName, decimal currentStock, decimal maxStock)
    {
        return new InventoryAlertInfo
        {
            AlertType = InventoryAlertType.OverStock,
            ProductName = productName,
            WarehouseName = warehouseName,
            CurrentStock = currentStock,
            Threshold = maxStock
        };
    }
}