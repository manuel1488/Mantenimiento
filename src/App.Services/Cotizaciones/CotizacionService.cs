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
    private readonly IFiscalCatalogService _fiscalCatalogService;
    private readonly ICotizacionFolioService _folioService;
    private readonly ICotizacionIntegrityHashService _integrityHashService;
    private readonly IOptions<ApplicationOptions> _applicationOptions;
    private readonly IOptions<BrandingOptions> _brandingOptions;
    private readonly IImageService _imageService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IOptions<CotizacionFotoOptions> _fotoOptions;

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
        IFiscalCatalogService fiscalCatalogService,
        ICotizacionFolioService folioService,
        ICotizacionIntegrityHashService integrityHashService,
        IOptions<ApplicationOptions> applicationOptions,
        IOptions<BrandingOptions> brandingOptions,
        IImageService imageService,
        IFileStorageService fileStorageService,
        IOptions<CotizacionFotoOptions> fotoOptions)
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
        _fiscalCatalogService = fiscalCatalogService;
        _folioService = folioService;
        _integrityHashService = integrityHashService;
        _applicationOptions = applicationOptions;
        _brandingOptions = brandingOptions;
        _imageService = imageService;
        _fileStorageService = fileStorageService;
        _fotoOptions = fotoOptions;
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

                    var (folioAnio, folioNumero) = await _folioService.GenerarSiguienteFolioAsync();

                    var cotizacion = new Cotizacion
                    {
                        ClienteId = dto.ClienteId,
                        FechaGeneracion = currentTime,
                        FolioAnio = folioAnio,
                        FolioNumero = folioNumero,
                        Estado = CotizacionEstado.Pendiente,
                        IncluirIva = dto.IncluirIva,
                        CreatedBy = currentUser,
                        CreatedAt = currentTime,
                        ModifiedBy = currentUser,
                        ModifiedAt = currentTime
                    };

                    decimal subtotalTotal = 0;
                    foreach (var linea in dto.Lineas)
                    {
                        var servicio = servicios[linea.ServicioId];
                        var precioUnitario = linea.PrecioUnitarioOverride ?? servicio.PrecioUnitario;
                        var subtotal = linea.Cantidad * precioUnitario;
                        subtotalTotal += subtotal;

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

                    await ApplyTotalesAsync(cotizacion, subtotalTotal, dto.IncluirIva);
                    cotizacion.IntegridadHash = ComputeIntegridadHash(cotizacion);

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

                    foreach (var lineaExistente in cotizacion.Lineas)
                        lineaExistente.DeletedBy = currentUser;
                    context.CotizacionLineas.RemoveRange(cotizacion.Lineas);
                    cotizacion.Lineas.Clear();
                    cotizacion.ClienteId = dto.ClienteId;

                    decimal subtotalTotal = 0;
                    foreach (var linea in dto.Lineas)
                    {
                        var servicio = servicios[linea.ServicioId];
                        var precioUnitario = linea.PrecioUnitarioOverride ?? servicio.PrecioUnitario;
                        var subtotal = linea.Cantidad * precioUnitario;
                        subtotalTotal += subtotal;

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

                    await ApplyTotalesAsync(cotizacion, subtotalTotal, dto.IncluirIva);
                    cotizacion.IntegridadHash = ComputeIntegridadHash(cotizacion);
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

            var templateSettings = await _templateSettingsService.GetConfigAsync();
            var regimenesFiscales = await _fiscalCatalogService.GetRegimenesFiscalesAsync();
            var timeZone = await _companySettingsService.GetCurrentTimeZoneAsync();

            var fotos = await context.CotizacionFotos
                .AsNoTracking()
                .Where(f => f.CotizacionId == cotizacionId)
                .OrderBy(f => f.FechaCarga)
                .ToListAsync(cancellationToken);

            var fotosData = new List<(string Url, string? Descripcion)>();
            foreach (var foto in fotos)
            {
                var presignedResult = await _fileStorageService.GetPresignedUrlAsync(foto.FileKey, cancellationToken);
                if (presignedResult.IsSuccess)
                    fotosData.Add((presignedResult.Value!, foto.Descripcion));
            }

            var generadoPorNombre = await context.Users
                .IgnoreQueryFilters()
                .Where(u => u.Id == cotizacion.CreatedBy)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(cancellationToken);
            var generadoPor = string.IsNullOrWhiteSpace(generadoPorNombre) ? cotizacion.CreatedBy : generadoPorNombre;
            var fechaGeneracionLocal = TimeZoneInfo.ConvertTimeFromUtc(cotizacion.FechaGeneracion, timeZone);

            var cliente = cotizacion.Cliente;
            var regimenFiscal = regimenesFiscales.FirstOrDefault(r => r.Codigo == cliente.RegimenFiscal);
            var clienteRegimenFiscalDisplay = string.IsNullOrWhiteSpace(cliente.RegimenFiscal)
                ? null
                : regimenFiscal is null
                    ? cliente.RegimenFiscal
                    : $"{regimenFiscal.Codigo} - {regimenFiscal.Descripcion}";
            var hasPaymentTerms = !string.IsNullOrWhiteSpace(templateSettings?.PaymentTermsText);
            var mostrarDatosBancarios = templateSettings != null
                && templateSettings.MostrarDatosBancarios
                && (!string.IsNullOrWhiteSpace(templateSettings.BancoBeneficiario)
                    || !string.IsNullOrWhiteSpace(templateSettings.BancoRfc)
                    || !string.IsNullOrWhiteSpace(templateSettings.BancoNombre)
                    || !string.IsNullOrWhiteSpace(templateSettings.BancoNumeroCuenta)
                    || !string.IsNullOrWhiteSpace(templateSettings.BancoClabe)
                    || !string.IsNullOrWhiteSpace(templateSettings.BancoSwift));
            var mostrarDireccion = templateSettings is { MostrarDireccionEnCotizacion: true }
                && !string.IsNullOrWhiteSpace(templateSettings.Direccion);
            var mostrarContacto = templateSettings != null
                && templateSettings.MostrarContacto
                && (!string.IsNullOrWhiteSpace(templateSettings.SitioWeb)
                    || !string.IsNullOrWhiteSpace(templateSettings.Telefono)
                    || !string.IsNullOrWhiteSpace(templateSettings.CorreoElectronico)
                    || !string.IsNullOrWhiteSpace(templateSettings.WhatsApp)
                    || !string.IsNullOrWhiteSpace(templateSettings.Facebook)
                    || !string.IsNullOrWhiteSpace(templateSettings.Instagram));

            var data = new
            {
                company_name = companyName,
                company_direccion = templateSettings?.Direccion,
                mostrar_direccion = mostrarDireccion,
                logo_base64 = logoBase64,
                has_logo = !string.IsNullOrEmpty(logoBase64),
                primary_color = _brandingOptions.Value.PrimaryColor,
                secondary_color = _brandingOptions.Value.SecondaryColor,
                cotizacion_id = cotizacion.Id,
                cotizacion_folio = CotizacionFolioFormatter.Format(
                    cotizacion.Id, cotizacion.FolioAnio, cotizacion.FolioNumero,
                    templateSettings?.FolioPrefijo, templateSettings?.FolioDigitos),
                generado_por = generadoPor,
                has_generado_por = !string.IsNullOrWhiteSpace(generadoPor),
                integridad_hash = cotizacion.IntegridadHash,
                has_integridad_hash = !string.IsNullOrWhiteSpace(cotizacion.IntegridadHash),
                fecha_generacion = fechaGeneracionLocal.ToString("dd/MM/yyyy"),
                hora_generacion = fechaGeneracionLocal.ToString("HH:mm"),
                cliente_nombre = cliente.Nombre,
                cliente_correo = cliente.Correo,
                has_cliente_correo = !string.IsNullOrWhiteSpace(cliente.Correo),
                cliente_telefono = cliente.Telefono,
                has_cliente_telefono = !string.IsNullOrWhiteSpace(cliente.Telefono),
                cliente_direccion = FormatDireccion(cliente.Calle, cliente.NumeroExterior, cliente.NumeroInterior, cliente.Colonia, cliente.Ciudad, cliente.Estado, cliente.CodigoPostal),
                has_cliente_direccion = !string.IsNullOrWhiteSpace(cliente.Calle),
                cliente_tiene_datos_fiscales = cliente.TieneDatosFiscales,
                cliente_razon_social = cliente.RazonSocial,
                cliente_rfc = cliente.Rfc,
                cliente_regimen_fiscal = clienteRegimenFiscalDisplay,
                subtotal = cotizacion.Subtotal.ToString("C2"),
                incluir_iva = cotizacion.IncluirIva,
                iva_tasa = cotizacion.IvaTasa.ToString("F2"),
                iva_monto = cotizacion.IvaMonto.ToString("C2"),
                total = cotizacion.Total.ToString("C2"),
                has_payment_terms = hasPaymentTerms,
                payment_terms_text = templateSettings?.PaymentTermsText,
                mostrar_datos_bancarios = mostrarDatosBancarios,
                banco_beneficiario = templateSettings?.BancoBeneficiario,
                banco_rfc = templateSettings?.BancoRfc,
                banco_nombre = templateSettings?.BancoNombre,
                banco_numero_cuenta = templateSettings?.BancoNumeroCuenta,
                banco_clabe = templateSettings?.BancoClabe,
                banco_swift = templateSettings?.BancoSwift,
                mostrar_contacto = mostrarContacto,
                contacto_sitio_web = templateSettings?.SitioWeb,
                contacto_sitio_web_href = ContactLinkFormatter.NormalizeUrl(templateSettings?.SitioWeb),
                has_contacto_sitio_web = !string.IsNullOrWhiteSpace(templateSettings?.SitioWeb),
                contacto_telefono = templateSettings?.Telefono,
                contacto_telefono_href = ContactLinkFormatter.BuildTelHref(templateSettings?.Telefono),
                has_contacto_telefono = !string.IsNullOrWhiteSpace(templateSettings?.Telefono),
                contacto_correo = templateSettings?.CorreoElectronico,
                contacto_correo_href = ContactLinkFormatter.BuildMailtoHref(templateSettings?.CorreoElectronico),
                has_contacto_correo = !string.IsNullOrWhiteSpace(templateSettings?.CorreoElectronico),
                contacto_whatsapp = templateSettings?.WhatsApp,
                contacto_whatsapp_href = ContactLinkFormatter.BuildWhatsAppUrl(templateSettings?.WhatsApp),
                has_contacto_whatsapp = !string.IsNullOrWhiteSpace(templateSettings?.WhatsApp),
                contacto_facebook = templateSettings?.Facebook,
                contacto_facebook_href = ContactLinkFormatter.NormalizeUrl(templateSettings?.Facebook),
                has_contacto_facebook = !string.IsNullOrWhiteSpace(templateSettings?.Facebook),
                contacto_instagram = templateSettings?.Instagram,
                contacto_instagram_href = ContactLinkFormatter.NormalizeUrl(templateSettings?.Instagram),
                has_contacto_instagram = !string.IsNullOrWhiteSpace(templateSettings?.Instagram),
                label_quotation = _localizer["Quotation"].Value,
                label_client = _localizer["Client"].Value,
                label_fiscal_data = _localizer["Fiscal Data"].Value,
                label_date = _localizer["Date"].Value,
                label_email = _localizer["Email"].Value,
                label_phone = _localizer["Phone"].Value,
                label_address = _localizer["Address"].Value,
                label_rfc = _localizer["RFC"].Value,
                label_tax_regime = _localizer["Tax Regime"].Value,
                label_service = _localizer["Service"].Value,
                label_quantity = _localizer["Quantity"].Value,
                label_unit_price = _localizer["Unit Price"].Value,
                label_subtotal = _localizer["Subtotal"].Value,
                label_iva = _localizer["IVA"].Value,
                label_total = _localizer["Total"].Value,
                label_payment_terms = _localizer["Payment Terms"].Value,
                label_bank_transfer = _localizer["Bank Transfer Details"].Value,
                label_beneficiary = _localizer["Beneficiary"].Value,
                label_bank = _localizer["Bank"].Value,
                label_account_number = _localizer["Account Number"].Value,
                label_clabe = _localizer["CLABE"].Value,
                label_swift = _localizer["SWIFT/BIC"].Value,
                label_generated_by = _localizer["Generated by"].Value,
                label_integrity_hash = _localizer["Integrity Hash"].Value,
                lineas = cotizacion.Lineas.Select((l, i) => new
                {
                    indice = i + 1,
                    servicio_nombre = l.ServicioNombre,
                    unidad_medida = l.UnidadMedida,
                    cantidad = l.Cantidad.ToString("F2"),
                    precio_unitario = l.PrecioUnitario.ToString("C2"),
                    subtotal = l.Subtotal.ToString("C2")
                }).ToList(),
                label_photos = _localizer["Photos"].Value,
                has_fotos = fotosData.Count > 0,
                fotos = fotosData.Select(f => new
                {
                    url = f.Url,
                    descripcion = f.Descripcion,
                    has_descripcion = !string.IsNullOrWhiteSpace(f.Descripcion)
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

    private static string? FormatDireccion(string? calle, string? numeroExterior, string? numeroInterior, string? colonia, string? ciudad, string? estado, string? codigoPostal)
    {
        if (string.IsNullOrWhiteSpace(calle))
            return null;

        var numero = string.IsNullOrWhiteSpace(numeroInterior)
            ? numeroExterior
            : $"{numeroExterior} Int. {numeroInterior}";

        var parts = new[]
        {
            string.IsNullOrWhiteSpace(numero) ? calle : $"{calle} {numero}",
            colonia,
            ciudad,
            estado,
            codigoPostal
        };

        return string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
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

    /// <summary>
    /// Sets Subtotal/IvaTasa/IvaMonto/Total on <paramref name="cotizacion"/>. When
    /// <paramref name="incluirIva"/> is true, snapshots the company's current default IVA rate
    /// (<see cref="Core.Interfaces.ICompanySettingsService"/>) — a later change to that default does
    /// not retroactively alter this Cotización's stored rate/amount.
    /// </summary>
    private async Task ApplyTotalesAsync(Cotizacion cotizacion, decimal subtotal, bool incluirIva)
    {
        cotizacion.Subtotal = subtotal;

        if (!incluirIva)
        {
            cotizacion.IvaTasa = 0;
            cotizacion.IvaMonto = 0;
            cotizacion.Total = subtotal;
            return;
        }

        var companySettings = await _companySettingsService.GetSettingsAsync();
        var ivaTasa = companySettings?.IvaTasaPorDefecto ?? 0;

        cotizacion.IvaTasa = ivaTasa;
        cotizacion.IvaMonto = Math.Round(subtotal * ivaTasa / 100m, 2);
        cotizacion.Total = subtotal + cotizacion.IvaMonto;
    }

    private string ComputeIntegridadHash(Cotizacion cotizacion) => _integrityHashService.Compute(
        cotizacion.FolioAnio,
        cotizacion.FolioNumero,
        cotizacion.ClienteId,
        cotizacion.FechaGeneracion,
        cotizacion.Subtotal,
        cotizacion.IncluirIva,
        cotizacion.IvaTasa,
        cotizacion.IvaMonto,
        cotizacion.Total,
        cotizacion.Lineas.Select(l => new CotizacionIntegrityLinea(
            l.ServicioNombre, l.UnidadMedida, l.Cantidad, l.PrecioUnitario, l.Subtotal)));

    public async Task<Result<CotizacionFotoDto>> UploadFotoAsync(
        int cotizacionId, byte[] data, string contentType, string fileName, string? descripcion = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var cotizacionExists = await context.Cotizaciones.AnyAsync(c => c.Id == cotizacionId, cancellationToken);
            if (!cotizacionExists)
                return Result<CotizacionFotoDto>.Failure(_localizer["Cotizacion not found"]);

            var currentFotoCount = await context.CotizacionFotos
                .Where(f => f.CotizacionId == cotizacionId)
                .CountAsync(cancellationToken);

            if (currentFotoCount >= _fotoOptions.Value.MaxFotos)
            {
                return Result<CotizacionFotoDto>.Failure(
                    _localizer["Maximum number of photos ({0}) reached for this Cotización", _fotoOptions.Value.MaxFotos]);
            }

            byte[] processedData;
            byte[] thumbnailData;
            string processedContentType;
            using (var stream = new MemoryStream(data))
            {
                (processedData, thumbnailData, processedContentType) = await _imageService.ProcessImageWithThumbnailAsync(
                    stream, fileName, contentType, cancellationToken: cancellationToken);
            }

            var extension = processedContentType switch
            {
                "image/png" => "png",
                "image/webp" => "webp",
                _ => "jpg"
            };

            var keyPrefix = $"cotizaciones/cotizacion-{cotizacionId}";

            var uploadResult = await _fileStorageService.UploadAsync(
                processedData, processedContentType, keyPrefix, extension, cancellationToken);

            if (!uploadResult.IsSuccess)
                return Result<CotizacionFotoDto>.Failure(uploadResult.Error!);

            // The thumbnail is an optimization, not a hard requirement — if it fails to upload,
            // log it and continue without one; the UI falls back to the full image for display.
            var thumbnailUploadResult = await _fileStorageService.UploadAsync(
                thumbnailData, processedContentType, $"{keyPrefix}/thumb", extension, cancellationToken);

            if (!thumbnailUploadResult.IsSuccess)
            {
                _logger.LogWarning(
                    "Failed to upload thumbnail for cotizacion {Id}, continuing with full image only: {Error}",
                    cotizacionId, thumbnailUploadResult.Error);
            }

            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var entity = new CotizacionFoto
                    {
                        CotizacionId = cotizacionId,
                        FileKey = uploadResult.Value!,
                        ThumbnailFileKey = thumbnailUploadResult.IsSuccess ? thumbnailUploadResult.Value : null,
                        MimeType = processedContentType,
                        FileSize = processedData.LongLength,
                        Descripcion = descripcion,
                        FechaCarga = _dateTimeService.Now
                    };

                    var currentUser = await _currentUserService.GetUserIdAsync();
                    var currentTime = _dateTimeService.Now;
                    entity.CreatedBy = currentUser;
                    entity.CreatedAt = currentTime;
                    entity.ModifiedBy = currentUser;
                    entity.ModifiedAt = currentTime;

                    context.CotizacionFotos.Add(entity);
                    await context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    var presignedResult = await _fileStorageService.GetPresignedUrlAsync(entity.FileKey, cancellationToken);
                    var thumbnailPresignedResult = entity.ThumbnailFileKey is not null
                        ? await _fileStorageService.GetPresignedUrlAsync(entity.ThumbnailFileKey, cancellationToken)
                        : null;

                    var resultDto = _mapper.Map<CotizacionFotoDto>(entity);
                    resultDto.PresignedUrl = presignedResult.IsSuccess ? presignedResult.Value : null;
                    resultDto.ThumbnailPresignedUrl = thumbnailPresignedResult?.IsSuccess == true
                        ? thumbnailPresignedResult.Value
                        : resultDto.PresignedUrl;

                    return Result<CotizacionFotoDto>.Success(resultDto);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    // Best effort cleanup of the orphaned blob(s) since the DB write failed.
                    await _fileStorageService.DeleteAsync(uploadResult.Value!, cancellationToken);
                    if (thumbnailUploadResult.IsSuccess)
                        await _fileStorageService.DeleteAsync(thumbnailUploadResult.Value!, cancellationToken);
                    throw;
                }
            });
        }
        catch (ArgumentException ex)
        {
            // Thrown by IImageService when the file fails validation (size/type) against
            // the ImageOptions configured in appsettings — surface the specific reason to the user.
            return Result<CotizacionFotoDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading foto for cotizacion {Id}", cotizacionId);
            return Result<CotizacionFotoDto>.Failure(_localizer["Error uploading foto"]);
        }
    }

    public async Task<Result> DeleteFotoAsync(int fotoId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var entity = await context.CotizacionFotos.FindAsync([fotoId], cancellationToken);
            if (entity == null)
                return Result.Failure(_localizer["Foto not found"]);

            var key = entity.FileKey;
            var thumbnailKey = entity.ThumbnailFileKey;

            var strategy = context.Database.CreateExecutionStrategy();
            var deleteResult = await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    entity.DeletedBy = await _currentUserService.GetUserIdAsync();
                    context.CotizacionFotos.Remove(entity);
                    await context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return Result.Success();
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            if (!deleteResult.IsSuccess)
                return deleteResult;

            var storageDeleteResult = await _fileStorageService.DeleteAsync(key, cancellationToken);
            if (!storageDeleteResult.IsSuccess)
                _logger.LogWarning("Failed to delete foto blob {Key} from storage: {Error}", key, storageDeleteResult.Error);

            if (thumbnailKey is not null)
            {
                var thumbnailDeleteResult = await _fileStorageService.DeleteAsync(thumbnailKey, cancellationToken);
                if (!thumbnailDeleteResult.IsSuccess)
                    _logger.LogWarning("Failed to delete foto thumbnail {Key} from storage: {Error}", thumbnailKey, thumbnailDeleteResult.Error);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting foto {Id}", fotoId);
            return Result.Failure(_localizer["Error deleting foto"]);
        }
    }

    public async Task<Result> UpdateFotoDescripcionAsync(int fotoId, string? descripcion, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var entity = await context.CotizacionFotos.FindAsync([fotoId], cancellationToken);
            if (entity == null)
                return Result.Failure(_localizer["Foto not found"]);

            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    entity.Descripcion = descripcion;
                    entity.ModifiedBy = await _currentUserService.GetUserIdAsync();
                    entity.ModifiedAt = _dateTimeService.Now;
                    await context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return Result.Success();
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating foto {Id} descripcion", fotoId);
            return Result.Failure(_localizer["Error updating foto"]);
        }
    }

    public async Task<Result<List<CotizacionFotoDto>>> GetFotosAsync(int cotizacionId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var fotos = await context.CotizacionFotos
                .AsNoTracking()
                .Where(f => f.CotizacionId == cotizacionId)
                .OrderBy(f => f.FechaCarga)
                .ToListAsync(cancellationToken);

            var dtos = new List<CotizacionFotoDto>();
            foreach (var foto in fotos)
            {
                var dto = _mapper.Map<CotizacionFotoDto>(foto);
                var presignedResult = await _fileStorageService.GetPresignedUrlAsync(foto.FileKey, cancellationToken);
                dto.PresignedUrl = presignedResult.IsSuccess ? presignedResult.Value : null;

                var thumbnailResult = foto.ThumbnailFileKey is not null
                    ? await _fileStorageService.GetPresignedUrlAsync(foto.ThumbnailFileKey, cancellationToken)
                    : null;
                dto.ThumbnailPresignedUrl = thumbnailResult?.IsSuccess == true ? thumbnailResult.Value : dto.PresignedUrl;

                dtos.Add(dto);
            }

            return Result<List<CotizacionFotoDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fotos for cotizacion {Id}", cotizacionId);
            return Result<List<CotizacionFotoDto>>.Failure(_localizer["Error retrieving fotos"]);
        }
    }
}
