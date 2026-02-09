namespace App.Core.DTOs.Product;

public class BulkProductLoadRequestDto
{
    public List<ProductBulkLoadDto> Items { get; set; } = new();
}