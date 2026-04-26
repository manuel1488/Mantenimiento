using App.Core.Enums.Shop;

namespace App.Core.DTOs.Inventory;

public class ProductWarehouseStockDto
{
    public int LocationId { get; set; }
    public string LocationName { get; set; } = null!;
    public LocationType LocationType { get; set; }
    public decimal Quantity { get; set; }
    public decimal IndividualUnits { get; set; }
    public decimal? MinStock { get; set; }
    public decimal? MaxStock { get; set; }
}