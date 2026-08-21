using AutoMapper;
using AutoMapper.QueryableExtensions;

using App.Core.Common;
using App.Core.DTOs.Cotizaciones;
using App.Core.Enums.Cotizaciones;
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
    private readonly IEmailService _emailService;
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
        IEmailService emailService,
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
        _emailService = emailService;
        _applicationOptions = applicationOptions;
        _brandingOptions = brandingOptions;
    }

    public async Task<Result<List<CotizacionDto>>> GetAllAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var cotizaciones = await context.Cotizaciones
                .AsNoTracking()
                .OrderByDescending(c => c.FechaGeneracion)
                .ProjectTo<CotizacionDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return Result<List<CotizacionDto>>.Success(cotizaciones);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cotizaciones");
            return Result<List<CotizacionDto>>.Failure(_localizer["Error retrieving cotizaciones"]);
        }
    }

    public async Task<Result<List<CotizacionDto>>> GetByClienteIdAsync(int clienteId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var cotizaciones = await context.Cotizaciones
                .AsNoTracking()
                .Where(c => c.ClienteId == clienteId)
                .OrderByDescending(c => c.FechaGeneracion)
                .ProjectTo<CotizacionDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return Result<List<CotizacionDto>>.Success(cotizaciones);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cotizaciones for cliente {Id}", clienteId);
            return Result<List<CotizacionDto>>.Failure(_localizer["Error retrieving cotizaciones"]);
        }
    }

    public async Task<Result<CotizacionDto>> GetByIdAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var cotizacion = await context.Cotizaciones
                .AsNoTracking()
                .Where(c => c.Id == id)
                .ProjectTo<CotizacionDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            if (cotizacion == null)
                return Result<CotizacionDto>.Failure(_localizer["Cotizacion not found"]);

            return Result<CotizacionDto>.Success(cotizacion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cotizacion {Id}", id);
            return Result<CotizacionDto>.Failure(_localizer["Error retrieving cotizacion"]);
        }
    }

    public async Task<Result<CotizacionDto>> CreateAsync(CreateCotizacionDto dto)
    {
        try
        {
            if (dto.Lineas.Count == 0)
                return Result<CotizacionDto>.Failure(_localizer["A Cotizacion must have at least one linea"]);

            await using var context = await _contextFactory.CreateDbContextAsync();

            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    var clienteExists = await context.Clientes.AnyAsync(c => c.Id == dto.ClienteId);
                    if (!clienteExists)
                    {
                        await transaction.RollbackAsync();
                        return Result<CotizacionDto>.Failure(_localizer["Cliente not found"]);
                    }

                    var servicioIds = dto.Lineas.Select(l => l.ServicioId).Distinct().ToList();
                    var servicios = await context.Servicios
                        .Include(s => s.UnidadMedida)
                        .Where(s => servicioIds.Contains(s.Id))
                        .ToDictionaryAsync(s => s.Id);

                    if (servicios.Count != servicioIds.Count)
                    {
                        await transaction.RollbackAsync();
                        return Result<CotizacionDto>.Failure(_localizer["One or more Servicios were not found"]);
                    }

                    var currentUser = await _currentUserService.GetUserIdAsync();
                    var currentTime = _dateTimeService.Now;

                    var cotizacion = new Cotizacion
                    {
                        ClienteId = dto.ClienteId,
                        FechaGeneracion = currentTime,
                        Estado = CotizacionEstado.Pendiente,
                        CreatedBy = currentUser,
                        CreatedAt = currentTime,
                        ModifiedBy = currentUser,
                        ModifiedAt = currentTime
                    };

                    decimal total = 0;
                    foreach (var linea in dto.Lineas)
                    {
                        var servicio = servicios[linea.ServicioId];
                        var precioUnitario = linea.PrecioUnitarioOverride ?? servicio.PrecioUnitario;
                        var subtotal = linea.Cantidad * precioUnitario;
                        total += subtotal;

                        cotizacion.Lineas.Add(new CotizacionLinea
                        {
                            ServicioId = servicio.Id,
                            ServicioNombre = servicio.Nombre,
                            UnidadMedida = servicio.UnidadMedida.Codigo,
                            Cantidad = linea.Cantidad,
                            PrecioUnitario = precioUnitario,
                            Subtotal = subtotal,
                            CreatedBy = currentUser,
                            CreatedAt = currentTime,
                            ModifiedBy = currentUser,
                            ModifiedAt = currentTime
                        });
                    }

                    cotizacion.Total = total;

                    context.Cotizaciones.Add(cotizacion);
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var created = await context.Cotizaciones
                        .AsNoTracking()
                        .Where(c => c.Id == cotizacion.Id)
                        .ProjectTo<CotizacionDto>(_mapper.ConfigurationProvider)
                        .FirstAsync();

                    return Result<CotizacionDto>.Success(created);
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
            _logger.LogError(ex, "Error creating cotizacion for cliente {Id}", dto.ClienteId);
            return Result<CotizacionDto>.Failure(_localizer["Error creating cotizacion"]);
        }
    }

    public async Task<Result<CotizacionDto>> UpdateAsync(int cotizacionId, UpdateCotizacionDto dto)
    {
        try
        {
            if (dto.Lineas.Count == 0)
                return Result<CotizacionDto>.Failure(_localizer["A Cotizacion must have at least one linea"]);

            await using var context = await _contextFactory.CreateDbContextAsync();

            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    var cotizacion = await context.Cotizaciones
                        .Include(c => c.Lineas)
                        .FirstOrDefaultAsync(c => c.Id == cotizacionId);

                    if (cotizacion == null)
                    {
                        await transaction.RollbackAsync();
                        return Result<CotizacionDto>.Failure(_localizer["Cotizacion not found"]);
                    }

                    if (cotizacion.Estado != CotizacionEstado.Pendiente)
                    {
                        await transaction.RollbackAsync();
                        return Result<CotizacionDto>.Failure(_localizer["Only a Pendiente Cotizacion can be edited"]);
                    }

                    var clienteExists = await context.Clientes.AnyAsync(c => c.Id == dto.ClienteId);
                    if (!clienteExists)
                    {
                        await transaction.RollbackAsync();
                        return Result<CotizacionDto>.Failure(_localizer["Cliente not found"]);
                    }

                    var servicioIds = dto.Lineas.Select(l => l.ServicioId).Distinct().ToList();
                    var servicios = await context.Servicios
                        .Include(s => s.UnidadMedida)
                        .Where(s => servicioIds.Contains(s.Id))
                        .ToDictionaryAsync(s => s.Id);

                    if (servicios.Count != servicioIds.Count)
                    {
                        await transaction.RollbackAsync();
                        return Result<CotizacionDto>.Failure(_localizer["One or more Servicios were not found"]);
                    }

                    var currentUser = await _currentUserService.GetUserIdAsync();
                    var currentTime = _dateTimeService.Now;

                    context.CotizacionLineas.RemoveRange(cotizacion.Lineas);
                    cotizacion.Lineas.Clear();
                    cotizacion.ClienteId = dto.ClienteId;

                    decimal total = 0;
                    foreach (var linea in dto.Lineas)
                    {
                        var servicio = servicios[linea.ServicioId];
                        var precioUnitario = linea.PrecioUnitarioOverride ?? servicio.PrecioUnitario;
                        var subtotal = linea.Cantidad * precioUnitario;
                        total += subtotal;

                        cotizacion.Lineas.Add(new CotizacionLinea
                        {
                            ServicioId = servicio.Id,
                            ServicioNombre = servicio.Nombre,
                            UnidadMedida = servicio.UnidadMedida.Codigo,
                            Cantidad = linea.Cantidad,
                            PrecioUnitario = precioUnitario,
                            Subtotal = subtotal,
                            CreatedBy = currentUser,
                            CreatedAt = currentTime,
                            ModifiedBy = currentUser,
                            ModifiedAt = currentTime
                        });
                    }

                    cotizacion.Total = total;
                    cotizacion.ModifiedBy = currentUser;
                    cotizacion.ModifiedAt = currentTime;

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var updated = await context.Cotizaciones
                        .AsNoTracking()
                        .Where(c => c.Id == cotizacion.Id)
                        .ProjectTo<CotizacionDto>(_mapper.ConfigurationProvider)
                        .FirstAsync();

                    return Result<CotizacionDto>.Success(updated);
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
            _logger.LogError(ex, "Error updating cotizacion {Id}", cotizacionId);
            return Result<CotizacionDto>.Failure(_localizer["Error updating cotizacion"]);
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
                    var cotizacion = await context.Cotizaciones.FirstOrDefaultAsync(c => c.Id == cotizacionId);

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

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var updated = await context.Cotizaciones
                        .AsNoTracking()
                        .Where(c => c.Id == cotizacionId)
                        .ProjectTo<CotizacionDto>(_mapper.ConfigurationProvider)
                        .FirstAsync();

                    return Result<CotizacionDto>.Success(updated);
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
                    var cotizacion = await context.Cotizaciones.FirstOrDefaultAsync(c => c.Id == cotizacionId);

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

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var updated = await context.Cotizaciones
                        .AsNoTracking()
                        .Where(c => c.Id == cotizacionId)
                        .ProjectTo<CotizacionDto>(_mapper.ConfigurationProvider)
                        .FirstAsync();

                    return Result<CotizacionDto>.Success(updated);
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

    public async Task<Result> DeleteAsync(int cotizacionId)
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
                    var cotizacion = await context.Cotizaciones.FirstOrDefaultAsync(c => c.Id == cotizacionId);

                    if (cotizacion == null)
                    {
                        await transaction.RollbackAsync();
                        return Result.Failure(_localizer["Cotizacion not found"]);
                    }

                    if (cotizacion.Estado != CotizacionEstado.Pendiente)
                    {
                        await transaction.RollbackAsync();
                        return Result.Failure(_localizer["Only a Cotizacion in Pendiente can be deleted"]);
                    }

                    cotizacion.DeletedBy = await _currentUserService.GetUserIdAsync();

                    context.Cotizaciones.Remove(cotizacion);
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Result.Success();
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
            _logger.LogError(ex, "Error deleting cotizacion {Id}", cotizacionId);
            return Result.Failure(_localizer["Error deleting cotizacion"]);
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
                .Include(c => c.Cliente)
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
                cotizacion_id = cotizacion.Id,
                fecha_generacion = cotizacion.FechaGeneracion.ToString("dd/MM/yyyy"),
                cliente_nombre = cotizacion.Cliente.Nombre,
                total = cotizacion.Total.ToString("C2"),
                label_quotation = _localizer["Quotation"].Value,
                label_client = _localizer["Client"].Value,
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

    public async Task<Result> SendCotizacionEmailAsync(int cotizacionId, string recipientEmail, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var cotizacion = await context.Cotizaciones
                .AsNoTracking()
                .Include(c => c.Cliente)
                .FirstOrDefaultAsync(c => c.Id == cotizacionId, cancellationToken);

            if (cotizacion == null)
                return Result.Failure(_localizer["Cotizacion not found"]);

            var pdfResult = await GetCotizacionPdfAsync(cotizacionId, cancellationToken);
            if (!pdfResult.IsSuccess)
                return Result.Failure(pdfResult.Error!);

            var companySettings = await _companySettingsService.GetSettingsAsync();
            var companyName = string.IsNullOrWhiteSpace(companySettings?.CompanyName)
                ? _applicationOptions.Value.Name
                : companySettings.CompanyName;

            var message = new Core.Models.Email.EmailMessage
            {
                To = recipientEmail,
                Subject = _localizer["Quotation {0} - {1}", cotizacion.Id, companyName],
                Body = _localizer["Attached is your quotation from {0}.", companyName],
                IsHtml = false,
                Attachments =
                [
                    new Core.Models.Email.EmailAttachment
                    {
                        FileName = $"cotizacion-{cotizacion.Id}.pdf",
                        Content = pdfResult.Value!,
                        ContentType = "application/pdf"
                    }
                ]
            };

            var emailResult = await _emailService.SendAsync(message, cancellationToken);
            if (!emailResult.Success)
            {
                _logger.LogWarning("Error sending cotizacion {Id} email to {Recipient}: {Error}", cotizacionId, recipientEmail, emailResult.Error);
                return Result.Failure(_localizer["Error sending cotizacion email"]);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending cotizacion {Id} email", cotizacionId);
            return Result.Failure(_localizer["Error sending cotizacion email"]);
        }
    }
}
