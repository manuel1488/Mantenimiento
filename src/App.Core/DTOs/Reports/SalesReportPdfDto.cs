using App.Core.DTOs.Shop;

namespace App.Core.DTOs.Reports;

public class SalesReportPdfDto
{
    public SalesSummaryDto Summary { get; set; } = null!;
    public IList<SaleDto> Sales { get; set; } = new List<SaleDto>();
    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Utc;
    public DateTime GeneratedAt { get; set; }
    public string? LogoBase64 { get; set; }
}