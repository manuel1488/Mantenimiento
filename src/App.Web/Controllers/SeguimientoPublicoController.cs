using App.Core.Common;
using App.Core.Enums.Obras;
using App.Core.Interfaces;
using App.Core.Options;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace App.Web.Controllers;

/// <summary>
/// Anonymous, read-only data source for the public obra-tracking link (wwwroot/seguimiento.html +
/// Alpine.js — see that file for the client side). Kept outside Blazor/SignalR entirely: this route
/// is opened by anonymous clients from an SMS/WhatsApp link, so it has no business holding a
/// per-visitor circuit open for content that never needs server-side interactivity.
/// </summary>
[ApiController]
[Route("api/seguimiento")]
[AllowAnonymous]
public class SeguimientoPublicoController : ControllerBase
{
    private readonly IObraClienteAccesoService _obraClienteAccesoService;
    private readonly IObraService _obraService;
    private readonly IActividadService _actividadService;
    private readonly IObraFolioSettingsService _obraFolioSettingsService;
    private readonly ICompanySettingsService _companySettingsService;
    private readonly IOptions<BrandingOptions> _brandingOptions;
    private readonly IOptions<ApplicationOptions> _applicationOptions;
    private readonly IStringLocalizer<SeguimientoPublicoController> _localizer;
    private readonly ILogger<SeguimientoPublicoController> _logger;

    public SeguimientoPublicoController(
        IObraClienteAccesoService obraClienteAccesoService,
        IObraService obraService,
        IActividadService actividadService,
        IObraFolioSettingsService obraFolioSettingsService,
        ICompanySettingsService companySettingsService,
        IOptions<BrandingOptions> brandingOptions,
        IOptions<ApplicationOptions> applicationOptions,
        IStringLocalizer<SeguimientoPublicoController> localizer,
        ILogger<SeguimientoPublicoController> logger)
    {
        _obraClienteAccesoService = obraClienteAccesoService;
        _obraService = obraService;
        _actividadService = actividadService;
        _obraFolioSettingsService = obraFolioSettingsService;
        _companySettingsService = companySettingsService;
        _brandingOptions = brandingOptions;
        _applicationOptions = applicationOptions;
        _localizer = localizer;
        _logger = logger;
    }

