using App.Core.Enums.Shop;

namespace App.Core.DTOs.Inventory;

public class WarehouseStockDto
{
    public int LocationId { get; set; }
    public string LocationName { get; set; } = null!;
    public LocationType LocationType { get; set; }
    public int TotalProducts { get; set; }
    public int ProductsWithStock { get; set; }
    public int ProductsBelowMinStock { get; set; }
    public int ProductsAboveMaxStock { get; set; }
    public List<WarehouseProductStockDto> ProductStock { get; set; } = new();

    // Backwards compatibility properties (deprecated)
    [Obsolete("Use LocationId instead")]
    public int WarehouseId => LocationId;

    [Obsolete("Use LocationName instead")]
    public string WarehouseName => LocationName;
}