using App.Core.Enums.Obras;

namespace App.Core.DTOs.Obras;

public class ActividadEvidenciaFotoDto
{
    public int Id { get; set; }
    public int ActividadId { get; set; }
    public TipoEvidencia Tipo { get; set; }

    /// <summary>
    /// URL prefirmada de la imagen completa, generada al momento de la consulta; no se persiste.
    /// </summary>
    public string? PresignedUrl { get; set; }

    /// <summary>
    /// URL prefirmada de la miniatura; cae a <see cref="PresignedUrl"/> si la foto no tiene miniatura
    /// (filas cargadas antes de que existiera este campo).
    /// </summary>
    public string? ThumbnailPresignedUrl { get; set; }

    public DateTime FechaCarga { get; set; }
}
