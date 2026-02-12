namespace App.Core.DTOs.Shop;

/// <summary>
/// DTO for exporting product wholesale discount data to Excel.
/// </summary>
public class ProductWholesaleExportDto
{
    public long ProductId { get; set; }
    public string TierName { get; set; } = null!;
    public decimal MinQuantity { get; set; }
    public decimal DiscountPercentage { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for wholesale tier column headers in export/import.
/// </summary>
public class WholesaleTierColumnDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int DisplayOrder { get; set; }
}
