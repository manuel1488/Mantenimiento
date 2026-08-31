using App.Core.Enums.Obras;

namespace App.Core.DTOs.Obras;

public class ObraMensajeDto
{
    public int Id { get; set; }
    public int ObraId { get; set; }

    public TipoObraMensaje Tipo { get; set; }
    public string Asunto { get; set; } = null!;
    public string Cuerpo { get; set; } = null!;

    /// <summary>Only set when <see cref="Tipo"/> is <see cref="TipoObraMensaje.Avance"/> — see
    /// <see cref="App.Models.Obras.ObraMensaje.PorcentajeAvance"/>.</summary>
    public int? PorcentajeAvance { get; set; }

    /// <summary>
    /// URL prefirmada de la imagen completa, generada al momento de la consulta; no se persiste.
    /// </summary>
    public string? FotoPresignedUrl { get; set; }

    /// <summary>
    /// URL prefirmada de la miniatura; cae a <see cref="FotoPresignedUrl"/> si el mensaje no tiene foto con miniatura.
    /// </summary>
    public string? FotoThumbnailPresignedUrl { get; set; }

    /// <summary>Resumen legible de a qué canal(es)/dirección(es) se envió, p. ej. "Email: cliente@correo.com".</summary>
    public string Destinatarios { get; set; } = null!;

    public DateTime FechaEnvio { get; set; }
}
