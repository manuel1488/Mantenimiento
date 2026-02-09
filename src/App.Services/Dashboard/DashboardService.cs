using App.Core.DTOs.Dashboard;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IDateTime _dateTime;
    private readonly ICompanySettingsService _companySettingsService;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IDateTime dateTime,
        ICompanySettingsService companySettingsService,
        ILogger<DashboardService> logger)
    {
        _contextFactory = contextFactory;
        _dateTime = dateTime;
        _companySettingsService = companySettingsService;
        _logger = logger;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            // Obtener la zona horaria de la compañía
            var timeZone = await _companySettingsService.GetCurrentTimeZoneAsync() ?? TimeZoneInfo.Utc;

            // Convertir la fecha actual a la zona horaria de la compañía
            var currentDate = TimeZoneInfo.ConvertTimeFromUtc(_dateTime.Now, timeZone);

            // Fechas para los filtros
            var todayStart = _dateTime.ToUtc(currentDate.Date, timeZone);
            var todayEnd = _dateTime.ToUtc(currentDate.Date.AddDays(1).AddTicks(-1), timeZone);

            var weekStart = _dateTime.ToUtc(currentDate.Date.AddDays(-(int)currentDate.DayOfWeek), timeZone);
            var monthStart = _dateTime.ToUtc(new DateTime(currentDate.Year, currentDate.Month, 1), timeZone);

            // Obtener ventas de hoy
            var todaySales = await context.Sales
                .Where(s => s.Status != App.Core.Enums.Shop.SaleStatus.Cancelled && s.SaleDate >= todayStart && s.SaleDate <= todayEnd)
                .ToListAsync(cancellationToken);

            // Obtener ventas de la semana
            var weekSales = await context.Sales
                .Where(s => s.Status != App.Core.Enums.Shop.SaleStatus.Cancelled && s.SaleDate >= weekStart && s.SaleDate <= todayEnd)
                .ToListAsync(cancellationToken);

            // Obtener ventas del mes
            var monthSales = await context.Sales
                .Where(s => s.Status != App.Core.Enums.Shop.SaleStatus.Cancelled && s.SaleDate >= monthStart && s.SaleDate <= todayEnd)
                .ToListAsync(cancellationToken);

            // Obtener conteo de productos con bajo stock o sin stock
            var lowStockCount = await context.Inventory
                .CountAsync(i => i.MinStock.HasValue && i.Quantity < i.MinStock.Value && i.Quantity > 0, cancellationToken);

            var outOfStockCount = await context.Inventory
                .CountAsync(i => i.Quantity == 0, cancellationToken);

            // Calcular el valor promedio de orden
            var averageOrderValue = monthSales.Any()
                ? monthSales.Average(s => s.Total)
                : 0;

            // Obtener ventas por método de pago (para el mes actual)
            var salesByPaymentMethod = monthSales
                .GroupBy(s => s.PaymentMethod)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(s => s.Total));

            // Obtener ventas por tipo (para el mes actual)
            var salesByType = monthSales
                .GroupBy(s => s.SaleType)
                .ToDictionary(
                    g => g.Key.ToString(),
                    g => g.Count());

            return new DashboardSummaryDto
            {
                TodaySales = todaySales.Sum(s => s.Total),
                TodaySalesCount = todaySales.Count(),
                WeekSales = weekSales.Sum(s => s.Total),
                WeekSalesCount = weekSales.Count(),
                MonthSales = monthSales.Sum(s => s.Total),
                MonthSalesCount = monthSales.Count(),
                LowStockCount = lowStockCount,
                OutOfStockCount = outOfStockCount,
                AverageOrderValue = averageOrderValue,
                SalesByPaymentMethod = salesByPaymentMethod,
                SalesByType = salesByType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard summary");
            throw;
        }
    }

    public async Task<IEnumerable<StockAlertDto>> GetStockAlertsAsync(
        int maxItems = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            // Obtener productos con stock bajo o sin stock
            var lowStockItems = await context.Inventory
                .Include(i => i.Product)
                .Include(i => i.Warehouse)
                .Where(i => (i.MinStock.HasValue && i.Quantity < i.MinStock.Value) || i.Quantity == 0)
                .OrderBy(i => i.Quantity)
                .Take(maxItems)
                .Select(i => new StockAlertDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    ProductCode = i.Product.Code,
                    CurrentStock = i.Quantity,
                    MinStock = i.MinStock,
                    AlertType = i.Quantity == 0 ? "OUT_OF_STOCK" : "LOW_STOCK",
                    WarehouseName = i.Warehouse.Name
                })
                .ToListAsync(cancellationToken);

            return lowStockItems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock alerts");
            throw;
        }
    }

    public async Task<IEnumerable<RecentSaleDto>> GetRecentSalesAsync(
        int maxItems = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            // Obtener las ventas más recientes
            var recentSales = await context.Sales
                .Include(s => s.Customer)
                .OrderByDescending(s => s.SaleDate)
                .Take(maxItems)
                .Select(s => new RecentSaleDto
                {
                    Id = s.Id,
                    SaleDate = s.SaleDate,
                    CustomerName = s.Customer.Name,
                    Total = s.Total,
                    Status = s.Status.ToString(),
                    SaleType = s.SaleType.ToString()
                })
                .ToListAsync(cancellationToken);

            return recentSales;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent sales");
            throw;
        }
    }

    public async Task<SalesPerformanceDto> GetSalesPerformanceAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            // Obtener la zona horaria de la compañía
            var timeZone = await _companySettingsService.GetCurrentTimeZoneAsync() ?? TimeZoneInfo.Utc;

            // Convertir la fecha actual a la zona horaria de la compañía
            var currentDate = TimeZoneInfo.ConvertTimeFromUtc(_dateTime.Now, timeZone);

            // Si no se proporcionan fechas, establecer un período predeterminado (30 días)
            if (!startDate.HasValue)
                startDate = currentDate.AddDays(-30);

            if (!endDate.HasValue)
                endDate = currentDate;

            // Convertir a UTC para consultas a la base de datos
            var utcStartDate = _dateTime.ToUtc(startDate.Value.Date, timeZone);
            var utcEndDate = _dateTime.ToUtc(endDate.Value.Date.AddDays(1).AddTicks(-1), timeZone);

            // Calcular el período anterior (mismo número de días)
            var daysDifference = (endDate.Value - startDate.Value).Days + 1;
            var previousPeriodStart = _dateTime.ToUtc(startDate.Value.AddDays(-daysDifference), timeZone);
            var previousPeriodEnd = _dateTime.ToUtc(endDate.Value.AddDays(-daysDifference), timeZone);

            // Obtener ventas del período actual
            var currentPeriodSales = await context.Sales
                .Where(s => s.Status != App.Core.Enums.Shop.SaleStatus.Cancelled && s.SaleDate >= utcStartDate && s.SaleDate <= utcEndDate)
                .ToListAsync(cancellationToken);

            // Obtener ventas del período anterior
            var previousPeriodSales = await context.Sales
                .Where(s => s.Status != App.Core.Enums.Shop.SaleStatus.Cancelled && s.SaleDate >= previousPeriodStart && s.SaleDate <= previousPeriodEnd)
                .ToListAsync(cancellationToken);

            // Calcular métricas
            var currentTotalRevenue = currentPeriodSales.Sum(s => s.Total);
            var previousTotalRevenue = previousPeriodSales.Sum(s => s.Total);

            var revenueGrowth = previousTotalRevenue > 0
                ? (currentTotalRevenue - previousTotalRevenue) / previousTotalRevenue * 100
                : 100;

            var currentTotalOrders = currentPeriodSales.Count();
            var previousTotalOrders = previousPeriodSales.Count();

            var ordersGrowth = previousTotalOrders > 0
                ? (decimal)(currentTotalOrders - previousTotalOrders) / previousTotalOrders * 100
                : 100;

            // Agrupar ventas por día
            var dailySales = currentPeriodSales
                .GroupBy(s => TimeZoneInfo.ConvertTimeFromUtc(s.SaleDate, timeZone).Date)
                .Select(g => new DailySalesDto
                {
                    Date = g.Key,
                    Amount = g.Sum(s => s.Total),
                    Count = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToList();

            // Asegurarse de que hay datos para cada día del período
            var allDates = new List<DailySalesDto>();
            for (var date = startDate.Value.Date; date <= endDate.Value.Date; date = date.AddDays(1))
            {
                var existingSales = dailySales.FirstOrDefault(d => d.Date.Date == date);

                if (existingSales != null)
                    allDates.Add(existingSales);
                else
                    allDates.Add(new DailySalesDto { Date = date, Amount = 0, Count = 0 });
            }

            return new SalesPerformanceDto
            {
                DailySales = allDates,
                TotalRevenue = currentTotalRevenue,
                PreviousPeriodRevenue = previousTotalRevenue,
                RevenueGrowth = revenueGrowth,
                TotalOrders = currentTotalOrders,
                PreviousPeriodOrders = previousTotalOrders,
                OrdersGrowth = ordersGrowth
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales performance");
            throw;
        }
    }
}