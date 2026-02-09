namespace App.Core.DTOs.Product;

public class ProductBulkLoadResultDto
{
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}