    [HttpGet("{token}")]
    public async Task<IActionResult> Get([FromRoute] string token)
    {
        // Presigned MinIO URLs are only valid for a limited window (PresignedUrlExpiryHours) —
        // never let a proxy/CDN cache and replay this response past that.
        Response.Headers.CacheControl = "no-store";

        try
        {
            var resolveResult = await _obraClienteAccesoService.ResolveTokenAsync(token);
            if (!resolveResult.IsSuccess)
                return NotFound(new { error = resolveResult.Error });

            var obraResult = await _obraService.GetByIdAsync(resolveResult.Value);
            if (!obraResult.IsSuccess)
                return NotFound(new { error = obraResult.Error });

            var obra = obraResult.Value!;

            var folioSettings = await _obraFolioSettingsService.GetSettingsAsync();
            var folio = CotizacionFolioFormatter.Format(
                obra.Id, obra.FolioAnio, obra.FolioNumero, folioSettings.FolioPrefijo, folioSettings.FolioDigitos);

            var actividadesResult = await _actividadService.GetByObraIdAsync(obra.Id);
            var actividades = actividadesResult.IsSuccess
                ? actividadesResult.Value!.Select(a => new ActividadSeguimientoDto
                {
                    ServicioNombre = a.ServicioNombre,
                    Descripcion = a.Descripcion,
                    Estado = a.Estado.ToString(),
                    PorcentajeAvance = a.PorcentajeAvance
                }).ToList()
                : new List<ActividadSeguimientoDto>();

            var mensajesResult = await _obraService.GetMensajesAsync(obra.Id);
            var mensajes = mensajesResult.IsSuccess
                ? mensajesResult.Value!.Select(m => new MensajeSeguimientoDto
                {
                    Tipo = m.Tipo.ToString(),
                    TipoLabel = TipoLabel(m.Tipo),
                    Asunto = AsuntoDisplay(m),
                    Cuerpo = CuerpoDisplay(m),
                    FechaEnvio = m.FechaEnvio,
                    Canales = ChannelsOnly(m.Destinatarios),
                    FotoUrl = m.FotoPresignedUrl,
                    FotoThumbnailUrl = m.FotoThumbnailPresignedUrl
                }).ToList()
                : new List<MensajeSeguimientoDto>();

            var fotosResult = await _obraService.GetFotosGeneralesAsync(obra.Id);
            var fotos = fotosResult.IsSuccess
                ? fotosResult.Value!.Select(f => new FotoSeguimientoDto
                {
                    Url = f.PresignedUrl,
                    ThumbnailUrl = f.ThumbnailPresignedUrl,
                    Descripcion = f.Descripcion
                }).ToList()
                : new List<FotoSeguimientoDto>();

            var logoUrl = await _companySettingsService.GetLogoDataUriAsync() ?? _brandingOptions.Value.LogoPath;

            var dto = new ObraSeguimientoPublicoDto
            {
                AppName = _applicationOptions.Value.Name,
                LogoUrl = logoUrl,
                PrimaryColor = _brandingOptions.Value.PrimaryColor,
                SecondaryColor = _brandingOptions.Value.SecondaryColor,
                Folio = folio,
                Direccion = obra.Direccion,
                Estado = obra.Estado.ToString(),
                EstadoLabel = EstadoLabel(obra.Estado),
                PorcentajeAvance = obra.PorcentajeAvance,
                Actividades = actividades,
                Mensajes = mensajes,
                Fotos = fotos
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building public seguimiento response for token");
            return StatusCode(500, new { error = _localizer["Error loading tracking information"].Value });
        }
    }

    private string EstadoLabel(ObraEstado estado) => estado switch
    {
        ObraEstado.Solicitada => _localizer["Requested"],
        ObraEstado.Cotizada => _localizer["Quoted"],
        ObraEstado.Rechazada => _localizer["Rejected"],
        ObraEstado.Aprobada => _localizer["Approved"],
        ObraEstado.EnProceso => _localizer["In Progress"],
        ObraEstado.Finalizada => _localizer["Finished"],
        ObraEstado.Facturada => _localizer["Invoiced"],
        _ => estado.ToString()
    };

    private string TipoLabel(TipoObraMensaje tipo) => tipo switch
    {
        TipoObraMensaje.Alerta => _localizer["Alert"],
        TipoObraMensaje.Avance => _localizer["Progress Update"],
        _ => _localizer["Message"]
    };

    // Mensaje/Alerta are free text the sender typed — shown as-is. Avance is system-generated, so
    // Asunto/Cuerpo hold raw data (Servicio name, note, %) instead of a frozen sentence — rebuilding
    // it here means it always renders in the request's current culture.
    private string AsuntoDisplay(App.Core.DTOs.Obras.ObraMensajeDto mensaje) => mensaje.Tipo == TipoObraMensaje.Avance
        ? _localizer["Progress update on your project"]
        : mensaje.Asunto;

    private string CuerpoDisplay(App.Core.DTOs.Obras.ObraMensajeDto mensaje) => mensaje.Tipo == TipoObraMensaje.Avance
        ? (string.IsNullOrWhiteSpace(mensaje.Cuerpo)
            ? _localizer["Progress on {0} has been updated to {1}%.", mensaje.Asunto, mensaje.PorcentajeAvance!]
            : _localizer["Progress on {0} has been updated to {1}%. Note: {2}", mensaje.Asunto, mensaje.PorcentajeAvance!, mensaje.Cuerpo])
        : mensaje.Cuerpo;

    // Destinatarios stores "Email: cliente@correo.com; WhatsApp: 555-1234" for the audit trail — the
    // actual address isn't exposed to the anonymous client, only the channel name.
    private static string ChannelsOnly(string destinatarios) => string.Join(", ",
        destinatarios.Split("; ", StringSplitOptions.RemoveEmptyEntries).Select(part => part.Split(':')[0].Trim()));
}
