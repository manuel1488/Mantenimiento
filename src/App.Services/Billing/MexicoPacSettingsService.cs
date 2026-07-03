using App.Core.Common;
using App.Core.DTOs.Billing.Mexico;
using App.Core.Interfaces.Billing;
using App.Models.Billing;
using App.Models.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Billing;

public class MexicoPacSettingsService : IMexicoPacSettingsService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<MexicoPacSettingsService> _logger;

    public MexicoPacSettingsService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<MexicoPacSettingsService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<MexicoPacSettingsDto?> GetAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var settings = await context.MexicoPacSettings.FirstOrDefaultAsync();
        if (settings == null) return null;
        return MapToDto(settings);
    }

    public async Task<Result<MexicoPacSettingsDto>> SaveAsync(UpdateMexicoPacSettingsDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var settings = await context.MexicoPacSettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new MexicoPacSettings
                {
                    CreatedBy = "System",
                    CreatedAt = DateTime.UtcNow,
                    ModifiedBy = "System",
                    ModifiedAt = DateTime.UtcNow
                };
                context.MexicoPacSettings.Add(settings);
            }
            else
            {
                settings.ModifiedBy = "System";
                settings.ModifiedAt = DateTime.UtcNow;
            }

            settings.ProviderName = dto.ProviderName;
            settings.User = dto.User;
            settings.ProductionUrl = dto.ProductionUrl;
            settings.TestUrl = dto.TestUrl;
            settings.IsProduction = dto.IsProduction;

            // Only update sensitive fields if provided
            if (!string.IsNullOrEmpty(dto.Password))
                settings.Password = dto.Password;
            if (!string.IsNullOrEmpty(dto.Token))
                settings.Token = dto.Token;
            if (!string.IsNullOrEmpty(dto.CsdCertificateBase64))
                settings.CsdCertificateBase64 = dto.CsdCertificateBase64;
            if (!string.IsNullOrEmpty(dto.CsdPrivateKeyBase64))
                settings.CsdPrivateKeyBase64 = dto.CsdPrivateKeyBase64;
            if (!string.IsNullOrEmpty(dto.CsdPassword))
                settings.CsdPassword = dto.CsdPassword;

            await context.SaveChangesAsync();

            return Result<MexicoPacSettingsDto>.Success(MapToDto(settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving PAC settings");
            return Result<MexicoPacSettingsDto>.Failure("Error al guardar la configuración PAC");
        }
    }

    public async Task<Result<MexicoPacSettingsDto>> UpdateBillingPreferencesAsync(UpdateMexicoBillingPreferencesDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var settings = await context.MexicoPacSettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new MexicoPacSettings
                {
                    ProviderName = "SW Sapien",
                    ProductionUrl = "https://services.sw.com.mx",
                    CreatedBy = "System",
                    CreatedAt = DateTime.UtcNow,
                    ModifiedBy = "System",
                    ModifiedAt = DateTime.UtcNow
                };
                context.MexicoPacSettings.Add(settings);
            }
            else
            {
                settings.ModifiedBy = "System";
                settings.ModifiedAt = DateTime.UtcNow;
            }

            settings.InvoiceSerie = dto.InvoiceSerie;
            settings.StartFolio = dto.StartFolio;
            settings.FolioLength = dto.FolioLength;
            settings.GlobalInvoiceSerie = dto.GlobalInvoiceSerie;
            settings.GlobalInvoiceStartFolio = dto.GlobalInvoiceStartFolio;
            settings.GlobalInvoiceFolioLength = dto.GlobalInvoiceFolioLength;
            settings.AutoInvoicePromptEnabled = dto.AutoInvoicePromptEnabled;
            settings.AllowEditFiscalDataInPrompt = dto.AllowEditFiscalDataInPrompt;
            settings.MultiPaymentFormPolicy = dto.MultiPaymentFormPolicy;
            settings.AllowPdfRegenerationForStampedInvoices = dto.AllowPdfRegenerationForStampedInvoices;

            await context.SaveChangesAsync();
            return Result<MexicoPacSettingsDto>.Success(MapToDto(settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving billing preferences");
            return Result<MexicoPacSettingsDto>.Failure("Error al guardar las preferencias de facturación");
        }
    }

    public async Task<Result<byte[]>> GetCsdCertificateBytesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var settings = await context.MexicoPacSettings.FirstOrDefaultAsync();
        if (settings?.CsdCertificateBase64 == null)
            return Result<byte[]>.Failure("No hay certificado CSD configurado");
        return Result<byte[]>.Success(Convert.FromBase64String(settings.CsdCertificateBase64));
    }

    public async Task<Result<byte[]>> GetCsdPrivateKeyBytesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var settings = await context.MexicoPacSettings.FirstOrDefaultAsync();
        if (settings?.CsdPrivateKeyBase64 == null)
            return Result<byte[]>.Failure("No hay llave privada CSD configurada");
        return Result<byte[]>.Success(Convert.FromBase64String(settings.CsdPrivateKeyBase64));
    }

    public async Task<Result<string>> GetCsdPasswordAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var settings = await context.MexicoPacSettings.FirstOrDefaultAsync();
        if (settings?.CsdPassword == null)
            return Result<string>.Failure("No hay contraseña CSD configurada");
        return Result<string>.Success(settings.CsdPassword);
    }

    private static MexicoPacSettingsDto MapToDto(MexicoPacSettings s) => new()
    {
        Id = s.Id,
        ProviderName = s.ProviderName,
        User = s.User,
        HasPassword = !string.IsNullOrEmpty(s.Password),
        HasToken = !string.IsNullOrEmpty(s.Token),
        ProductionUrl = s.ProductionUrl,
        TestUrl = s.TestUrl,
        IsProduction = s.IsProduction,
        InvoiceSerie = s.InvoiceSerie,
        StartFolio = s.StartFolio,
        FolioLength = s.FolioLength,
        GlobalInvoiceSerie = s.GlobalInvoiceSerie,
        GlobalInvoiceStartFolio = s.GlobalInvoiceStartFolio,
        GlobalInvoiceFolioLength = s.GlobalInvoiceFolioLength,
        HasCsdCertificate = !string.IsNullOrEmpty(s.CsdCertificateBase64),
        HasCsdPrivateKey = !string.IsNullOrEmpty(s.CsdPrivateKeyBase64),
        HasCsdPassword = !string.IsNullOrEmpty(s.CsdPassword),
        AutoInvoicePromptEnabled = s.AutoInvoicePromptEnabled,
        AllowEditFiscalDataInPrompt = s.AllowEditFiscalDataInPrompt,
        MultiPaymentFormPolicy = s.MultiPaymentFormPolicy,
        AllowPdfRegenerationForStampedInvoices = s.AllowPdfRegenerationForStampedInvoices
    };
}
