using App.Core.DTOs.Inventory;

namespace App.Core.DTOs.Reports;

public class BaseReportDto<T>
{
    public IList<T> Movements { get; set; } = new List<T>();
    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Utc;
    public DateTime GeneratedAt { get; set; }
}