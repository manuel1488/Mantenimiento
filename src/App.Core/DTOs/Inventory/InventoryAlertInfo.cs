using App.Core.Constants;

public class InventoryAlertInfo
{
    public string AlertType { get; set; } = null!;
    public decimal CurrentStock { get; set; }
    public decimal? Threshold { get; set; }  // MinStock o MaxStock depending on AlertType
    public string ProductName { get; set; } = null!;
    public string LocationName { get; set; } = null!;

    public static InventoryAlertInfo LowStock(string productName, string locationName, decimal currentStock, decimal minStock)
    {
        return new InventoryAlertInfo
        {
            AlertType = InventoryAlertType.LowStock,
            ProductName = productName,
            LocationName = locationName,
            CurrentStock = currentStock,
            Threshold = minStock
        };
    }

    public static InventoryAlertInfo OverStock(string productName, string locationName, decimal currentStock, decimal maxStock)
    {
        return new InventoryAlertInfo
        {
            AlertType = InventoryAlertType.OverStock,
            ProductName = productName,
            LocationName = locationName,
            CurrentStock = currentStock,
            Threshold = maxStock
        };
    }
}