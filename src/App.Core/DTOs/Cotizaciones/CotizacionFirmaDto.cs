namespace App.Core.DTOs.Cotizaciones;

public class CotizacionFirmaDto
{
    public int Id { get; set; }
    public string FirmanteNombre { get; set; } = null!;
    public DateTime FechaFirma { get; set; }

    /// <summary>
    /// URL prefirmada de la imagen de la firma, generada al momento de la consulta; no se persiste.
    /// </summary>
    public string? PresignedUrl { get; set; }
}
