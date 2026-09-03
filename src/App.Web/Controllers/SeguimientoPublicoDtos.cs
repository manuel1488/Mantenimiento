namespace App.Web.Controllers;

/// <summary>
/// Response shape for the public, anonymous obra-tracking API (<see cref="SeguimientoPublicoController"/>).
/// Deliberately not one of the App.Core DTOs mapped by AutoMapper — this is a presentation contract
/// for the static /seguimiento.html + Alpine.js page, with labels already localized server-side and
/// only the fields safe to expose to an anonymous client (no ClienteId/ClienteCorreo, no full
/// Destinatarios addresses, etc.).
/// </summary>
public class ObraSeguimientoPublicoDto
{
    public string AppName { get; set; } = null!;
    public string LogoUrl { get; set; } = null!;
    public string PrimaryColor { get; set; } = null!;
    public string SecondaryColor { get; set; } = null!;
    public string Folio { get; set; } = null!;
    public string Direccion { get; set; } = null!;
    public string Estado { get; set; } = null!;
    public string EstadoLabel { get; set; } = null!;
    public int PorcentajeAvance { get; set; }
    public List<ActividadSeguimientoDto> Actividades { get; set; } = new();
    public List<MensajeSeguimientoDto> Mensajes { get; set; } = new();
    public List<FotoSeguimientoDto> Fotos { get; set; } = new();
}

public class ActividadSeguimientoDto
{
    public string ServicioNombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public string Estado { get; set; } = null!;
    public int PorcentajeAvance { get; set; }
}

public class MensajeSeguimientoDto
{
    public string Tipo { get; set; } = null!;
    public string TipoLabel { get; set; } = null!;
    public string Asunto { get; set; } = null!;
    public string Cuerpo { get; set; } = null!;
    public DateTime FechaEnvio { get; set; }
    public string Canales { get; set; } = null!;
    public string? FotoUrl { get; set; }
    public string? FotoThumbnailUrl { get; set; }
}

public class FotoSeguimientoDto
{
    public string? Url { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Descripcion { get; set; }
}
