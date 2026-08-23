using App.Core.DTOs.Settings;
using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Settings;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace App.Services.Settings;

public class CotizacionTemplateSettingsService : ICotizacionTemplateSettingsService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IFileProvider _fileProvider;
    private readonly ILogger<CotizacionTemplateSettingsService> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private const string DefaultTemplateHtmlPath = "CotizacionTemplates/template.html";
    private const string DefaultTemplateCssPath = "CotizacionTemplates/styles.css";

    public CotizacionTemplateSettingsService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IFileProvider fileProvider,
        ILogger<CotizacionTemplateSettingsService> logger,
        ICurrentUserService currentUserService,
        IDateTime dateTime)
    {
        _contextFactory = contextFactory;
        _fileProvider = fileProvider;
        _logger = logger;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    public async Task<CotizacionTemplateSettingsDto?> GetConfigAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var settings = await context.CotizacionTemplateSettings
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync();

        return settings == null ? null : MapToDto(settings);
    }

    public async Task<CotizacionTemplateSettingsDto> UpdateConfigAsync(UpdateCotizacionTemplateSettingsDto dto)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var settings = await context.CotizacionTemplateSettings
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync();

        var currentUser = await _currentUserService.GetUserIdAsync() ?? "System";
        var currentTime = _dateTime.Now;

        if (settings == null)
        {
            settings = new CotizacionTemplateSettings
            {
                CreatedBy = currentUser,
                CreatedAt = currentTime
            };
            context.CotizacionTemplateSettings.Add(settings);
        }

        settings.HtmlContent = dto.HtmlContent;
        settings.CssContent = dto.CssContent;
        settings.PaymentTermsText = dto.PaymentTermsText;
        settings.MostrarDatosBancarios = dto.MostrarDatosBancarios;
        settings.BancoBeneficiario = dto.BancoBeneficiario;
        settings.BancoRfc = dto.BancoRfc;
        settings.BancoNombre = dto.BancoNombre;
        settings.BancoNumeroCuenta = dto.BancoNumeroCuenta;
        settings.BancoClabe = dto.BancoClabe;
        settings.BancoSwift = dto.BancoSwift;
        settings.MostrarDireccionEnCotizacion = dto.MostrarDireccionEnCotizacion;
        settings.Direccion = dto.Direccion;
        settings.MostrarContacto = dto.MostrarContacto;
        settings.SitioWeb = dto.SitioWeb;
        settings.Telefono = dto.Telefono;
        settings.CorreoElectronico = dto.CorreoElectronico;
        settings.WhatsApp = dto.WhatsApp;
        settings.Facebook = dto.Facebook;
        settings.Instagram = dto.Instagram;

        settings.ModifiedBy = currentUser;
        settings.ModifiedAt = currentTime;

        await context.SaveChangesAsync();

        return MapToDto(settings);
    }

    public async Task ResetAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var settings = await context.CotizacionTemplateSettings
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync();

        if (settings == null) return;

        settings.DeletedBy = await _currentUserService.GetUserIdAsync();
        settings.DeletedAt = _dateTime.Now;
        context.CotizacionTemplateSettings.Remove(settings);
        await context.SaveChangesAsync();
    }

    public async Task<(string HtmlContent, string CssContent)> GetEffectiveTemplateAsync()
    {
        var dbOverride = await GetConfigAsync();
        if (dbOverride != null)
            return (dbOverride.HtmlContent, dbOverride.CssContent);

        var htmlFileInfo = _fileProvider.GetFileInfo(DefaultTemplateHtmlPath);
        var cssFileInfo = _fileProvider.GetFileInfo(DefaultTemplateCssPath);

        if (!htmlFileInfo.Exists || !cssFileInfo.Exists)
        {
            _logger.LogWarning("Default Cotización template not found at {HtmlPath} / {CssPath}", DefaultTemplateHtmlPath, DefaultTemplateCssPath);
            return (string.Empty, string.Empty);
        }

        using var htmlReader = new StreamReader(htmlFileInfo.CreateReadStream());
        using var cssReader = new StreamReader(cssFileInfo.CreateReadStream());

        var html = await htmlReader.ReadToEndAsync();
        var css = await cssReader.ReadToEndAsync();

        return (html, css);
    }

    private static CotizacionTemplateSettingsDto MapToDto(CotizacionTemplateSettings entity) => new()
    {
        Id = entity.Id,
        HtmlContent = entity.HtmlContent,
        CssContent = entity.CssContent,
        PaymentTermsText = entity.PaymentTermsText,
        MostrarDatosBancarios = entity.MostrarDatosBancarios,
        BancoBeneficiario = entity.BancoBeneficiario,
        BancoRfc = entity.BancoRfc,
        BancoNombre = entity.BancoNombre,
        BancoNumeroCuenta = entity.BancoNumeroCuenta,
        BancoClabe = entity.BancoClabe,
        BancoSwift = entity.BancoSwift,
        MostrarDireccionEnCotizacion = entity.MostrarDireccionEnCotizacion,
        Direccion = entity.Direccion,
        MostrarContacto = entity.MostrarContacto,
        SitioWeb = entity.SitioWeb,
        Telefono = entity.Telefono,
        CorreoElectronico = entity.CorreoElectronico,
        WhatsApp = entity.WhatsApp,
        Facebook = entity.Facebook,
        Instagram = entity.Instagram
    };
}
