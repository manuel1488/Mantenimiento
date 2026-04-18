using App.Core.DTOs.Settings;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Settings;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Settings;

public class QuotationSettingsService : IQuotationSettingsService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly ILogger<QuotationSettingsService> _logger;

    public QuotationSettingsService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ICurrentUserService currentUserService,
        IDateTime dateTime,
        ILogger<QuotationSettingsService> logger)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
        _logger = logger;
    }

    public async Task<QuotationSettingsDto> GetSettingsAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var settings = await context.QuotationSettings.AsNoTracking().FirstOrDefaultAsync();
            return settings == null ? new QuotationSettingsDto() : MapToDto(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quotation settings");
            throw;
        }
    }

    public async Task<QuotationSettingsDto> SaveSettingsAsync(QuotationSettingsDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var settings = await context.QuotationSettings.FirstOrDefaultAsync();
            var now = _dateTime.Now;
            var user = _currentUserService.UserId ?? "System";

            if (settings == null)
            {
                settings = new QuotationSettings { CreatedBy = user, CreatedAt = now };
                context.QuotationSettings.Add(settings);
            }
            else
            {
                settings.ModifiedBy = user;
                settings.ModifiedAt = now;
            }

            MapFromDto(dto, settings);

            // Force all columns into the UPDATE — the EF Core change tracker can miss
            // transitions to null on large string columns (HtmlBody, CustomCss).
            if (settings.Id != 0)
                context.Entry(settings).State = Microsoft.EntityFrameworkCore.EntityState.Modified;

            await context.SaveChangesAsync();

            return MapToDto(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving quotation settings");
            throw;
        }
    }

    private static QuotationSettingsDto MapToDto(QuotationSettings s) => new()
    {
        Id = s.Id,
        PaymentTermsText = s.PaymentTermsText,
        ShowBankDetails = s.ShowBankDetails,
        BankBeneficiary = s.BankBeneficiary,
        BankRfc = s.BankRfc,
        BankName = s.BankName,
        BankAccountNumber = s.BankAccountNumber,
        BankClabeNumber = s.BankClabeNumber,
        BankSwift = s.BankSwift,
        ShowContactInfo = s.ShowContactInfo,
        ContactWebsite = s.ContactWebsite,
        ContactFacebook = s.ContactFacebook,
        ContactInstagram = s.ContactInstagram,
        ContactWhatsapp = s.ContactWhatsapp,
        ContactPhone = s.ContactPhone,
        ContactEmail = s.ContactEmail,
        HtmlBody = s.HtmlBody,
        CustomCss = s.CustomCss
    };

    private static void MapFromDto(QuotationSettingsDto dto, QuotationSettings s)
    {
        s.PaymentTermsText = dto.PaymentTermsText;
        s.ShowBankDetails = dto.ShowBankDetails;
        s.BankBeneficiary = dto.BankBeneficiary;
        s.BankRfc = dto.BankRfc;
        s.BankName = dto.BankName;
        s.BankAccountNumber = dto.BankAccountNumber;
        s.BankClabeNumber = dto.BankClabeNumber;
        s.BankSwift = dto.BankSwift;
        s.ShowContactInfo = dto.ShowContactInfo;
        s.ContactWebsite = dto.ContactWebsite;
        s.ContactFacebook = dto.ContactFacebook;
        s.ContactInstagram = dto.ContactInstagram;
        s.ContactWhatsapp = dto.ContactWhatsapp;
        s.ContactPhone = dto.ContactPhone;
        s.ContactEmail = dto.ContactEmail;
        s.HtmlBody = dto.HtmlBody;
        s.CustomCss = dto.CustomCss;
    }
}
