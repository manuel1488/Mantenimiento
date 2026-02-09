namespace App.Core.DTOs.Shop;

/// <summary>
/// DTO for exporting product partial surcharge data to Excel.
/// </summary>
public class ProductSurchargeExportDto
{
    public long ProductId { get; set; }
    public string FractionCode { get; set; } = null!;
    public decimal SurchargePercentage { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for fraction column definitions in Excel export.
/// </summary>
public class FractionColumnDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int DisplayOrder { get; set; }
}
