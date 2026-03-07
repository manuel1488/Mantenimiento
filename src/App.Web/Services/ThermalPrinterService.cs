using App.Core.Common;
using App.Core.Interfaces;
using App.Core.Interfaces.Shop;
using App.Models.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace App.Web.Services;

public class ThermalPrinterService : IThermalPrinterService
{
    private readonly IJSRuntime _js;
    private readonly ITicketService _ticketService;
    private readonly ISaleService _saleService;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ICompanySettingsService _companySettingsService;
    private readonly ILogger<ThermalPrinterService> _logger;

    public ThermalPrinterService(
        IJSRuntime js,
        ITicketService ticketService,
        ISaleService saleService,
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ICompanySettingsService companySettingsService,
        ILogger<ThermalPrinterService> logger)
    {
        _js = js;
        _ticketService = ticketService;
        _saleService = saleService;
        _contextFactory = contextFactory;
        _companySettingsService = companySettingsService;
        _logger = logger;
    }

    public async Task<bool> IsSupportedAsync()
    {
        try
        {
            return await _js.InvokeAsync<bool>("thermalPrint.isSupported");
        }
        catch
        {
            return false;
        }
    }

    public async Task<Result<string>> RequestPortAsync()
    {
        try
        {
            var result = await _js.InvokeAsync<PortRequestResult>("thermalPrint.requestPort");
            return result.Success
                ? Result<string>.Success(result.Description)
                : Result<string>.Failure("Port selection cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Port request failed");
            return Result<string>.Failure(ex.Message);
        }
    }

    public async Task<Result> PrintSaleAsync(long saleId)
    {
        try
        {
            var config = await _ticketService.GetTicketConfigurationAsync();
            if (!config.DirectPrintEnabled)
                return Result.Failure("Direct print not enabled");

            var sale = await _saleService.GetSaleByIdAsync(saleId);
            if (sale == null)
                return Result.Failure($"Sale {saleId} not found");

            var tz = await _companySettingsService.GetCurrentTimeZoneAsync();
            var saleDate = ConvertDate(sale.SaleDate, tz);

            var qrContent = config.ShowQRCode
                ? $"#{sale.Id} {saleDate:dd/MM/yyyy} {sale.Total:C}"
                : null;

            var data = new
            {
                config = new
                {
                    companyName = config.CompanyName,
                    companyAddress = config.CompanyAddress,
                    companyPhone = config.CompanyPhone,
                    companyTaxId = config.CompanyTaxId,
                    customHeader = config.CustomHeader,
                    customFooter = config.CustomFooter,
                    showQrCode = config.ShowQRCode,
                    showCompanyLogo = config.ShowCompanyLogo,
                    companyLogoBase64 = config.CompanyLogoBase64
                },
                sale = new
                {
                    id = sale.Id,
                    saleDate = saleDate.ToString("dd/MM/yyyy HH:mm"),
                    customerName = sale.CustomerName,
                    items = sale.Details.Select(d => new
                    {
                        name = d.ProductName,
                        quantity = d.Quantity,
                        unitPrice = d.UnitPrice,
                        total = d.Total
                    }).ToList(),
                    subtotal = sale.Subtotal,
                    discountAmount = sale.DiscountAmount,
                    taxAmount = sale.TaxAmount,
                    roundingAmount = sale.RoundingAmount,
                    total = sale.Total,
                    payments = sale.Payments.Select(p => new
                    {
                        name = p.PaymentMethodName,
                        amount = p.Amount
                    }).ToList(),
                    qrContent
                }
            };

            var success = await _js.InvokeAsync<bool>("thermalPrint.printSale", data, config.PrintFlushDelayMs);
            return success ? Result.Success() : Result.Failure("Printer did not confirm success");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Direct print failed for sale {SaleId}", saleId);
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> PrintWithdrawalAsync(long movementId)
    {
        try
        {
            var config = await _ticketService.GetTicketConfigurationAsync();
            if (!config.DirectPrintEnabled)
                return Result.Failure("Direct print not enabled");

            await using var context = await _contextFactory.CreateDbContextAsync();
            var movement = await context.CashRegisterMovements
                .AsNoTracking()
                .Include(m => m.CashRegister)
                    .ThenInclude(c => c.Location)
                .FirstOrDefaultAsync(m => m.Id == movementId);

            if (movement == null)
                return Result.Failure($"Movement {movementId} not found");

            var tz = await _companySettingsService.GetCurrentTimeZoneAsync();
            var createdAt = ConvertDate(movement.CreatedAt, tz);

            var data = new
            {
                config = new
                {
                    companyName = config.CompanyName,
                    companyAddress = config.CompanyAddress,
                    companyPhone = config.CompanyPhone,
                    companyTaxId = config.CompanyTaxId
                },
                withdrawal = new
                {
                    movementId = movement.Id,
                    withdrawalNumber = movement.WithdrawalNumber,
                    amount = movement.Amount,
                    reason = movement.Reason,
                    cashierName = movement.CashRegister?.CreatedBy ?? string.Empty,
                    locationName = movement.CashRegister?.Location?.Name ?? string.Empty,
                    createdAt = createdAt.ToString("dd/MM/yyyy HH:mm")
                }
            };

            var success = await _js.InvokeAsync<bool>("thermalPrint.printWithdrawal", data, config.PrintFlushDelayMs);
            return success ? Result.Success() : Result.Failure("Printer did not confirm success");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Direct print failed for withdrawal {MovementId}", movementId);
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> PrintTestPageAsync()
    {
        try
        {
            var success = await _js.InvokeAsync<bool>("thermalPrint.printTest");
            return success ? Result.Success() : Result.Failure("Printer did not confirm success");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Test print failed");
            return Result.Failure(ex.Message);
        }
    }

    private static DateTime ConvertDate(DateTime utc, TimeZoneInfo? tz)
    {
        if (tz == null) return utc.ToLocalTime();
        try { return TimeZoneInfo.ConvertTimeFromUtc(utc, tz); }
        catch { return utc.ToLocalTime(); }
    }

    private record PortRequestResult(bool Success, string Description);
}
