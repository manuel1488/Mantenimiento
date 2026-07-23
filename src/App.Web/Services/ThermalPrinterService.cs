using App.Core.Common;
using App.Core.Interfaces;
using App.Core.Interfaces.Shop;
using App.Models.Data.Contexts;
using App.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;

namespace App.Web.Services;

public class ThermalPrinterService : IThermalPrinterService
{
    private readonly IJSRuntime _js;
    private readonly ITicketService _ticketService;
    private readonly ISaleService _saleService;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ICompanySettingsService _companySettingsService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ThermalPrinterService> _logger;
    private readonly IStringLocalizer<ThermalPrinterService> _localizer;

    public ThermalPrinterService(
        IJSRuntime js,
        ITicketService ticketService,
        ISaleService saleService,
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ICompanySettingsService companySettingsService,
        ICurrentUserService currentUser,
        ILogger<ThermalPrinterService> logger,
        IStringLocalizer<ThermalPrinterService> localizer)
    {
        _js = js;
        _ticketService = ticketService;
        _saleService = saleService;
        _contextFactory = contextFactory;
        _companySettingsService = companySettingsService;
        _currentUser = currentUser;
        _logger = logger;
        _localizer = localizer;
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
            return Result<string>.Failure(_localizer["Port selection cancelled"]);
        }
    }

    public async Task<Result> PrintSaleAsync(long saleId)
    {
        try
        {
            var config = await _ticketService.GetTicketConfigurationAsync();
            if (!config.DirectPrintEnabled)
                return Result.Failure(_localizer["Direct print not enabled"]);

            var sale = await _saleService.GetSaleByIdAsync(saleId);
            if (sale == null)
                return Result.Failure(_localizer["Sale not found"]);

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

            var printResult = await _js.InvokeAsync<DirectPrintResult?>(
                "thermalPrint.printSale", data, config.PrintFlushDelayMs, config.PrintChunkSize, config.PortSettlingDelayMs);
            if (printResult == null)
                return Result.Failure(_localizer["No response from printer — refresh the page and try again"]);
            await LogPrintResult("Sale", saleId, printResult);

            if (!printResult.Success)
                return Result.Failure(_localizer["Printer did not confirm success"]);

            if (config.CashDrawerEnabled && !string.IsNullOrWhiteSpace(config.CashDrawerCommand))
            {
                try
                {
                    await _js.InvokeAsync<bool>("thermalPrint.openDrawer", config.CashDrawerCommand, config.PrintFlushDelayMs);
                }
                catch (Exception drawerEx)
                {
                    _logger.LogWarning(drawerEx, "Cash drawer open failed after sale {SaleId}", saleId);
                }
            }

            return Result.Success();
        }
        catch (JSDisconnectedException)
        {
            return Result.Failure("circuit-disconnected");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Direct print failed for sale {SaleId} by user {User}", saleId, await _currentUser.GetUserNameAsync() ?? await _currentUser.GetUserIdAsync());
            return Result.Failure(_localizer["Print failed — check printer connection and try again"]);
        }
    }

    public async Task<Result> PrintWithdrawalAsync(long movementId)
    {
        try
        {
            var config = await _ticketService.GetTicketConfigurationAsync();
            if (!config.DirectPrintEnabled)
                return Result.Failure(_localizer["Direct print not enabled"]);

            await using var context = await _contextFactory.CreateDbContextAsync();
            var movement = await context.CashRegisterMovements
                .AsNoTracking()
                .Include(m => m.CashRegister)
                    .ThenInclude(c => c.Location)
                .FirstOrDefaultAsync(m => m.Id == movementId);

            if (movement == null)
                return Result.Failure(_localizer["Movement not found"]);

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

            var printResult = await _js.InvokeAsync<DirectPrintResult?>(
                "thermalPrint.printWithdrawal", data, config.PrintFlushDelayMs, config.PrintChunkSize, config.PortSettlingDelayMs);
            if (printResult == null)
                return Result.Failure(_localizer["No response from printer — refresh the page and try again"]);
            await LogPrintResult("Withdrawal", movementId, printResult);

            return printResult.Success
                ? Result.Success()
                : Result.Failure(_localizer["Printer did not confirm success"]);
        }
        catch (JSDisconnectedException)
        {
            return Result.Failure("circuit-disconnected");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Direct print failed for withdrawal {MovementId} by user {User}", movementId, await _currentUser.GetUserNameAsync() ?? await _currentUser.GetUserIdAsync());
            return Result.Failure(_localizer["Print failed — check printer connection and try again"]);
        }
    }

    public async Task<Result> PrintTestPageAsync()
    {
        try
        {
            var config = await _ticketService.GetTicketConfigurationAsync();
            var printResult = await _js.InvokeAsync<DirectPrintResult?>("thermalPrint.printTest", config.PortSettlingDelayMs);
            if (printResult == null)
                return Result.Failure(_localizer["No response from printer — refresh the page and try again"]);
            await LogPrintResult("TestPage", 0, printResult);

            return printResult.Success
                ? Result.Success()
                : Result.Failure(_localizer["Printer did not confirm success"]);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Test print failed by user {User}", await _currentUser.GetUserNameAsync() ?? await _currentUser.GetUserIdAsync());
            return Result.Failure(_localizer["Print failed — check printer connection and try again"]);
        }
    }

    public async Task<Result> OpenCashDrawerAsync()
    {
        try
        {
            var config = await _ticketService.GetTicketConfigurationAsync();
            if (!config.CashDrawerEnabled)
                return Result.Failure(_localizer["Cash drawer not enabled"]);
            if (string.IsNullOrWhiteSpace(config.CashDrawerCommand))
                return Result.Failure(_localizer["Cash drawer command not configured"]);

            var success = await _js.InvokeAsync<bool>("thermalPrint.openDrawer", config.CashDrawerCommand, config.PrintFlushDelayMs);
            return success ? Result.Success() : Result.Failure(_localizer["Drawer did not respond"]);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cash drawer open failed");
            return Result.Failure(ex.Message);
        }
    }

    private async Task LogPrintResult(string operation, long entityId, DirectPrintResult? result)
    {
        if (result == null) return;
        if (result.Success)
        {
            _logger.LogInformation(
                "Direct print {Operation} #{EntityId}: {BytesSent} bytes, paper: {PaperStatus}",
                operation, entityId, result.BytesSent, result.PaperStatus ?? "unknown");

            if (result.PaperStatus == "near-end")
                _logger.LogWarning("Printer paper near end — replace soon");
            else if (result.PaperStatus == "empty")
                _logger.LogWarning("Printer paper empty");
        }
        else
        {
            _logger.LogWarning(
                "Direct print {Operation} #{EntityId} failed: {Error} ({BytesSent} bytes sent) portFresh={PortFresh} DSR={Dsr} CTS={Cts} — user {User}",
                operation, entityId, result.Error ?? "unknown", result.BytesSent,
                result.PortFresh, result.Dsr, result.Cts,
                await _currentUser.GetUserNameAsync() ?? await _currentUser.GetUserIdAsync());
        }
    }

    private static DateTime ConvertDate(DateTime utc, TimeZoneInfo? tz)
    {
        if (tz == null) return utc.ToLocalTime();
        try { return TimeZoneInfo.ConvertTimeFromUtc(utc, tz); }
        catch { return utc.ToLocalTime(); }
    }

    private record PortRequestResult(bool Success, string Description);
    private record DirectPrintResult(bool Success, int BytesSent, string? PaperStatus, string? Error, bool? PortFresh, bool? Dsr, bool? Cts);
}
