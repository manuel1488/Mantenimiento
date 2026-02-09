using App.Core.DTOs.Dashboard;

namespace App.Core.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardSummaryDto> GetDashboardSummaryAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default);
            
        Task<IEnumerable<StockAlertDto>> GetStockAlertsAsync(
            int maxItems = 10,
            CancellationToken cancellationToken = default);
            
        Task<IEnumerable<RecentSaleDto>> GetRecentSalesAsync(
            int maxItems = 10,
            CancellationToken cancellationToken = default);
            
        Task<SalesPerformanceDto> GetSalesPerformanceAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default);
    }
}