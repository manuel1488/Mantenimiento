using AutoMapper;

using App.Core.Common;
using App.Core.DTOs.Cotizaciones;
using App.Core.Enums.Cotizaciones;
using App.Core.Enums.Obras;
using App.Core.Interfaces;
using App.Core.Options;
using App.Models.Cotizaciones;
using App.Models.Data.Contexts;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace App.Services.Cotizaciones;

public class CotizacionService : ICotizacionService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<CotizacionService> _logger;
    private readonly IStringLocalizer<CotizacionService> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTimeService;
    private readonly IPdfService _pdfService;
    private readonly ICotizacionTemplateSettingsService _templateSettingsService;
    private readonly ICompanySettingsService _companySettingsService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IOptions<ApplicationOptions> _applicationOptions;
    private readonly IOptions<BrandingOptions> _brandingOptions;

    public CotizacionService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<CotizacionService> logger,
        IStringLocalizer<CotizacionService> localizer,
        ICurrentUserService currentUserService,
        IDateTime dateTimeService,
        IPdfService pdfService,
        ICotizacionTemplateSettingsService templateSettingsService,
        ICompanySettingsService companySettingsService,
        IEmailTemplateService emailTemplateService,
        IOptions<ApplicationOptions> applicationOptions,
        IOptions<BrandingOptions> brandingOptions)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        _localizer = localizer;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
        _pdfService = pdfService;
        _templateSettingsService = templateSettingsService;
        _companySettingsService = companySettingsService;
        _emailTemplateService = emailTemplateService;
        _applicationOptions = applicationOptions;
        _brandingOptions = brandingOptions;
    }

    public async Task<Result<CotizacionDto>> GenerarAsync(int obraId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    var obra = await context.Obras
                        .Include(o => o.Actividades).ThenInclude(a => a.Servicio).ThenInclude(s => s.UnidadMedida)
                        .FirstOrDefaultAsync(o => o.Id == obraId);

                    if (obra == null)
                    {
                        await transaction.RollbackAsync();
                        return Result<CotizacionDto>.Failure(_localizer["Obra not found"]);
                    }

                    if (obra.Estado is not (ObraEstado.Solicitada or ObraEstado.Rechazada))
                    {
                        await transaction.RollbackAsync();
                        return Result<CotizacionDto>.Failure(_localizer["A Cotizacion can only be generated for an Obra in Solicitada or Rechazada"]);
                    }

                    if (obra.Actividades.Count == 0)
                    {
                        await transaction.RollbackAsync();
                        return Result<CotizacionDto>.Failure(_localizer["The Obra must have at least one Actividad to generate a Cotizacion"]);
                    }

                    var maxVersion = await context.Cotizaciones
                        .Where(c => c.ObraId == obraId)
                        .Select(c => (int?)c.Version)
                        .MaxAsync() ?? 0;

                    var currentUser = await _currentUserService.GetUserIdAsync();
                    var currentTime = _dateTimeService.Now;

                    var cotizacion = new Cotizacion
                    {
                        ObraId = obraId,
                        Version = maxVersion + 1,
                        FechaGeneracion = currentTime,
                        Estado = CotizacionEstado.Pendiente,
                        CreatedBy = currentUser,
                        CreatedAt = currentTime,
                        ModifiedBy = currentUser,
                        ModifiedAt = currentTime
                    };

                    decimal total = 0;
                    foreach (var actividad in obra.Actividades)
                    {
                        var subtotal = actividad.Cantidad * actividad.PrecioUnitario;
                        total += subtotal;

                        cotizacion.Lineas.Add(new CotizacionLinea
                        {
                            ActividadId = actividad.Id,
                            ServicioNombre = actividad.Servicio.Nombre,
                            UnidadMedida = actividad.Servicio.UnidadMedida.Nombre,
                            Cantidad = actividad.Cantidad,
                            PrecioUnitario = actividad.PrecioUnitario,
                            Subtotal = subtotal,
                            CreatedBy = currentUser,
                            CreatedAt = currentTime,
                            ModifiedBy = currentUser,
                            ModifiedAt = currentTime
                        });
                    }

                    cotizacion.Total = total;

                    context.Cotizaciones.Add(cotizacion);

                    obra.Estado = ObraEstado.Cotizada;
                    obra.ModifiedBy = currentUser;
                    obra.ModifiedAt = currentTime;

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var created = await context.Cotizaciones
                        .AsNoTracking()
                        .Include(c => c.Lineas)
                        .FirstAsync(c => c.Id == cotizacion.Id);

                    return Result<CotizacionDto>.Success(_mapper.Map<CotizacionDto>(created));
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating cotizacion for obra {Id}", obraId);
            return Result<CotizacionDto>.Failure(_localizer["Error generating cotizacion"]);
        }
    }

    public async Task<Result<CotizacionDto>> AprobarAsync(int cotizacionId, AprobarCotizacionDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    var cotizacion = await context.Cotizaciones
                        .Include(c => c.Obra)
                        .FirstOrDefaultAsync(c => c.Id == cotizacionId);

                    if (cotizacion == null)
                    {
                        await transaction.RollbackAsync();
                        return Result<CotizacionDto>.Failure(_localizer["Cotizacion not found"]);
                    }

                    if (cotizacion.Estado != CotizacionEstado.Pendiente)
                    {
                        await transaction.RollbackAsync();
                        return Result<CotizacionDto>.Failure(_localizer["Only a Cotizacion in Pendiente can be Aprobada"]);
                    }

                    var currentTime = _dateTimeService.Now;
                    var currentUser = await _currentUserService.GetUserIdAsync();

                    cotizacion.Estado = CotizacionEstado.Aprobada;
                    cotizacion.FechaAprobacion = currentTime;
                    cotizacion.AprobadaPor = dto.AprobadaPor;
                    cotizacion.MedioAprobacion = dto.MedioAprobacion;
                    cotizacion.ModifiedBy = currentUser;
                    cotizacion.ModifiedAt = currentTime;

                    cotizacion.Obra.Estado = ObraEstado.Aprobada;
                    cotizacion.Obra.ModifiedBy = currentUser;
                    cotizacion.Obra.ModifiedAt = currentTime;

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var updated = await context.Cotizaciones
                        .AsNoTracking()
                        .Include(c => c.Lineas)
                        .FirstAsync(c => c.Id == cotizacionId);

                    return Result<CotizacionDto>.Success(_mapper.Map<CotizacionDto>(updated));
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving cotizacion {Id}", cotizacionId);
            return Result<CotizacionDto>.Failure(_localizer["Error approving cotizacion"]);
        }
    }

    public async Task<Result<CotizacionDto>> RechazarAsync(int cotizacionId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    var cotizacion = await context.Cotizaciones
                        .Include(c => c.Obra)
                        .FirstOrDefaultAsync(c => c.Id == cotizacionId);

                    if (cotizacion == null)
                    {
                        await transaction.RollbackAsync();
                        return Result<CotizacionDto>.Failure(_localizer["Cotizacion not found"]);
                    }

                    if (cotizacion.Estado != CotizacionEstado.Pendiente)
                    {
                        await transaction.RollbackAsync();
                        return Result<CotizacionDto>.Failure(_localizer["Only a Cotizacion in Pendiente can be Rechazada"]);
                    }

                    var currentTime = _dateTimeService.Now;
                    var currentUser = await _currentUserService.GetUserIdAsync();

                    cotizacion.Estado = CotizacionEstado.Rechazada;
                    cotizacion.ModifiedBy = currentUser;
                    cotizacion.ModifiedAt = currentTime;

                    cotizacion.Obra.Estado = ObraEstado.Rechazada;
                    cotizacion.Obra.ModifiedBy = currentUser;
                    cotizacion.Obra.ModifiedAt = currentTime;

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var updated = await context.Cotizaciones
                        .AsNoTracking()
                        .Include(c => c.Lineas)
                        .FirstAsync(c => c.Id == cotizacionId);

                    return Result<CotizacionDto>.Success(_mapper.Map<CotizacionDto>(updated));
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting cotizacion {Id}", cotizacionId);
            return Result<CotizacionDto>.Failure(_localizer["Error rejecting cotizacion"]);
        }
    }

    public async Task<Result<CotizacionDto?>> GetLatestByObraIdAsync(int obraId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var cotizacion = await context.Cotizaciones
                .AsNoTracking()
                .Include(c => c.Lineas)
                .Where(c => c.ObraId == obraId)
                .OrderByDescending(c => c.Version)
                .FirstOrDefaultAsync();

            return Result<CotizacionDto?>.Success(cotizacion == null ? null : _mapper.Map<CotizacionDto>(cotizacion));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latest cotizacion for obra {Id}", obraId);
            return Result<CotizacionDto?>.Failure(_localizer["Error retrieving cotizacion"]);
        }
    }

    public async Task<Result<byte[]>> GetCotizacionPdfAsync(int cotizacionId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var cotizacion = await context.Cotizaciones
                .AsNoTracking()
                .Include(c => c.Lineas)
                .Include(c => c.Obra).ThenInclude(o => o.Cliente)
                .FirstOrDefaultAsync(c => c.Id == cotizacionId, cancellationToken);

            if (cotizacion == null)
                return Result<byte[]>.Failure(_localizer["Cotizacion not found"]);

            var companySettings = await _companySettingsService.GetSettingsAsync();
            var companyName = string.IsNullOrWhiteSpace(companySettings?.CompanyName)
                ? _applicationOptions.Value.Name
                : companySettings.CompanyName;

            var logoBase64 = await _companySettingsService.GetLogoDataUriAsync()
                ?? await _emailTemplateService.GetStaticFileBase64Async(_brandingOptions.Value.LogoPath);

            var data = new
            {
                company_name = companyName,
                logo_base64 = logoBase64,
                has_logo = !string.IsNullOrEmpty(logoBase64),
                primary_color = _brandingOptions.Value.PrimaryColor,
                secondary_color = _brandingOptions.Value.SecondaryColor,
                cotizacion_version = cotizacion.Version,
                fecha_generacion = cotizacion.FechaGeneracion.ToString("dd/MM/yyyy"),
                cliente_nombre = cotizacion.Obra.Cliente.Nombre,
                obra_direccion = cotizacion.Obra.Direccion,
                total = cotizacion.Total.ToString("C2"),
                label_quotation = _localizer["Quotation"].Value,
                label_version = _localizer["Version"].Value,
                label_client = _localizer["Client"].Value,
                label_address = _localizer["Address"].Value,
                label_service = _localizer["Service"].Value,
                label_quantity = _localizer["Quantity"].Value,
                label_unit_price = _localizer["Unit Price"].Value,
                label_subtotal = _localizer["Subtotal"].Value,
                label_total = _localizer["Total"].Value,
                lineas = cotizacion.Lineas.Select(l => new
                {
                    servicio_nombre = l.ServicioNombre,
                    unidad_medida = l.UnidadMedida,
                    cantidad = l.Cantidad.ToString("F2"),
                    precio_unitario = l.PrecioUnitario.ToString("C2"),
                    subtotal = l.Subtotal.ToString("C2")
                }).ToList()
            };

            var (htmlBody, css) = await _templateSettingsService.GetEffectiveTemplateAsync();

            var fullHtml = $"""
                <!DOCTYPE html>
                <html lang="es">
                <head>
                <meta charset="UTF-8">
                <style>
                {css}
                </style>
                </head>
                <body>
                {htmlBody}
                </body>
                </html>
                """;

            var template = Scriban.Template.Parse(fullHtml);
            var renderedHtml = await template.RenderAsync(data);

            var pdfBytes = await _pdfService.GeneratePdfFromHtmlAsync(renderedHtml, cancellationToken);

            return Result<byte[]>.Success(pdfBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for cotizacion {Id}", cotizacionId);
            return Result<byte[]>.Failure(_localizer["Error generating cotizacion PDF"]);
        }
    }
}
