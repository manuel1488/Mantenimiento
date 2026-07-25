namespace App.Core.DTOs.Inventory;

public class PhysicalCountPdfLineDto
{
    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public decimal SystemQuantity { get; set; }
    public decimal CountedQuantity { get; set; }
    public decimal Difference { get; set; }
}

public class PhysicalCountPdfDto
{
    public string CompanyName { get; set; } = null!;
    public string LogoBase64 { get; set; } = string.Empty;
    public Guid BatchId { get; set; }
    public string BatchNumber { get; set; } = null!;
    public string LocationName { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public string? Reference { get; set; }
    public DateTime CountDate { get; set; }
    public string CreatedBy { get; set; } = null!;
    public List<PhysicalCountPdfLineDto> Lines { get; set; } = new();
}
