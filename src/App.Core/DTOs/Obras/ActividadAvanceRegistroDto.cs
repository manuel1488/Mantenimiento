namespace App.Core.DTOs.Obras;

public class ActividadAvanceRegistroDto
{
    public int Id { get; set; }
    public int ActividadId { get; set; }
    public int PorcentajeAvance { get; set; }
    public string? Observaciones { get; set; }

    /// <summary>
    /// URL prefirmada de la foto adjunta al registro, generada al momento de la consulta; no se persiste.
    /// </summary>
    public string? FotoPresignedUrl { get; set; }

    /// <summary>
    /// URL prefirmada de la miniatura; cae a <see cref="FotoPresignedUrl"/> si no tiene miniatura.
    /// </summary>
    public string? FotoThumbnailPresignedUrl { get; set; }

    public DateTime FechaRegistro { get; set; }
}
