using App.Core.Enums.Obras;

namespace App.Core.DTOs.Obras;

public class ActividadEvidenciaFotoDto
{
    public int Id { get; set; }
    public int ActividadId { get; set; }
    public TipoEvidencia Tipo { get; set; }

    /// <summary>
    /// URL prefirmada generada al momento de la consulta; no se persiste.
    /// </summary>
    public string? PresignedUrl { get; set; }

    public DateTime FechaCarga { get; set; }
}
