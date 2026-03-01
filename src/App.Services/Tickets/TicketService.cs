using AutoMapper;
using App.Core.DTOs.Shop;
using App.Core.DTOs.Ticket;
using App.Core.Interfaces;
using App.Core.Interfaces.Shop;
using App.Models.Data.Contexts;
using App.Models.Settings;
using App.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QRCoder;
using System.Text.Json;

namespace App.Services.Tickets;

public class TicketService : ITicketService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IPdfService _pdfService;
    private readonly ICompanySettingsService _companySettingsService;
    private readonly ILogger<TicketService> _logger;
    private readonly IMapper _mapper;
    private readonly IDateTime _dateTime;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICashRegisterService _cashRegisterService;

    public TicketService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IPdfService pdfService,
        ICompanySettingsService companySettingsService,
        ILogger<TicketService> logger,
        IMapper mapper,
        IDateTime dateTime,
        ICurrentUserService currentUserService,
        ICashRegisterService cashRegisterService)
    {
        _contextFactory = contextFactory;
        _pdfService = pdfService;
        _companySettingsService = companySettingsService;
        _logger = logger;
        _mapper = mapper;
        _dateTime = dateTime;
        _currentUserService = currentUserService;
        _cashRegisterService = cashRegisterService;
    }

    public async Task<byte[]> GenerateSaleTicketPdfAsync(long saleId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get sale data
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            
            var sale = await context.Sales
                .AsNoTracking()
                .Include(s => s.Customer)
                .Include(s => s.Details)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(s => s.Id == saleId, cancellationToken);

            if (sale == null)
            {
                throw new InvalidOperationException($"Sale not found with ID: {saleId}");
            }

            // Get ticket configuration
            var config = await GetTicketConfigurationAsync();
            
            // Get company timezone for date conversion
            var companyTimeZone = await _companySettingsService.GetCurrentTimeZoneAsync();
            
            // Map to DTO
            var saleDto = _mapper.Map<SaleDto>(sale);
            
            // Convert sale date from UTC to company timezone
            if (companyTimeZone != null)
            {
                try
                {
                    // Convert UTC date to company timezone
                    var convertedDate = TimeZoneInfo.ConvertTimeFromUtc(sale.SaleDate, companyTimeZone);
                    saleDto.SaleDate = convertedDate;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error converting sale date to company timezone, using local time fallback");
                    // Fallback to local time if conversion fails
                    saleDto.SaleDate = sale.SaleDate.ToLocalTime();
                }
            }
            else
            {
                // If no company timezone is configured, convert to system local time as fallback
                saleDto.SaleDate = sale.SaleDate.ToLocalTime();
            }
            
            // Generate QR Code if enabled
            string? qrCodeData = null;
            if (config.ShowQRCode)
            {
                var saleInfo = new
                {
                    SaleId = sale.Id,
                    Date = saleDto.SaleDate, // Use the converted date
                    Customer = sale.Customer.Name,
                    Total = sale.Total
                };
                
                var json = JsonSerializer.Serialize(saleInfo);
                qrCodeData = GenerateQRCode(json);
            }
            
            // Prepare the model for the view
            var ticketDto = new TicketDto<SaleDto>
            {
                Data = saleDto,
                CompanyName = config.CompanyName,
                CompanyLogoBase64 = config.ShowCompanyLogo ? config.CompanyLogoBase64 : null,
                CompanyAddress = config.CompanyAddress,
                CompanyPhone = config.CompanyPhone,
                CompanyTaxId = config.CompanyTaxId,
                ShowQRCode = config.ShowQRCode,
                ShowCompanyLogo = config.ShowCompanyLogo,
                QRCodeData = qrCodeData,
                CustomHeader = config.CustomHeader,
                CustomFooter = config.CustomFooter,
                TicketWidth = config.TicketWidth,
                Copies = config.DefaultCopies,
                TimeZone = companyTimeZone
            };
                        
            var pdfBytes = await _pdfService.GenerateThermalTicketPdfFromViewAsync(
                "/Views/Tickets/SaleTicket.cshtml", 
                ticketDto,
                config.TicketWidth,
                cancellationToken);
                
            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF ticket for sale {SaleId}", saleId);
            throw;
        }
    }

    public string GetSaleTicketUrl(long saleId)
    {
        return $"/api/tickets/sale/{saleId}";
    }

    public string GetWithdrawalTicketUrl(long movementId)
    {
        return $"/api/tickets/withdrawal/{movementId}";
    }

    public async Task<byte[]> GenerateWithdrawalTicketPdfAsync(long movementId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var movement = await context.CashRegisterMovements
                .AsNoTracking()
                .Include(m => m.CashRegister)
                    .ThenInclude(c => c.Location)
                .FirstOrDefaultAsync(m => m.Id == movementId, cancellationToken);

            if (movement == null)
                throw new InvalidOperationException($"Movement not found with ID: {movementId}");

            var config = await GetTicketConfigurationAsync();
            var companyTimeZone = await _companySettingsService.GetCurrentTimeZoneAsync();

            DateTime createdAt;
            if (companyTimeZone != null)
            {
                try { createdAt = TimeZoneInfo.ConvertTimeFromUtc(movement.CreatedAt, companyTimeZone); }
                catch { createdAt = movement.CreatedAt.ToLocalTime(); }
            }
            else
            {
                createdAt = movement.CreatedAt.ToLocalTime();
            }

            var data = new App.Core.DTOs.Shop.WithdrawalTicketDataDto
            {
                MovementId = movement.Id,
                WithdrawalNumber = movement.WithdrawalNumber,
                Amount = movement.Amount,
                Reason = movement.Reason,
                CashierName = movement.CashRegister?.CreatedBy ?? string.Empty,
                LocationName = movement.CashRegister?.Location?.Name ?? string.Empty,
                CashRegisterOpenedAt = movement.CashRegister?.OpenedAt ?? DateTime.UtcNow,
                CreatedAt = createdAt
            };

            var ticketDto = new App.Core.DTOs.Ticket.TicketDto<App.Core.DTOs.Shop.WithdrawalTicketDataDto>
            {
                Data = data,
                CompanyName = config.CompanyName,
                CompanyLogoBase64 = config.ShowCompanyLogo ? config.CompanyLogoBase64 : null,
                CompanyAddress = config.CompanyAddress,
                CompanyPhone = config.CompanyPhone,
                CompanyTaxId = config.CompanyTaxId,
                ShowQRCode = false,
                ShowCompanyLogo = config.ShowCompanyLogo,
                CustomHeader = config.CustomHeader,
                CustomFooter = null,
                TicketWidth = config.TicketWidth,
                Copies = config.DefaultCopies,
                TimeZone = companyTimeZone
            };

            var pdfBytes = await _pdfService.GenerateThermalTicketPdfFromViewAsync(
                "/Views/Tickets/WithdrawalTicket.cshtml",
                ticketDto,
                config.TicketWidth,
                cancellationToken);

            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating withdrawal ticket PDF for movement {MovementId}", movementId);
            throw;
        }
    }

    public string GetCashRegisterReportTicketUrl(long cashRegisterId)
    {
        return $"/api/tickets/cash-register/{cashRegisterId}";
    }

    public async Task<byte[]> GenerateCashRegisterReportTicketPdfAsync(long cashRegisterId, CancellationToken cancellationToken = default)
    {
        try
        {
            var reportResult = await _cashRegisterService.GetReportDataAsync(cashRegisterId);
            if (!reportResult.IsSuccess)
                throw new InvalidOperationException($"Cash register report not found: {reportResult.Error}");

            var config = await GetTicketConfigurationAsync();
            var companyTimeZone = await _companySettingsService.GetCurrentTimeZoneAsync();

            var ticketDto = new App.Core.DTOs.Ticket.TicketDto<App.Core.DTOs.Shop.CashRegisterReportDto>
            {
                Data = reportResult.Value,
                CompanyName = config.CompanyName,
                CompanyLogoBase64 = config.ShowCompanyLogo ? config.CompanyLogoBase64 : null,
                CompanyAddress = config.CompanyAddress,
                CompanyPhone = config.CompanyPhone,
                CompanyTaxId = config.CompanyTaxId,
                ShowQRCode = false,
                ShowCompanyLogo = config.ShowCompanyLogo,
                CustomHeader = config.CustomHeader,
                CustomFooter = null,
                TicketWidth = config.TicketWidth,
                Copies = config.DefaultCopies,
                TimeZone = companyTimeZone
            };

            return await _pdfService.GenerateThermalTicketPdfFromViewAsync(
                "/Views/Tickets/CashRegisterReportTicket.cshtml",
                ticketDto,
                config.TicketWidth,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating cash register report ticket PDF for register {CashRegisterId}", cashRegisterId);
            throw;
        }
    }

    public string GetCashRegisterReportLetterUrl(long cashRegisterId)
    {
        return $"/api/tickets/cash-register/{cashRegisterId}/letter";
    }

    public async Task<byte[]> GenerateCashRegisterReportLetterPdfAsync(long cashRegisterId, CancellationToken cancellationToken = default)
    {
        try
        {
            var reportResult = await _cashRegisterService.GetReportDataAsync(cashRegisterId);
            if (!reportResult.IsSuccess)
                throw new InvalidOperationException($"Cash register report not found: {reportResult.Error}");

            var config = await GetTicketConfigurationAsync();
            var companyTimeZone = await _companySettingsService.GetCurrentTimeZoneAsync();

            var ticketDto = new App.Core.DTOs.Ticket.TicketDto<App.Core.DTOs.Shop.CashRegisterReportDto>
            {
                Data = reportResult.Value,
                CompanyName = config.CompanyName,
                CompanyLogoBase64 = config.ShowCompanyLogo ? config.CompanyLogoBase64 : null,
                CompanyAddress = config.CompanyAddress,
                CompanyPhone = config.CompanyPhone,
                CompanyTaxId = config.CompanyTaxId,
                ShowQRCode = false,
                ShowCompanyLogo = config.ShowCompanyLogo,
                CustomHeader = config.CustomHeader,
                CustomFooter = null,
                TicketWidth = config.TicketWidth,
                Copies = config.DefaultCopies,
                TimeZone = companyTimeZone
            };

            return await _pdfService.GeneratePdfFromViewAsync(
                "/Views/Tickets/CashRegisterReportLetter.cshtml",
                ticketDto,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating cash register report letter PDF for register {CashRegisterId}", cashRegisterId);
            throw;
        }
    }

    public async Task<TicketConfigurationDto> GetTicketConfigurationAsync()
    {
        try
        {
            TicketConfigurationDto result;
            
            await using var context = await _contextFactory.CreateDbContextAsync();
            
            // Buscar configuración existente
            var config = await context.TicketConfigurations
                .AsNoTracking()
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync();

            // Intentar mapear la configuración
            var map = config != null ? _mapper.Map<TicketConfigurationDto>(config) : null;
            
            if (map != null)
            {
                // Crear una nueva instancia con los valores mapeados
                result = new TicketConfigurationDto()
                {
                    CompanyName = map.CompanyName,
                    CompanyAddress = map.CompanyAddress,
                    CompanyPhone = map.CompanyPhone,
                    CompanyTaxId = map.CompanyTaxId,
                    ShowQRCode = map.ShowQRCode,
                    ShowCompanyLogo = map.ShowCompanyLogo,
                    CustomHeader = map.CustomHeader,
                    CustomFooter = map.CustomFooter,
                    TicketWidth = map.TicketWidth,
                    DefaultCopies = map.DefaultCopies,
                    CompanyLogoBase64 = map.CompanyLogoBase64,
                    DirectPrintEnabled = map.DirectPrintEnabled
                };
            }
            else
            {
                // Crear configuración por defecto con datos de la empresa
                var companySettings = await _companySettingsService.GetSettingsAsync();
                
                result = new TicketConfigurationDto
                {
                    CompanyName = companySettings?.CompanyName ?? "App",                    
                    ShowQRCode = true,
                    ShowCompanyLogo = true,
                    CustomFooter = "¡Gracias por su compra!",
                    TicketWidth = 80,
                    DefaultCopies = 1
                };
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo configuración de tickets");
            throw;
        }
    }

    public async Task<TicketConfigurationDto> UpdateTicketConfigurationAsync(UpdateTicketConfigurationDto updateDto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            
            // Buscar configuración existente o crear una nueva
            var config = await context.TicketConfigurations
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync();

            if (config == null)
            {
                // Crear nueva configuración
                config = new TicketConfiguration();
                config.CreatedBy = _currentUserService.FullName;
                context.TicketConfigurations.Add(config);
            }
            else
            {
                // Actualizar la fecha de modificación
                config.ModifiedBy = _currentUserService.FullName;                
            }
            
            // Actualizar propiedades (solo las que no son null)
            if (updateDto.CompanyName != null) 
                config.CompanyName = updateDto.CompanyName;
                
            if (updateDto.CompanyLogoBase64 != null) 
                config.CompanyLogoBase64 = updateDto.CompanyLogoBase64;
                
            if (updateDto.CompanyAddress != null) 
                config.CompanyAddress = updateDto.CompanyAddress;
                
            if (updateDto.CompanyPhone != null) 
                config.CompanyPhone = updateDto.CompanyPhone;
                
            if (updateDto.CompanyTaxId != null) 
                config.CompanyTaxId = updateDto.CompanyTaxId;
                
            if (updateDto.ShowQRCode.HasValue) 
                config.ShowQRCode = updateDto.ShowQRCode.Value;
                
            if (updateDto.ShowCompanyLogo.HasValue) 
                config.ShowCompanyLogo = updateDto.ShowCompanyLogo.Value;
                
            if (updateDto.CustomHeader != null) 
                config.CustomHeader = updateDto.CustomHeader;
                
            if (updateDto.CustomFooter != null) 
                config.CustomFooter = updateDto.CustomFooter;
                
            if (updateDto.TicketWidth.HasValue) 
                config.TicketWidth = updateDto.TicketWidth.Value;
                
            if (updateDto.DefaultCopies.HasValue)
                config.DefaultCopies = updateDto.DefaultCopies.Value;

            if (updateDto.DirectPrintEnabled.HasValue)
                config.DirectPrintEnabled = updateDto.DirectPrintEnabled.Value;

            await context.SaveChangesAsync();
            
            return _mapper.Map<TicketConfigurationDto>(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando configuración de tickets");
            throw;
        }
    }

    private string GenerateQRCode(string content)
    {
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(5);
            return Convert.ToBase64String(qrCodeBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando código QR");
            return string.Empty;
        }
    }
}