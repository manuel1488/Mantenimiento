namespace App.Core.DTOs.Shop;

public class CloneQuotationResultDto
{
    public QuotationDto Quotation { get; set; } = null!;
    public List<string> SkippedProducts { get; set; } = [];
}
