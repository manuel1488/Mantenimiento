namespace App.Core.DTOs.Obras;

public class ObraFotoGeneralDto
{
    public int Id { get; set; }
    public int ObraId { get; set; }

    /// <summary>
    /// URL prefirmada de la imagen completa, generada al momento de la consulta; no se persiste.
    /// </summary>
    public string? PresignedUrl { get; set; }

    /// <summary>
    /// URL prefirmada de la miniatura; cae a <see cref="PresignedUrl"/> si la foto no tiene miniatura.
    /// </summary>
    public string? ThumbnailPresignedUrl { get; set; }

    public string? Descripcion { get; set; }

    public DateTime FechaCarga { get; set; }
}
